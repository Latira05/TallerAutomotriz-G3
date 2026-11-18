using AOTech.Data.Contexto;
using AOTech.Data.Modelos;
using AOTech.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace AOTech.Web.Controllers
{
    // TEMPORALMENTE DESACTIVADO PARA PRUEBAS
    // [Authorize(Roles = "Administrador,Recepcionista")]
    public class CitasController : Controller
    {
        private readonly AOTechDbContext _contexto;

        public CitasController(AOTechDbContext contexto)
        {
            _contexto = contexto;
        }

        // LISTADO DE CITAS
        [HttpGet]
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

            TempData["Exito"] =
                "La cita fue aprobada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // CANCELAR CITA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var cita = await _contexto.Citas
                .FirstOrDefaultAsync(c => c.CitaId == id);

            if (cita == null)
            {
                TempData["Error"] = "La cita no existe.";
                return RedirectToAction(nameof(Index));
            }

            // Validar que la cita pueda cancelarse
            if (cita.Estado != "Pendiente" &&
                cita.Estado != "Aprobada" &&
                cita.Estado != "Reprogramada")
            {
                TempData["Error"] =
                    "No se puede cancelar la cita porque su estado actual no lo permite.";

                return RedirectToAction(nameof(Index));
            }

            // Actualizar estado
            cita.Estado = "Cancelada";
            cita.FechaActualizacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();

            TempData["Exito"] =
                "La cita fue cancelada correctamente.";

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

            // Validar fecha pasada
            if (fechaNueva < DateOnly.FromDateTime(DateTime.Today))
            {
                ModelState.AddModelError(
                    nameof(modelo.FechaSolicitada),
                    "No se puede seleccionar una fecha pasada.");

                return View(modelo);
            }

            // Validar fecha bloqueada
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

            // Validar horario de atención
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

            // Guardar nueva fecha y hora
            cita.FechaSolicitada = fechaNueva;
            cita.HoraSolicitada = horaNueva;
            cita.Estado = "Reprogramada";
            cita.FechaActualizacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();

            TempData["Exito"] =
                "La cita fue reprogramada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // HISTORIAL DE CITAS
        [HttpGet]
        // [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Historial(
    int? clienteId,
    string? estado,
    DateOnly? fechaDesde,
    DateOnly? fechaHasta)
        {
            // Validar rango de fechas antes de consultar
            if (fechaDesde.HasValue &&
                fechaHasta.HasValue &&
                fechaDesde.Value > fechaHasta.Value)
            {
                TempData["Error"] =
                    "La fecha inicial no puede ser posterior a la fecha final.";

                var modeloInvalido = new HistorialCitasViewModel
                {
                    ClienteId = clienteId,
                    Estado = estado,
                    FechaDesde = fechaDesde,
                    FechaHasta = fechaHasta,
                    Citas = new List<Cita>()
                };

                return View(modeloInvalido);
            }

            var consulta = _contexto.Citas
                .AsNoTracking()
                .AsQueryable();

            // Filtrar por cliente
            if (clienteId.HasValue && clienteId.Value > 0)
            {
                consulta = consulta.Where(c =>
                    c.ClienteId == clienteId.Value);
            }

            // Filtrar por estado
            if (!string.IsNullOrWhiteSpace(estado))
            {
                consulta = consulta.Where(c =>
                    c.Estado == estado);
            }

            // Filtrar por fecha inicial
            if (fechaDesde.HasValue)
            {
                consulta = consulta.Where(c =>
                    c.FechaSolicitada >= fechaDesde.Value);
            }

            // Filtrar por fecha final
            if (fechaHasta.HasValue)
            {
                consulta = consulta.Where(c =>
                    c.FechaSolicitada <= fechaHasta.Value);
            }

            var modelo = new HistorialCitasViewModel
            {
                ClienteId = clienteId,
                Estado = estado,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,

                Citas = await consulta
                    .OrderByDescending(c => c.FechaSolicitada)
                    .ThenByDescending(c => c.HoraSolicitada)
                    .ToListAsync()
            };

            return View(modelo);
        }

        // AGENDAR CITA - MOSTRAR FORMULARIO
        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            var modelo = new CrearCitaViewModel();

            await CargarListasCrearCita(modelo);

            return View(modelo);
        }

        // AGENDAR CITA - GUARDAR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CrearCitaViewModel modelo)
        {
            // Comprobar que los datos sean válidos
            if (!ModelState.IsValid)
            {
                await CargarListasCrearCita(modelo);
                return View(modelo);
            }

            int clienteId = modelo.ClienteId!.Value;
            int vehiculoId = modelo.VehiculoId!.Value;
            int servicioId = modelo.ServicioId!.Value;
            DateOnly fecha = modelo.FechaSolicitada!.Value;
            TimeOnly hora = modelo.HoraSolicitada!.Value;

            // Validar fecha pasada
            if (fecha < DateOnly.FromDateTime(DateTime.Today))
            {
                ModelState.AddModelError(
                    nameof(modelo.FechaSolicitada),
                    "No se puede seleccionar una fecha pasada.");

                await CargarListasCrearCita(modelo);
                return View(modelo);
            }

            // Validar cliente activo
            bool clienteValido = await _contexto.Clientes
                .AnyAsync(c => c.ClienteId == clienteId && c.EstaActivo);

            if (!clienteValido)
            {
                ModelState.AddModelError(
                    nameof(modelo.ClienteId),
                    "El cliente seleccionado no existe o está inactivo.");

                await CargarListasCrearCita(modelo);
                return View(modelo);
            }

            // Validar que el vehículo pertenezca al cliente
            bool vehiculoValido = await _contexto.Vehiculos
                .AnyAsync(v =>
                    v.VehiculoId == vehiculoId &&
                    v.ClienteId == clienteId &&
                    v.EstaActivo);

            if (!vehiculoValido)
            {
                ModelState.AddModelError(
                    nameof(modelo.VehiculoId),
                    "El vehículo no pertenece al cliente seleccionado o está inactivo.");

                await CargarListasCrearCita(modelo);
                return View(modelo);
            }

            // Validar servicio activo
            var servicio = await _contexto.CatalogoServicios
                .FirstOrDefaultAsync(s =>
                    s.ServicioId == servicioId &&
                    s.EstaActivo);

            if (servicio == null)
            {
                ModelState.AddModelError(
                    nameof(modelo.ServicioId),
                    "El servicio seleccionado no existe o está inactivo.");

                await CargarListasCrearCita(modelo);
                return View(modelo);
            }

            // Validar fecha bloqueada
            bool fechaBloqueada = await _contexto.FechasBloqueadas
                .AnyAsync(f => f.FechaBloqueada == fecha);

            if (fechaBloqueada)
            {
                ModelState.AddModelError(
                    nameof(modelo.FechaSolicitada),
                    "El taller no trabaja en la fecha seleccionada.");

                await CargarListasCrearCita(modelo);
                return View(modelo);
            }

            // Buscar regla de agenda específica del servicio o general
            byte diaSemana = (byte)fecha.DayOfWeek;

            var reglas = await _contexto.ReglasAgenda
                .Where(r =>
                    r.DiaSemana == diaSemana &&
                    (r.ServicioId == null || r.ServicioId == servicioId))
                .OrderBy(r => r.HoraApertura)
                .ToListAsync();

            var regla = reglas.FirstOrDefault(r =>
                    r.ServicioId.HasValue &&
                    r.ServicioId == servicioId)
                ?? reglas.FirstOrDefault(r => r.ServicioId == null);

            if (regla == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No hay horarios configurados para la fecha y el servicio seleccionados.");

                await CargarListasCrearCita(modelo);
                return View(modelo);
            }

            // Validar horario de atención
            if (hora < regla.HoraApertura || hora >= regla.HoraCierre)
            {
                ModelState.AddModelError(
                    nameof(modelo.HoraSolicitada),
                    "La hora está fuera del horario de atención.");

                await CargarListasCrearCita(modelo);
                return View(modelo);
            }

            // Comprobar capacidad del horario
            string[] estadosOcupados =
            {
        "Pendiente",
        "Aprobada",
        "Reprogramada"
    };

            int citasOcupadas = await _contexto.Citas
                .CountAsync(c =>
                    c.FechaSolicitada == fecha &&
                    c.HoraSolicitada == hora &&
                    estadosOcupados.Contains(c.Estado) &&
                    (c.ServicioId == servicioId ||
                     c.ServicioId == null));

            if (citasOcupadas >= regla.CapacidadBahias)
            {
                ModelState.AddModelError(
                    nameof(modelo.HoraSolicitada),
                    "El horario seleccionado ya no tiene espacios disponibles.");

                await CargarListasCrearCita(modelo);
                return View(modelo);
            }

            // Crear la cita
            var cita = new Cita
            {
                ClienteId = clienteId,
                VehiculoId = vehiculoId,
                ServicioId = servicioId,
                FechaSolicitada = fecha,
                HoraSolicitada = hora,
                Estado = "Pendiente",
                Notas = modelo.Notas,
                FechaCreacion = DateTime.UtcNow
            };

            _contexto.Citas.Add(cita);

            await _contexto.SaveChangesAsync();

            TempData["Exito"] = "La cita fue agendada correctamente y está pendiente de aprobación.";

            return RedirectToAction(nameof(Index));
        }

        // CARGAR LISTAS DEL FORMULARIO
        private async Task CargarListasCrearCita(CrearCitaViewModel modelo)
        {
            modelo.Clientes = await _contexto.Clientes
                .AsNoTracking()
                .Where(c => c.EstaActivo)
                .OrderBy(c => c.ClienteId)
                .Select(c => new SelectListItem
                {
                    Value = c.ClienteId.ToString(),
                    Text = c.CodigoClienteFrecuente
                })
                .ToListAsync();

            modelo.Vehiculos = await _contexto.Vehiculos
                .AsNoTracking()
                .Where(v => v.EstaActivo)
                .OrderBy(v => v.Placa)
                .Select(v => new SelectListItem
                {
                    Value = v.VehiculoId.ToString(),
                    Text = v.Placa + " - " + v.Marca + " " + v.Modelo
                })
                .ToListAsync();

            modelo.Servicios = await _contexto.CatalogoServicios
                .AsNoTracking()
                .Where(s => s.EstaActivo)
                .OrderBy(s => s.Nombre)
                .Select(s => new SelectListItem
                {
                    Value = s.ServicioId.ToString(),
                    Text = s.Nombre
                })
                .ToListAsync();
        }

    }



}