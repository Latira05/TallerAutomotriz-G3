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
    [Authorize(Roles = "Cliente")]
    public class CitasController : ControllerBase
    {
        private readonly AOTechDbContext _contexto;

        public CitasController(AOTechDbContext contexto)
        {
            _contexto = contexto;
        }

        // =====================================================
        // POST: api/Citas/Agendar
        // =====================================================
        [HttpPost("Agendar")]
        public async Task<IActionResult> AgendarCita(
            [FromBody] AgendarCitaDto datos)
        {
            // 1. Validar los datos obligatorios
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

            // 2. Validar la fecha
            if (datos.FechaSolicitada <
                DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new
                {
                    mensaje = "No puede agendar una cita en una fecha pasada."
                });
            }

            // 3. Identificar al usuario autenticado
            var usuarioIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(usuarioIdClaim, out int usuarioId))
            {
                return Unauthorized(new
                {
                    mensaje = "No se pudo identificar al usuario autenticado."
                });
            }

            // 4. Verificar que el cliente pertenezca al usuario autenticado
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
                    v.ClienteId == datos.ClienteId);

            if (!vehiculoValido)
            {
                return BadRequest(new
                {
                    mensaje = "El vehículo no existe o no pertenece al cliente."
                });
            }

            // 6. Verificar que el servicio exista
            if (datos.ServicioId.HasValue)
            {
                bool servicioExiste =
                    await _contexto.CatalogoServicios
                        .AnyAsync(s =>
                            s.ServicioId == datos.ServicioId.Value);

                if (!servicioExiste)
                {
                    return BadRequest(new
                    {
                        mensaje = "El servicio seleccionado no existe."
                    });
                }
            }

            // 7. Comprobar si la fecha está bloqueada
            bool fechaBloqueada =
                await _contexto.FechasBloqueadas
                    .AnyAsync(f =>
                        f.FechaBloqueada == datos.FechaSolicitada);

            if (fechaBloqueada)
            {
                return Conflict(new
                {
                    mensaje = "La fecha seleccionada está bloqueada."
                });
            }

            // 8. Obtener las reglas de agenda del día
            byte diaSemana =
                (byte)datos.FechaSolicitada.DayOfWeek;

            var reglas = await _contexto.ReglasAgenda
                .Where(r =>
                    r.DiaSemana == diaSemana &&
                    (r.ServicioId == null ||
                     r.ServicioId == datos.ServicioId))
                .OrderBy(r => r.HoraApertura)
                .ToListAsync();

            // Priorizar la regla específica del servicio
            var reglaEspecifica = reglas
                .Where(r =>
                    r.ServicioId.HasValue &&
                    r.ServicioId == datos.ServicioId)
                .OrderBy(r => r.HoraApertura)
                .FirstOrDefault();

            // Utilizar la regla general si no hay una específica
            var reglaGeneral = reglas
                .Where(r => r.ServicioId == null)
                .OrderBy(r => r.HoraApertura)
                .FirstOrDefault();

            var regla = reglaEspecifica ?? reglaGeneral;

            if (regla == null)
            {
                return Conflict(new
                {
                    mensaje = "No hay horario configurado para ese día y servicio."
                });
            }

            // 9. Validar el horario de atención
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

            // 10. Comprobar la capacidad del horario
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

        // GET: api/Citas/Disponibilidad?fecha=2026-10-20&servicioId=1
        [HttpGet("Disponibilidad")]
        public async Task<IActionResult> ConsultarDisponibilidad(
            [FromQuery] DateOnly fecha,
            [FromQuery] int? servicioId)
        {
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

            if (servicioId.HasValue)
            {
                bool servicioExiste = await _contexto.CatalogoServicios
                    .AnyAsync(s => s.ServicioId == servicioId.Value);

                if (!servicioExiste)
                {
                    return BadRequest(new
                    {
                        mensaje = "El servicio seleccionado no existe."
                    });
                }
            }

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

            byte diaSemana = (byte)fecha.DayOfWeek;

            var reglas = await _contexto.ReglasAgenda
                .Where(r =>
                    r.DiaSemana == diaSemana &&
                    (r.ServicioId == null ||
                     r.ServicioId == servicioId))
                .OrderBy(r => r.HoraApertura)
                .ToListAsync();

            var reglaEspecifica = reglas
                .Where(r => r.ServicioId.HasValue &&
                            r.ServicioId == servicioId)
                .OrderBy(r => r.HoraApertura)
                .FirstOrDefault();

            var reglaGeneral = reglas
                .Where(r => r.ServicioId == null)
                .OrderBy(r => r.HoraApertura)
                .FirstOrDefault();

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

            // Generar horarios cada 30 minutos
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

        [HttpPut("Cancelar/{id:int}")]
        public async Task<IActionResult> CancelarCita(int id)
        {
            // Obtener el ID del usuario autenticado
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(usuarioIdClaim, out int usuarioId))
            {
                return Unauthorized(new
                {
                    mensaje = "No se pudo identificar al usuario autenticado."
                });
            }

            // Buscar la cita y el cliente asociado
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

            // Verificar que la cita pertenezca al cliente autenticado
            if (cita.Cliente.UsuarioId != usuarioId ||
                !cita.Cliente.EstaActivo)
            {
                return Forbid();
            }

            // Criterio 2: la cita ya está cancelada
            if (cita.Estado.Equals(
                "Cancelada",
                StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new
                {
                    mensaje = "La cita ya se encuentra cancelada."
                });
            }

            // Criterio 3: validar si la cita puede cancelarse
            if (!cita.Estado.Equals(
                    "Pendiente",
                    StringComparison.OrdinalIgnoreCase) &&
                !cita.Estado.Equals(
                    "Reprogramada",
                    StringComparison.OrdinalIgnoreCase) &&
                !cita.Estado.Equals(
                    "Aprobada",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new
                {
                    mensaje = "La cita no se puede cancelar debido a su estado actual.",
                    estado = cita.Estado
                });
            }

            // Criterio 1: cancelar sin eliminar el registro
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
            // 1. Validar que se haya enviado al menos un cambio
            if (!datos.FechaSolicitada.HasValue &&
                !datos.HoraSolicitada.HasValue)
            {
                return BadRequest(new
                {
                    mensaje = "Debe indicar la nueva fecha, la nueva hora o ambas."
                });
            }

            // 2. Identificar al usuario autenticado
            var usuarioIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(usuarioIdClaim, out int usuarioId))
            {
                return Unauthorized(new
                {
                    mensaje = "No se pudo identificar al usuario autenticado."
                });
            }

            // 3. Buscar la cita y comprobar que pertenezca al cliente
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

            if (cita.Cliente.UsuarioId != usuarioId ||
                !cita.Cliente.EstaActivo)
            {
                return Forbid();
            }

            // 4. Comprobar si la cita permite modificaciones
            // En este ejemplo solo se permiten citas Pendientes o Reprogramadas.
            if (cita.Estado != "Pendiente" &&
                cita.Estado != "Reprogramada")
            {
                return Conflict(new
                {
                    mensaje = "La cita no se puede modificar porque su estado actual no lo permite."
                });
            }

            // 5. Determinar la fecha y hora nuevas
            DateOnly nuevaFecha =
                datos.FechaSolicitada ?? cita.FechaSolicitada;

            TimeOnly nuevaHora =
                datos.HoraSolicitada ?? cita.HoraSolicitada;

            // 6. Validar la fecha
            if (nuevaFecha < DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new
                {
                    mensaje = "No puede modificar la cita a una fecha pasada."
                });
            }

            // 7. Verificar si la nueva fecha está bloqueada
            bool fechaBloqueada = await _contexto.FechasBloqueadas
                .AnyAsync(f => f.FechaBloqueada == nuevaFecha);

            if (fechaBloqueada)
            {
                return Conflict(new
                {
                    mensaje = "La nueva fecha está bloqueada. Seleccione otra fecha."
                });
            }

            // 8. Buscar la regla de agenda para el día y servicio de la cita
            byte diaSemana = (byte)nuevaFecha.DayOfWeek;

            var reglas = await _contexto.ReglasAgenda
                .Where(r =>
                    r.DiaSemana == diaSemana &&
                    (r.ServicioId == null ||
                     r.ServicioId == cita.ServicioId))
                .OrderBy(r => r.HoraApertura)
                .ToListAsync();

            var reglaEspecifica = reglas
                .Where(r =>
                    r.ServicioId.HasValue &&
                    r.ServicioId == cita.ServicioId)
                .FirstOrDefault();

            var reglaGeneral = reglas
                .Where(r => r.ServicioId == null)
                .FirstOrDefault();

            var regla = reglaEspecifica ?? reglaGeneral;

            if (regla == null)
            {
                return Conflict(new
                {
                    mensaje = "No hay horario configurado para la nueva fecha."
                });
            }

            // 9. Validar que la nueva hora esté dentro del horario permitido
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

            // 10. Verificar la capacidad del nuevo horario.
            // Se excluye la propia cita para no contarla como una reserva adicional.
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

            // 11. Aplicar los cambios
            cita.FechaSolicitada = nuevaFecha;
            cita.HoraSolicitada = nuevaHora;
            cita.FechaActualizacion = DateTime.UtcNow;

            // Si estaba Pendiente, se conserva ese estado.
            // Si estaba Reprogramada, también se conserva.

            await _contexto.SaveChangesAsync();

            // 12. Devolver el resultado
            return Ok(new
            {
                mensaje = "La cita se modificó correctamente.",
                citaId = cita.CitaId,
                fechaAnterior = datos.FechaSolicitada.HasValue
                    ? (DateOnly?)null
                    : cita.FechaSolicitada,
                fecha = cita.FechaSolicitada,
                hora = cita.HoraSolicitada.ToString("HH:mm"),
                estado = cita.Estado
            });





        }


    }
}