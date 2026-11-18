using AOTech.Data.Contexto;
using AOTech.Data.Modelos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AOTech.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //desactivado por prueba
    //[Authorize(Roles = "Cliente")]
    public class CitasController : ControllerBase
    {
        private readonly AOTechDbContext _contexto;

        public CitasController(AOTechDbContext contexto)
        {
            _contexto = contexto;
        }

        // =====================================================
        // MÉTODO AUXILIAR: IDENTIFICAR USUARIO AUTENTICADO
        // =====================================================
        private bool ObtenerUsuarioId(out int usuarioId)
        {
            var usuarioIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(usuarioIdClaim, out usuarioId);
        }

        // =====================================================
        // POST: api/Citas/Agendar
        // =====================================================
        [HttpPost("Agendar")]
        public async Task<IActionResult> AgendarCita(
            [FromBody] AgendarCitaDto datos)
        {
            if (datos == null)
            {
                return BadRequest(new
                {
                    mensaje = "Debe proporcionar los datos de la cita."
                });
            }

            // 1. Validar datos obligatorios
            if (datos.ClienteId <= 0 ||
                datos.VehiculoId <= 0 ||
                datos.FechaSolicitada == default ||
                datos.HoraSolicitada == default)
            {
                return BadRequest(new
                {
                    mensaje = "Debe completar todos los datos obligatorios."
                });
            }

            // 2. Validar que la fecha no sea pasada
            if (datos.FechaSolicitada <
                DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new
                {
                    mensaje = "No puede agendar una cita en una fecha pasada."
                });
            }

            // 3. Identificar al usuario autenticado
            if (!ObtenerUsuarioId(out int usuarioId))
            {
                return Unauthorized(new
                {
                    mensaje = "No se pudo identificar al usuario autenticado."
                });
            }

            // 4. Verificar que el cliente pertenezca al usuario
            bool clienteValido = await _contexto.Clientes
                .AnyAsync(c =>
                    c.ClienteId == datos.ClienteId &&
                    c.UsuarioId == usuarioId &&
                    c.EstaActivo);

            if (!clienteValido)
            {
                return Forbid();
            }

            // 5. Verificar que el vehículo pertenezca al cliente
            bool vehiculoValido = await _contexto.Vehiculos
                .AnyAsync(v =>
                    v.VehiculoId == datos.VehiculoId &&
                    v.ClienteId == datos.ClienteId &&
                    v.EstaActivo);

            if (!vehiculoValido)
            {
                return BadRequest(new
                {
                    mensaje = "El vehículo no existe, está inactivo o no pertenece al cliente."
                });
            }

            // 6. Verificar que el servicio exista y esté activo
            if (datos.ServicioId.HasValue)
            {
                bool servicioExiste = await _contexto.CatalogoServicios
                    .AnyAsync(s =>
                        s.ServicioId == datos.ServicioId.Value &&
                        s.EstaActivo);

                if (!servicioExiste)
                {
                    return BadRequest(new
                    {
                        mensaje = "El servicio seleccionado no existe o está inactivo."
                    });
                }
            }

            // 7. Verificar si la fecha está bloqueada
            bool fechaBloqueada = await _contexto.FechasBloqueadas
                .AnyAsync(f =>
                    f.FechaBloqueada == datos.FechaSolicitada);

            if (fechaBloqueada)
            {
                return Conflict(new
                {
                    mensaje = "La fecha seleccionada está bloqueada."
                });
            }

            // 8. Buscar las reglas de agenda
            byte diaSemana = (byte)datos.FechaSolicitada.DayOfWeek;

            var reglas = await _contexto.ReglasAgenda
                .Where(r =>
                    r.DiaSemana == diaSemana &&
                    (r.ServicioId == null ||
                     r.ServicioId == datos.ServicioId))
                .OrderBy(r => r.HoraApertura)
                .ToListAsync();

            // Priorizar regla específica del servicio
            var reglaEspecifica = reglas
                .FirstOrDefault(r =>
                    r.ServicioId.HasValue &&
                    r.ServicioId == datos.ServicioId);

            // Utilizar regla general si no existe una específica
            var reglaGeneral = reglas
                .FirstOrDefault(r => r.ServicioId == null);

            var regla = reglaEspecifica ?? reglaGeneral;

            if (regla == null)
            {
                return Conflict(new
                {
                    mensaje = "No hay horario configurado para ese día y servicio."
                });
            }

            // 9. Validar horario de atención
            if (datos.HoraSolicitada < regla.HoraApertura ||
                datos.HoraSolicitada >= regla.HoraCierre)
            {
                return Conflict(new
                {
                    mensaje = "La hora seleccionada está fuera del horario de atención.",
                    horaApertura = regla.HoraApertura.ToString("HH:mm"),
                    horaCierre = regla.HoraCierre.ToString("HH:mm")
                });
            }

            // 10. Comprobar capacidad del horario
            string[] estadosOcupados =
            {
                "Pendiente",
                "Aprobada",
                "Reprogramada"
            };

            int citasExistentes = await _contexto.Citas
                .CountAsync(c =>
                    c.FechaSolicitada == datos.FechaSolicitada &&
                    c.HoraSolicitada == datos.HoraSolicitada &&
                    estadosOcupados.Contains(c.Estado) &&
                    (c.ServicioId == datos.ServicioId ||
                     c.ServicioId == null ||
                     datos.ServicioId == null));

            if (citasExistentes >= regla.CapacidadBahias)
            {
                return Conflict(new
                {
                    mensaje = "El horario está ocupado. Seleccione otra hora."
                });
            }

            // 11. Registrar la cita
            var cita = new Cita
            {
                ClienteId = datos.ClienteId,
                VehiculoId = datos.VehiculoId,
                ServicioId = datos.ServicioId,
                FechaSolicitada = datos.FechaSolicitada,
                HoraSolicitada = datos.HoraSolicitada,
                Estado = "Pendiente",
                Notas = datos.Notas,
                FechaCreacion = DateTime.UtcNow
            };

            _contexto.Citas.Add(cita);
            await _contexto.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Cita agendada correctamente.",
                citaId = cita.CitaId,
                clienteId = cita.ClienteId,
                vehiculoId = cita.VehiculoId,
                servicioId = cita.ServicioId,
                fecha = cita.FechaSolicitada,
                hora = cita.HoraSolicitada.ToString("HH:mm"),
                estado = cita.Estado
            });
        }

        // =====================================================
        // GET: api/Citas/Disponibilidad?fecha=2026-11-20&servicioId=1
        // =====================================================
        [HttpGet("Disponibilidad")]
        public async Task<IActionResult> ConsultarDisponibilidad(
            [FromQuery] DateOnly fecha,
            [FromQuery] int? servicioId)
        {
            // 1. Validar fecha
            if (fecha == default)
            {
                return BadRequest(new
                {
                    mensaje = "Debe indicar una fecha válida."
                });
            }

            if (fecha < DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new
                {
                    mensaje = "No puede consultar fechas pasadas."
                });
            }

            // 2. Verificar servicio
            if (servicioId.HasValue)
            {
                bool servicioExiste = await _contexto.CatalogoServicios
                    .AnyAsync(s =>
                        s.ServicioId == servicioId.Value &&
                        s.EstaActivo);

                if (!servicioExiste)
                {
                    return BadRequest(new
                    {
                        mensaje = "El servicio seleccionado no existe o está inactivo."
                    });
                }
            }

            // 3. Verificar fecha bloqueada
            bool fechaBloqueada = await _contexto.FechasBloqueadas
                .AnyAsync(f => f.FechaBloqueada == fecha);

            if (fechaBloqueada)
            {
                return Ok(new
                {
                    fecha,
                    disponible = false,
                    mensaje = "La fecha seleccionada está bloqueada.",
                    horarios = Array.Empty<object>()
                });
            }

            // 4. Buscar reglas de agenda
            byte diaSemana = (byte)fecha.DayOfWeek;

            var reglas = await _contexto.ReglasAgenda
                .Where(r =>
                    r.DiaSemana == diaSemana &&
                    (r.ServicioId == null ||
                     r.ServicioId == servicioId))
                .OrderBy(r => r.HoraApertura)
                .ToListAsync();

            var reglaEspecifica = reglas
                .FirstOrDefault(r =>
                    r.ServicioId.HasValue &&
                    r.ServicioId == servicioId);

            var reglaGeneral = reglas
                .FirstOrDefault(r => r.ServicioId == null);

            var regla = reglaEspecifica ?? reglaGeneral;

            if (regla == null)
            {
                return Ok(new
                {
                    fecha,
                    disponible = false,
                    mensaje = "No hay horarios configurados para ese día y servicio.",
                    horarios = Array.Empty<object>()
                });
            }

            // 5. Obtener citas existentes
            string[] estadosOcupados =
            {
                "Pendiente",
                "Aprobada",
                "Reprogramada"
            };

            var citasExistentes = await _contexto.Citas
                .Where(c =>
                    c.FechaSolicitada == fecha &&
                    estadosOcupados.Contains(c.Estado) &&
                    (c.ServicioId == servicioId ||
                     c.ServicioId == null ||
                     servicioId == null))
                .Select(c => c.HoraSolicitada)
                .ToListAsync();

            // 6. Generar horarios cada 30 minutos
            var horarios = new List<object>();
            bool hayDisponibilidad = false;

            TimeOnly horaActual = regla.HoraApertura;

            while (horaActual < regla.HoraCierre)
            {
                int citasEnHorario =
                    citasExistentes.Count(h => h == horaActual);

                int espaciosDisponibles = Math.Max(
                    0,
                    regla.CapacidadBahias - citasEnHorario);

                bool disponibleHorario = espaciosDisponibles > 0;

                if (disponibleHorario)
                {
                    hayDisponibilidad = true;
                }

                horarios.Add(new
                {
                    hora = horaActual.ToString("HH:mm"),
                    disponible = disponibleHorario,
                    espaciosDisponibles
                });

                horaActual = horaActual.AddMinutes(30);
            }

            return Ok(new
            {
                fecha,
                servicioId,
                disponible = hayDisponibilidad,
                horaApertura = regla.HoraApertura.ToString("HH:mm"),
                horaCierre = regla.HoraCierre.ToString("HH:mm"),
                capacidadBahias = regla.CapacidadBahias,
                horarios
            });
        }

        // =====================================================
        // PUT: api/Citas/Cancelar/1
        // =====================================================
        [HttpPut("Cancelar/{id:int}")]
        public async Task<IActionResult> CancelarCita(int id)
        {
            // 1. Identificar usuario autenticado
            if (!ObtenerUsuarioId(out int usuarioId))
            {
                return Unauthorized(new
                {
                    mensaje = "No se pudo identificar al usuario autenticado."
                });
            }

            // 2. Buscar cita y cliente asociado
            var cita = await _contexto.Citas
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(c => c.CitaId == id);

            if (cita == null)
            {
                return NotFound(new
                {
                    mensaje = "La cita no existe."
                });
            }

            // 3. Verificar que la cita pertenezca al cliente
            if (cita.Cliente.UsuarioId != usuarioId ||
                !cita.Cliente.EstaActivo)
            {
                return Forbid();
            }

            // 4. Verificar que no esté cancelada
            if (cita.Estado.Equals(
                "Cancelada",
                StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new
                {
                    mensaje = "La cita ya se encuentra cancelada."
                });
            }

            // 5. Validar estados permitidos
            if (!cita.Estado.Equals("Pendiente",
                    StringComparison.OrdinalIgnoreCase) &&
                !cita.Estado.Equals("Reprogramada",
                    StringComparison.OrdinalIgnoreCase) &&
                !cita.Estado.Equals("Aprobada",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new
                {
                    mensaje = "La cita no se puede cancelar debido a su estado actual.",
                    estado = cita.Estado
                });
            }

            // 6. Cancelar sin eliminar el registro
            cita.Estado = "Cancelada";
            cita.FechaActualizacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "La cita se canceló correctamente.",
                citaId = cita.CitaId,
                fecha = cita.FechaSolicitada,
                hora = cita.HoraSolicitada.ToString("HH:mm"),
                estado = cita.Estado
            });
        }

        // =====================================================
        // PUT: api/Citas/Modificar/1
        // =====================================================
        [HttpPut("Modificar/{id:int}")]
        public async Task<IActionResult> ModificarCita(
            int id,
            [FromBody] ModificarCitaDto datos)
        {
            if (datos == null)
            {
                return BadRequest(new
                {
                    mensaje = "Debe proporcionar los datos de modificación."
                });
            }

            // 1. Validar que exista al menos un cambio
            if (!datos.FechaSolicitada.HasValue &&
                !datos.HoraSolicitada.HasValue)
            {
                return BadRequest(new
                {
                    mensaje = "Debe indicar la nueva fecha, la nueva hora o ambas."
                });
            }

            // 2. Identificar usuario autenticado
            if (!ObtenerUsuarioId(out int usuarioId))
            {
                return Unauthorized(new
                {
                    mensaje = "No se pudo identificar al usuario autenticado."
                });
            }

            // 3. Buscar cita y cliente asociado
            var cita = await _contexto.Citas
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(c => c.CitaId == id);

            if (cita == null)
            {
                return NotFound(new
                {
                    mensaje = "La cita no existe."
                });
            }

            // 4. Verificar propiedad de la cita
            if (cita.Cliente.UsuarioId != usuarioId ||
                !cita.Cliente.EstaActivo)
            {
                return Forbid();
            }

            // 5. Verificar estados que permiten modificación
            if (cita.Estado != "Pendiente" &&
                cita.Estado != "Reprogramada")
            {
                return Conflict(new
                {
                    mensaje = "La cita no se puede modificar porque su estado actual no lo permite."
                });
            }

            // 6. Guardar los valores originales ANTES de modificarlos
            DateOnly fechaAnterior = cita.FechaSolicitada;
            TimeOnly horaAnterior = cita.HoraSolicitada;

            // 7. Determinar los nuevos valores
            DateOnly nuevaFecha =
                datos.FechaSolicitada ?? cita.FechaSolicitada;

            TimeOnly nuevaHora =
                datos.HoraSolicitada ?? cita.HoraSolicitada;

            // 8. Validar fecha
            if (nuevaFecha < DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new
                {
                    mensaje = "No puede modificar la cita a una fecha pasada."
                });
            }

            // 9. Verificar fecha bloqueada
            bool fechaBloqueada = await _contexto.FechasBloqueadas
                .AnyAsync(f => f.FechaBloqueada == nuevaFecha);

            if (fechaBloqueada)
            {
                return Conflict(new
                {
                    mensaje = "La nueva fecha está bloqueada. Seleccione otra fecha."
                });
            }

            // 10. Buscar regla de agenda
            byte diaSemana = (byte)nuevaFecha.DayOfWeek;

            var reglas = await _contexto.ReglasAgenda
                .Where(r =>
                    r.DiaSemana == diaSemana &&
                    (r.ServicioId == null ||
                     r.ServicioId == cita.ServicioId))
                .OrderBy(r => r.HoraApertura)
                .ToListAsync();

            var reglaEspecifica = reglas
                .FirstOrDefault(r =>
                    r.ServicioId.HasValue &&
                    r.ServicioId == cita.ServicioId);

            var reglaGeneral = reglas
                .FirstOrDefault(r => r.ServicioId == null);

            var regla = reglaEspecifica ?? reglaGeneral;

            if (regla == null)
            {
                return Conflict(new
                {
                    mensaje = "No hay horario configurado para la nueva fecha."
                });
            }

            // 11. Validar horario permitido
            if (nuevaHora < regla.HoraApertura ||
                nuevaHora >= regla.HoraCierre)
            {
                return Conflict(new
                {
                    mensaje = "La nueva hora está fuera del horario de atención.",
                    horaApertura = regla.HoraApertura.ToString("HH:mm"),
                    horaCierre = regla.HoraCierre.ToString("HH:mm")
                });
            }

            // 12. Comprobar capacidad del horario
            // Se excluye la propia cita para no contarla dos veces.
            string[] estadosOcupados =
            {
                "Pendiente",
                "Aprobada",
                "Reprogramada"
            };

            int citasExistentes = await _contexto.Citas
                .CountAsync(c =>
                    c.CitaId != cita.CitaId &&
                    c.FechaSolicitada == nuevaFecha &&
                    c.HoraSolicitada == nuevaHora &&
                    estadosOcupados.Contains(c.Estado) &&
                    (c.ServicioId == cita.ServicioId ||
                     c.ServicioId == null ||
                     cita.ServicioId == null));

            if (citasExistentes >= regla.CapacidadBahias)
            {
                return Conflict(new
                {
                    mensaje = "El nuevo horario está ocupado. Seleccione otra hora."
                });
            }

            // 13. Aplicar los cambios
            cita.FechaSolicitada = nuevaFecha;
            cita.HoraSolicitada = nuevaHora;
            cita.FechaActualizacion = DateTime.UtcNow;

            // Se conserva el estado actual de la cita.

            await _contexto.SaveChangesAsync();

            // 14. Devolver los valores anteriores y nuevos
            return Ok(new
            {
                mensaje = "La cita se modificó correctamente.",
                citaId = cita.CitaId,
                fechaAnterior,
                horaAnterior = horaAnterior.ToString("HH:mm"),
                fecha = cita.FechaSolicitada,
                hora = cita.HoraSolicitada.ToString("HH:mm"),
                estado = cita.Estado
            });
        }
    }
}