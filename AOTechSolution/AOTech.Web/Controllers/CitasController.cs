
using AOTech.Data.Contexto;
using AOTech.Data.Modelos;
using AOTech.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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

            if (cita.Estado != "Pendiente" &&
                cita.Estado != "Aprobada" &&
                cita.Estado != "Reprogramada")
            {
                TempData["Error"] =
                    "No se puede cancelar la cita porque su estado actual no lo permite.";

                return RedirectToAction(nameof(Index));
            }

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

            // Validar horario, duración y capacidad
            // Se excluye la cita actual para evitar contarla dos veces.
            string? errorDisponibilidad =
                await ValidarDisponibilidadAsync(
                    fechaNueva,
                    horaNueva,
                    cita.ServicioId,
                    cita.CitaId);

            if (errorDisponibilidad != null)
            {
                ModelState.AddModelError(
                    nameof(modelo.HoraSolicitada),
                    errorDisponibilidad);

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
        public async Task<IActionResult> Crear(
            CrearCitaViewModel modelo)
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
                .AnyAsync(c =>
                    c.ClienteId == clienteId &&
                    c.EstaActivo);

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

            // Validar horario, duración y capacidad
            string? errorDisponibilidad =
                await ValidarDisponibilidadAsync(
                    fecha,
                    hora,
                    servicioId);

            if (errorDisponibilidad != null)
            {
                ModelState.AddModelError(
                    nameof(modelo.HoraSolicitada),
                    errorDisponibilidad);

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

            TempData["Exito"] =
                "La cita fue agendada correctamente y está pendiente de aprobación.";

            return RedirectToAction(nameof(Index));
        }

        // CARGAR LISTAS DEL FORMULARIO
        private async Task CargarListasCrearCita(
            CrearCitaViewModel modelo)
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
                    Text = v.Placa + " - " +
                           v.Marca + " " + v.Modelo
                })
                .ToListAsync();

            modelo.Servicios = await _contexto.CatalogoServicios
                .AsNoTracking()
                .Where(s => s.EstaActivo)
                .OrderBy(s => s.Nombre)
                .Select(s => new SelectListItem
                {
                    Value = s.ServicioId.ToString(),
                    Text = s.Nombre + " (" +
                           s.DuracionEstimadaMinutos + " min)"
                })
                .ToListAsync();
        }

        // VALIDAR DISPONIBILIDAD, DURACIÓN Y CAPACIDAD
        private async Task<string?> ValidarDisponibilidadAsync(
            DateOnly fecha,
            TimeOnly hora,
            int? servicioId,
            int? citaExcluirId = null)
        {
            // Verificar que exista un servicio asociado
            if (!servicioId.HasValue)
            {
                return "La cita no tiene un servicio asociado para calcular su duración.";
            }

            // Obtener duración del servicio
            var servicio = await _contexto.CatalogoServicios
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.ServicioId == servicioId.Value);

            if (servicio == null)
            {
                return "No se encontró el servicio seleccionado.";
            }

            int duracionNueva = servicio.DuracionEstimadaMinutos;

            if (duracionNueva <= 0)
            {
                return "La duración configurada para el servicio no es válida.";
            }

            // Calcular inicio y final de la nueva cita
            DateTime inicioNueva = fecha.ToDateTime(hora);
            DateTime finNueva = inicioNueva.AddMinutes(duracionNueva);

            // Obtener regla específica o general
            byte diaSemana = (byte)fecha.DayOfWeek;

            var reglas = await _contexto.ReglasAgenda
                .Where(r =>
                    r.DiaSemana == diaSemana &&
                    (r.ServicioId == null ||
                     r.ServicioId == servicioId.Value))
                .OrderBy(r => r.HoraApertura)
                .ToListAsync();

            var regla = reglas.FirstOrDefault(r =>
                    r.ServicioId.HasValue &&
                    r.ServicioId == servicioId.Value)
                ?? reglas.FirstOrDefault(r =>
                    r.ServicioId == null);

            if (regla == null)
            {
                return "No hay horarios configurados para la fecha y el servicio seleccionados.";
            }

            // Validar horario completo de la cita
            DateTime apertura = fecha.ToDateTime(regla.HoraApertura);
            DateTime cierre = fecha.ToDateTime(regla.HoraCierre);

            if (inicioNueva < apertura || finNueva > cierre)
            {
                return "La duración de la cita supera el horario de atención del taller.";
            }

            // Estados que ocupan una bahía
            string[] estadosOcupados =
            {
                "Pendiente",
                "Aprobada",
                "Reprogramada"
            };

            // Consultar citas existentes del mismo día
            var citasExistentes = await _contexto.Citas
                .AsNoTracking()
                .Include(c => c.Servicio)
                .Where(c =>
                    c.FechaSolicitada == fecha &&
                    c.CitaId != (citaExcluirId ?? 0) &&
                    estadosOcupados.Contains(c.Estado))
                .ToListAsync();

            // Construir intervalos de las citas existentes
            var intervalosExistentes = new List<(DateTime Inicio, DateTime Fin)>();

            foreach (var existente in citasExistentes)
            {
                int duracionExistente =
                    existente.Servicio?.DuracionEstimadaMinutos
                    ?? duracionNueva;

                // Evitar duraciones inválidas en registros existentes
                if (duracionExistente <= 0)
                {
                    duracionExistente = duracionNueva;
                }

                DateTime inicioExistente =
                    existente.FechaSolicitada.ToDateTime(
                        existente.HoraSolicitada);

                DateTime finExistente =
                    inicioExistente.AddMinutes(duracionExistente);

                intervalosExistentes.Add((
                    inicioExistente,
                    finExistente
                ));
            }

            // Revisar los momentos en los que puede aumentar
            // la ocupación durante la nueva cita.
            var puntosRevision = new List<DateTime>
            {
                inicioNueva
            };

            foreach (var intervalo in intervalosExistentes)
            {
                if (intervalo.Inicio > inicioNueva &&
                    intervalo.Inicio < finNueva)
                {
                    puntosRevision.Add(intervalo.Inicio);
                }
            }

            // Comprobar ocupación máxima simultánea
            foreach (var punto in puntosRevision)
            {
                int ocupacion = 1; // La nueva cita ocupa una bahía

                foreach (var intervalo in intervalosExistentes)
                {
                    bool estaOcupada =
                        intervalo.Inicio <= punto &&
                        intervalo.Fin > punto;

                    if (estaOcupada)
                    {
                        ocupacion++;
                    }
                }

                if (ocupacion > regla.CapacidadBahias)
                {
                    return "No hay suficientes bahías disponibles durante toda la duración de ese horario.";
                }
            }

            return null;
        }
    }
}