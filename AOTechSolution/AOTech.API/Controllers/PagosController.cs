
using AOTech.Data.Contexto;
using AOTech.Data.Modelos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AOTech.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PagosController : ControllerBase
    {
        private readonly AOTechDbContext _contexto;

        public PagosController(AOTechDbContext contexto)
        {
            _contexto = contexto;
        }


        // SCRUM-58 Actualizar órdenes de servicio
        [HttpGet("ordenes-cliente/{clienteId:int}")]
        public async Task<IActionResult> ObtenerOrdenesCliente(int clienteId)
        {
            if (clienteId <= 0)
            {
                return BadRequest("El identificador del cliente no es válido.");
            }

            bool clienteExiste = await _contexto.Clientes
                .AnyAsync(c => c.ClienteId == clienteId);

            if (!clienteExiste)
            {
                return NotFound("No se encontró el cliente.");
            }

            var ordenes = await _contexto.OrdenesServicios
                .AsNoTracking()
                .Where(o => o.ClienteId == clienteId
                    && o.Estado == "Finalizada")
                .Select(o => new
                {
                    o.OrdenServicioId,
                    o.Estado,
                    o.FechaIngreso,
                    o.MontoTotal,

                    TotalPagado = o.Pagos
        .Where(p => p.Estado == "Validado")
        .Sum(p => (decimal?)p.Monto) ?? 0,

                    TotalReportado = o.Pagos
        .Where(p => p.Estado == "Reportado")
        .Sum(p => (decimal?)p.Monto) ?? 0,

                    SaldoPendiente = o.MontoTotal -
        o.Pagos
            .Where(p => p.Estado == "Validado" || p.Estado == "Reportado")
            .Sum(p => (decimal?)p.Monto) ?? 0
                })
                .ToListAsync();

            return Ok(ordenes);
        }



        // MO-03: Reportar un pago realizado por el cliente
        [HttpPost("reportar")]
        public async Task<IActionResult> ReportarPago([FromBody] Pago pago)
        {
            if (pago == null)
            {
                return BadRequest("Los datos del pago son obligatorios.");
            }

            if (pago.ClienteId <= 0)
            {
                return BadRequest("El identificador del cliente no es válido.");
            }

            if (pago.OrdenServicioId <= 0)
            {
                return BadRequest("El identificador de la orden no es válido.");
            }

            if (pago.Monto <= 0)
            {
                return BadRequest("El monto del pago debe ser mayor que cero.");
            }

            if (string.IsNullOrWhiteSpace(pago.MetodoPago))
            {
                return BadRequest("Debe indicar el método de pago.");
            }

            bool clienteExiste = await _contexto.Clientes
                .AnyAsync(c => c.ClienteId == pago.ClienteId);

            if (!clienteExiste)
            {
                return NotFound("No se encontró el cliente.");
            }

            var orden = await _contexto.OrdenesServicios
                .FirstOrDefaultAsync(o =>
                    o.OrdenServicioId == pago.OrdenServicioId &&
                    o.ClienteId == pago.ClienteId);

            if (orden == null)
            {
                return BadRequest(
                    "La orden no existe o no pertenece al cliente indicado.");
            }

            if (orden.Estado != "Finalizada")
            {
                return BadRequest(
                    "Solo se pueden reportar pagos de órdenes finalizadas.");
            }

            decimal totalComprometido = await _contexto.Pagos
    .Where(p => p.OrdenServicioId == pago.OrdenServicioId
        && (p.Estado == "Validado" || p.Estado == "Reportado"))
    .SumAsync(p => (decimal?)p.Monto) ?? 0;

            decimal saldoPendiente = orden.MontoTotal - totalComprometido;

            if (pago.Monto > saldoPendiente)
            {
                return BadRequest(
                    $"El monto supera el saldo pendiente de la orden: {saldoPendiente:N2}.");
            }

            pago.PagoId = 0;
            pago.Estado = "Reportado";
            pago.FechaReporte = DateTime.UtcNow;
            pago.ValidadoPorUsuarioId = null;
            pago.FechaValidacion = null;
            pago.MetodoPago = pago.MetodoPago.Trim();

            _contexto.Pagos.Add(pago);

            await _contexto.SaveChangesAsync();

            return Ok(new
            {
                Mensaje = "El pago fue reportado correctamente.",
                pago.PagoId,
                pago.OrdenServicioId,
                pago.ClienteId,
                pago.Monto,
                pago.MetodoPago,
                pago.Estado,
                pago.FechaReporte
            });
        }
        // SCRUM-59 Reportar pagos
        [HttpGet("pendientes")]
        public async Task<IActionResult> ObtenerPagosPendientes()
        {
            var pagos = await _contexto.Pagos
                .AsNoTracking()
                .Where(p => p.Estado == "Reportado")
                .Select(p => new
                {
                    p.PagoId,
                    p.ClienteId,
                    NombreCliente = p.Cliente.Usuario.NombreCompleto,
                    p.OrdenServicioId,
                    p.Monto,
                    p.MetodoPago,
                    p.FechaReporte
                })
                .OrderBy(p => p.FechaReporte)
                .ToListAsync();

            return Ok(pagos);
        }
        // MO-04: Rechazar un pago reportado
        [HttpPut("{pagoId:int}/rechazar/{usuarioId:int}")]
        public async Task<IActionResult> RechazarPago(int pagoId, int usuarioId)
        {
            if (pagoId <= 0 || usuarioId <= 0)
            {
                return BadRequest("Los identificadores proporcionados no son válidos.");
            }

            var usuario = await _contexto.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

            if (usuario == null)
            {
                return NotFound("No se encontró el usuario.");
            }

            if (usuario.Rol.NombreRol != "Administrador")
            {
                return Forbid();
            }

            var pago = await _contexto.Pagos
                .FirstOrDefaultAsync(p => p.PagoId == pagoId);

            if (pago == null)
            {
                return NotFound("No se encontró el pago.");
            }

            if (pago.Estado != "Reportado")
            {
                return BadRequest("El pago ya fue procesado anteriormente.");
            }

            pago.Estado = "Rechazado";
            pago.ValidadoPorUsuarioId = usuarioId;
            pago.FechaValidacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();

            return Ok(new
            {
                Mensaje = "El pago fue rechazado correctamente.",
                pago.PagoId,
                pago.Estado,
                pago.ValidadoPorUsuarioId,
                pago.FechaValidacion
            });
        }


        // MO-04: Aprobar un pago reportado
        [HttpPut("{pagoId:int}/validar/{usuarioId:int}")]
        public async Task<IActionResult> ValidarPago(int pagoId, int usuarioId)
        {
            if (pagoId <= 0 || usuarioId <= 0)
            {
                return BadRequest("Los identificadores proporcionados no son válidos.");
            }

            var usuario = await _contexto.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

            if (usuario == null)
            {
                return NotFound("No se encontró el usuario.");
            }

            if (usuario.Rol.NombreRol != "Administrador")
            {
                return Forbid();
            }

            var pago = await _contexto.Pagos
                .FirstOrDefaultAsync(p => p.PagoId == pagoId);

            if (pago == null)
            {
                return NotFound("No se encontró el pago.");
            }

            if (pago.Estado != "Reportado")
            {
                return BadRequest("El pago ya fue procesado anteriormente.");
            }

            pago.Estado = "Validado";
            pago.ValidadoPorUsuarioId = usuarioId;
            pago.FechaValidacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();

            return Ok(new
            {
                Mensaje = "El pago fue validado correctamente.",
                pago.PagoId,
                pago.Estado,
                pago.ValidadoPorUsuarioId,
                pago.FechaValidacion
            });
        }



        // SCRUM-61: Consultar estado de cuenta de un cliente
        [HttpGet("estado-cuenta/{clienteId:int}")]
        public async Task<IActionResult> ObtenerEstadoCuenta(int clienteId)
        {
            if (clienteId <= 0)
            {
                return BadRequest("El identificador del cliente no es válido.");
            }

            bool clienteExiste = await _contexto.Clientes
                .AnyAsync(c => c.ClienteId == clienteId);

            if (!clienteExiste)
            {
                return NotFound("No se encontró el cliente.");
            }

            // Consultar resumen financiero desde la vista SQL
            var resumen = await _contexto.VwSaldoCuentaClientes
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.ClienteId == clienteId);

            // Consultar órdenes finalizadas y sus pagos validados
            var ordenes = await _contexto.OrdenesServicios
                .AsNoTracking()
                .Where(o => o.ClienteId == clienteId
                    && o.Estado == "Finalizada")
                .Select(o => new
                {
                    o.OrdenServicioId,
                    o.FechaIngreso,
                    o.MontoTotal,

                    PagosAplicados = o.Pagos
                        .Where(p => p.Estado == "Validado")
                        .Sum(p => (decimal?)p.Monto) ?? 0,

                    SaldoPendiente = o.MontoTotal -
                        (o.Pagos
                            .Where(p => p.Estado == "Validado")
                            .Sum(p => (decimal?)p.Monto) ?? 0)
                })
                .ToListAsync();

            return Ok(new
            {
                ClienteId = clienteId,

                TotalCargos = resumen?.TotalCargos ?? 0,
                TotalPagado = resumen?.TotalPagado ?? 0,
                Saldo = resumen?.Saldo ?? 0,

                Ordenes = ordenes
            });
        }


        // SCRUM-62: Consultar historial de pagos de un cliente
        [HttpGet("historial/{clienteId:int}")]
        public async Task<IActionResult> ObtenerHistorialPagos(int clienteId)
        {
            if (clienteId <= 0)
            {
                return BadRequest("El identificador del cliente no es válido.");
            }

            bool clienteExiste = await _contexto.Clientes
                .AnyAsync(c => c.ClienteId == clienteId);

            if (!clienteExiste)
            {
                return NotFound("No se encontró el cliente.");
            }

            var pagos = await _contexto.Pagos
                .AsNoTracking()
                .Where(p => p.ClienteId == clienteId)
                .OrderByDescending(p => p.FechaReporte)
                .Select(p => new
                {
                    p.PagoId,
                    p.OrdenServicioId,
                    p.FechaReporte,
                    p.Monto,
                    p.MetodoPago,
                    p.Estado,
                    p.FechaValidacion
                })
                .ToListAsync();

            return Ok(new
            {
                ClienteId = clienteId,
                TotalPagos = pagos.Count,
                Pagos = pagos
            });


        }

    }
}