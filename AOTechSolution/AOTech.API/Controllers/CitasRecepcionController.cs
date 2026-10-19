using AOTech.Data.Contexto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AOTech.API.Controllers
{
    [ApiController]
    [Route("api/CitasRecepcion")]
    [Authorize(Roles = "Recepcionista")]
    public class CitasRecepcionController : ControllerBase
    {
        private readonly AOTechDbContext _contexto;

        public CitasRecepcionController(AOTechDbContext contexto)
        {
            _contexto = contexto;
        }

        // Criterio 1: aprobar una solicitud de cita
        [HttpPut("Aprobar/{id:int}")]
        public async Task<IActionResult> AprobarCita(int id)
        {
            var cita = await _contexto.Citas
                .FirstOrDefaultAsync(c => c.CitaId == id);

            if (cita == null)
            {
                return NotFound(new
                {
                    mensaje = "La solicitud de cita no existe."
                });
            }

            if (cita.Estado != "Pendiente")
            {
                return BadRequest(new
                {
                    mensaje = "Solo se pueden aprobar citas pendientes.",
                    estadoActual = cita.Estado
                });
            }

            cita.Estado = "Aprobada";
            cita.FechaActualizacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "La cita fue aprobada correctamente.",
                citaId = cita.CitaId,
                estado = cita.Estado
            });
        }

        // Criterio 2: reprogramar una solicitud de cita
        [HttpPut("Reprogramar/{id:int}")]
        public async Task<IActionResult> ReprogramarCita(
            int id,
            [FromBody] ReprogramarCitaDto datos)
        {
            var cita = await _contexto.Citas
                .FirstOrDefaultAsync(c => c.CitaId == id);

            if (cita == null)
            {
                return NotFound(new
                {
                    mensaje = "La solicitud de cita no existe."
                });
            }

            if (cita.Estado != "Pendiente" &&
                cita.Estado != "Aprobada" &&
                cita.Estado != "Reprogramada")
            {
                return BadRequest(new
                {
                    mensaje = "La cita no puede reprogramarse debido a su estado actual.",
                    estadoActual = cita.Estado
                });
            }

            var fechaNueva = datos.FechaSolicitada;
            var horaNueva = datos.HoraSolicitada;

            if (fechaNueva == default || horaNueva == default)
            {
                return BadRequest(new
                {
                    mensaje = "Debe indicar una fecha y una hora válidas."
                });
            }

            if (fechaNueva < DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new
                {
                    mensaje = "No se puede reprogramar una cita para una fecha pasada."
                });
            }

            // Verificar si la fecha está bloqueada
            bool fechaBloqueada = await _contexto.FechasBloqueadas
                .AnyAsync(f => f.FechaBloqueada == fechaNueva);

            if (fechaBloqueada)
            {
                return BadRequest(new
                {
                    mensaje = "La fecha seleccionada está bloqueada."
                });
            }

            // Buscar las reglas de agenda del día y servicio
            byte diaSemana = (byte)fechaNueva.DayOfWeek;

            var reglas = await _contexto.ReglasAgenda
                .Where(r =>
                    r.DiaSemana == diaSemana &&
                    (r.ServicioId == null ||
                     r.ServicioId == cita.ServicioId))
                .OrderBy(r => r.HoraApertura)
                .ToListAsync();

            var regla = reglas
                .Where(r => r.ServicioId.HasValue &&
                            r.ServicioId == cita.ServicioId)
                .FirstOrDefault()
                ?? reglas.FirstOrDefault(r => r.ServicioId == null);

            if (regla == null)
            {
                return BadRequest(new
                {
                    mensaje = "No hay horarios configurados para esa fecha y servicio."
                });
            }

            if (horaNueva < regla.HoraApertura ||
                horaNueva >= regla.HoraCierre)
            {
                return BadRequest(new
                {
                    mensaje = "La hora seleccionada está fuera del horario de atención."
                });
            }

            // Verificar la capacidad disponible, excluyendo esta cita
            string[] estadosOcupados =
            {
                "Pendiente",
                "Aprobada",
                "Reprogramada"
            };

            var citasOcupadas = await _contexto.Citas
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
                return Conflict(new
                {
                    mensaje = "El nuevo horario no tiene espacios disponibles."
                });
            }

            // Guardar la reprogramación
            var fechaAnterior = cita.FechaSolicitada;
            var horaAnterior = cita.HoraSolicitada;

            cita.FechaSolicitada = fechaNueva;
            cita.HoraSolicitada = horaNueva;
            cita.Estado = "Reprogramada";
            cita.FechaActualizacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "La cita fue reprogramada correctamente.",
                citaId = cita.CitaId,
                fechaAnterior,
                horaAnterior = horaAnterior.ToString("HH:mm"),
                fechaNueva = cita.FechaSolicitada,
                horaNueva = cita.HoraSolicitada.ToString("HH:mm"),
                estado = cita.Estado
            });
        }
    }

    public class ReprogramarCitaDto
    {
        public DateOnly FechaSolicitada { get; set; }

        public TimeOnly HoraSolicitada { get; set; }
    }
}