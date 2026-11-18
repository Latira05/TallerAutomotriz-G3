using AOTech.Data.Contexto;
using AOTech.Data.Modelos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AOTech.Web.Controllers
{
    [Authorize(Roles = "Administrador,Recepcionista")]
    public class CitasController : Controller
    {
        private readonly AOTechDbContext _contexto;

        public CitasController(AOTechDbContext contexto)
        {
            _contexto = contexto;
        }

        // LISTADO DE CITAS
        public async Task<IActionResult> Index()
        {
            var citas = await _contexto.Citas
                .AsNoTracking()
                .OrderBy(c => c.FechaSolicitada)
                .ThenBy(c => c.HoraSolicitada)
                .ToListAsync();

            return View(citas);
        }

        // APROBAR CITA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprobar(int id)
        {
            var cita = await _contexto.Citas
                .FirstOrDefaultAsync(c => c.CitaId == id);

            if (cita == null)
            {
                TempData["Error"] = "La cita no existe.";
                return RedirectToAction(nameof(Index));
            }

            if (cita.Estado != "Pendiente")
            {
                TempData["Error"] =
                    "Solo se pueden aprobar citas pendientes.";

                return RedirectToAction(nameof(Index));
            }

            cita.Estado = "Aprobada";
            cita.FechaActualizacion = DateTime.UtcNow;

            // Registrar quién aprobó la cita
            var usuarioIdClaim = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(usuarioIdClaim, out int usuarioId))
            {
                cita.AprobadaPorUsuarioId = usuarioId;
            }

            await _contexto.SaveChangesAsync();

            TempData["Exito"] = "La cita fue aprobada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // REPROGRAMAR CITA - MOSTRAR FORMULARIO
        [HttpGet]
        public async Task<IActionResult> Reprogramar(int id)
        {
            var cita = await _contexto.Citas
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CitaId == id);

            if (cita == null)
            {
                TempData["Error"] = "La cita no existe.";
                return RedirectToAction(nameof(Index));
            }

            if (cita.Estado != "Pendiente" &&
                cita.Estado != "Aprobada" &&
                cita.Estado != "Reprogramada")
            {
                TempData["Error"] =
                    "La cita no se puede reprogramar por su estado actual.";

                return RedirectToAction(nameof(Index));
            }

            var modelo = new ReprogramarCitaViewModel
            {
                CitaId = cita.CitaId,
                FechaSolicitada = cita.FechaSolicitada,
                HoraSolicitada = cita.HoraSolicitada
            };

            return View(modelo);
        }

        // REPROGRAMAR CITA - GUARDAR CAMBIOS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reprogramar(
            ReprogramarCitaViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var cita = await _contexto.Citas
                .FirstOrDefaultAsync(c => c.CitaId == modelo.CitaId);

            if (cita == null)
            {
                TempData["Error"] = "La cita no existe.";
                return RedirectToAction(nameof(Index));
            }

            if (cita.Estado != "Pendiente" &&
                cita.Estado != "Aprobada" &&
                cita.Estado != "Reprogramada")
            {
                TempData["Error"] =
                    "La cita no se puede reprogramar por su estado actual.";

                return RedirectToAction(nameof(Index));
            }

            var fechaNueva = modelo.FechaSolicitada;
            var horaNueva = modelo.HoraSolicitada;

            if (fechaNueva < DateOnly.FromDateTime(DateTime.Today))
            {
                ModelState.AddModelError(
                    nameof(modelo.FechaSolicitada),
                    "No se puede seleccionar una fecha pasada.");

                return View(modelo);
            }

            bool fechaBloqueada = await _contexto.FechasBloqueadas
                .AnyAsync(f => f.FechaBloqueada == fechaNueva);

            if (fechaBloqueada)
            {
                ModelState.AddModelError(
                    nameof(modelo.FechaSolicitada),
                    "La fecha seleccionada está bloqueada.");

                return View(modelo);
            }

            // Buscar regla específica del servicio o regla general
            byte diaSemana = (byte)fechaNueva.DayOfWeek;

            var reglas = await _contexto.ReglasAgenda
                .Where(r =>
                    r.DiaSemana == diaSemana &&
                    (r.ServicioId == null ||
                     r.ServicioId == cita.ServicioId))
                .OrderBy(r => r.HoraApertura)
                .ToListAsync();

            var regla = reglas.FirstOrDefault(
                r => r.ServicioId.HasValue &&
                     r.ServicioId == cita.ServicioId)
                ?? reglas.FirstOrDefault(r => r.ServicioId == null);

            if (regla == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No hay horarios configurados para esa fecha y servicio.");

                return View(modelo);
            }

            if (horaNueva < regla.HoraApertura ||
                horaNueva >= regla.HoraCierre)
            {
                ModelState.AddModelError(
                    nameof(modelo.HoraSolicitada),
                    "La hora está fuera del horario de atención.");

                return View(modelo);
            }

            // Comprobar capacidad en el horario solicitado
            string[] estadosOcupados =
            {
                "Pendiente",
                "Aprobada",
                "Reprogramada"
            };

            int citasOcupadas = await _contexto.Citas
                .CountAsync(c =>
                    c.CitaId != cita.CitaId &&
                    c.FechaSolicitada == fechaNueva &&
                    c.HoraSolicitada == horaNueva &&
                    estadosOcupados.Contains(c.Estado) &&
                    (c.ServicioId == cita.ServicioId ||
                     c.ServicioId == null ||
                     cita.ServicioId == null));

            if (citasOcupadas >= regla.CapacidadBahias)
            {
                ModelState.AddModelError(
                    nameof(modelo.HoraSolicitada),
                    "El horario seleccionado está ocupado.");

                return View(modelo);
            }

            cita.FechaSolicitada = fechaNueva;
            cita.HoraSolicitada = horaNueva;
            cita.Estado = "Reprogramada";
            cita.FechaActualizacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();

            TempData["Exito"] =
                "La cita fue reprogramada correctamente.";

            return RedirectToAction(nameof(Index));
        }
    }

    public class ReprogramarCitaViewModel
    {
        public int CitaId { get; set; }

        public DateOnly FechaSolicitada { get; set; }

        public TimeOnly HoraSolicitada { get; set; }
    }
}