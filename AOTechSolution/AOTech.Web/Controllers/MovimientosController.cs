using System.Security.Claims;
using AOTech.Data.Contexto;
using AOTech.Data.Modelos;
using AOTech.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AOTech.Web.Controllers
{
    // Cualquier usuario con sesión entra a la clase; cada acción define su rol.
    [Authorize]
    public class MovimientosController : Controller
    {
        private readonly AOTechDbContext _contexto;

        // Máximo de filas que se muestran en el historial (se avisa si hay más)
        private const int LimiteHistorial = 200;

        public MovimientosController(AOTechDbContext contexto)
        {
            _contexto = contexto;
        }

        // Accesos cortos a las tablas
        private DbSet<Repuesto> Repuestos => _contexto.Set<Repuesto>();
        private DbSet<MovimientosInventario> Movimientos => _contexto.Set<MovimientosInventario>();
        private DbSet<OrdenesServicio> Ordenes => _contexto.Set<OrdenesServicio>();

        // =====================================================
        // HISTORIAL DE MOVIMIENTOS (con filtros)
        // =====================================================
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta, int? repuestoId, int? ordenId, string? tipo)
        {
            var consulta =
                from m in Movimientos.AsNoTracking()
                join r in Repuestos on m.RepuestoId equals r.RepuestoId
                join u in _contexto.Usuarios on m.UsuarioId equals u.UsuarioId
                select new { m, r, u };

            if (desde.HasValue)
            {
                var inicio = desde.Value.Date;
                consulta = consulta.Where(x => x.m.FechaMovimiento >= inicio);
            }

            if (hasta.HasValue)
            {
                // "hasta" incluye todo ese día
                var limite = hasta.Value.Date.AddDays(1);
                consulta = consulta.Where(x => x.m.FechaMovimiento < limite);
            }

            if (repuestoId.HasValue)
            {
                consulta = consulta.Where(x => x.m.RepuestoId == repuestoId.Value);
            }

            if (ordenId.HasValue)
            {
                consulta = consulta.Where(x => x.m.OrdenServicioId == ordenId.Value);
            }

            if (tipo == "Entrada" || tipo == "Salida")
            {
                consulta = consulta.Where(x => x.m.TipoMovimiento == tipo);
            }
            else
            {
                tipo = null;
            }

            var total = await consulta.CountAsync();

            var movimientos = await consulta
                .OrderByDescending(x => x.m.FechaMovimiento)
                .ThenByDescending(x => x.m.MovimientoId)
                .Take(LimiteHistorial)
                .Select(x => new MovimientoListaViewModel
                {
                    MovimientoId = x.m.MovimientoId,
                    FechaMovimiento = x.m.FechaMovimiento,
                    TipoMovimiento = x.m.TipoMovimiento,
                    NombreRepuesto = x.r.Nombre,
                    Cantidad = x.m.Cantidad,
                    OrdenServicioId = x.m.OrdenServicioId,
                    NombreUsuario = x.u.NombreCompleto,
                    Motivo = x.m.Motivo
                })
                .ToListAsync();

            return View(new MovimientoIndexViewModel
            {
                Movimientos = movimientos,
                Desde = desde,
                Hasta = hasta,
                RepuestoId = repuestoId,
                OrdenId = ordenId,
                Tipo = tipo,
                // En el filtro se incluyen también los repuestos dados de baja (pueden tener historial)
                Repuestos = await ObtenerOpcionesAsync(soloActivos: false),
                TotalCoincidencias = total,
                Limite = LimiteHistorial
            });
        }

        // =====================================================
        // INGRESO DE REPUESTOS — GET
        // =====================================================
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Entrada(int? repuestoId)
        {
            return View(new MovimientoEntradaViewModel
            {
                RepuestoId = repuestoId,
                Repuestos = await ObtenerOpcionesAsync(soloActivos: true)
            });
        }

        // =====================================================
        // INGRESO DE REPUESTOS — POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Entrada(MovimientoEntradaViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return await VolverAEntradaAsync(modelo);
            }

            var usuarioId = await ObtenerUsuarioActualIdAsync();
            if (usuarioId == null)
            {
                ModelState.AddModelError(string.Empty, "No se pudo identificar al usuario que registra el ingreso. Cierra sesión y vuelve a entrar.");
                return await VolverAEntradaAsync(modelo);
            }

            var repuestoId = modelo.RepuestoId!.Value;
            var cantidad = modelo.Cantidad!.Value;

            var repuesto = await Repuestos.AsNoTracking().FirstOrDefaultAsync(r => r.RepuestoId == repuestoId);

            if (repuesto == null || !repuesto.EstaActivo)
            {
                ModelState.AddModelError(nameof(modelo.RepuestoId), "El repuesto no existe o está dado de baja.");
                return await VolverAEntradaAsync(modelo);
            }

            // Todo o nada: la suma al stock y el registro del movimiento van juntos.
            await using var transaccion = await _contexto.Database.BeginTransactionAsync();

            // Suma directa en la base de datos (segura aunque dos personas registren a la vez)
            await Repuestos
                .Where(r => r.RepuestoId == repuestoId)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.CantidadStock, r => r.CantidadStock + cantidad));

            Movimientos.Add(new MovimientosInventario
            {
                RepuestoId = repuestoId,
                TipoMovimiento = "Entrada",
                Cantidad = cantidad,
                OrdenServicioId = null,
                UsuarioId = usuarioId.Value,
                Motivo = modelo.Motivo,
                FechaMovimiento = DateTime.Now
            });

            await _contexto.SaveChangesAsync();

            var stockActual = await Repuestos.AsNoTracking()
                .Where(r => r.RepuestoId == repuestoId)
                .Select(r => r.CantidadStock)
                .FirstAsync();

            await transaccion.CommitAsync();

            TempData["Exito"] = $"Ingreso registrado: +{cantidad} de \"{repuesto.Nombre}\". Existencias actuales: {stockActual}.";
            return RedirectToAction("Index", "Repuestos");
        }

        // =====================================================
        // SALIDA DE REPUESTOS (uso en una orden de servicio) — GET
        // =====================================================
        [Authorize(Roles = "Administrador,Mecanico")]
        public async Task<IActionResult> Salida(int? repuestoId)
        {
            return View(new MovimientoSalidaViewModel
            {
                RepuestoId = repuestoId,
                Repuestos = await ObtenerOpcionesAsync(soloActivos: true),
                Ordenes = await ObtenerOrdenesAbiertasAsync()
            });
        }

        // =====================================================
        // SALIDA DE REPUESTOS — POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Mecanico")]
        public async Task<IActionResult> Salida(MovimientoSalidaViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return await VolverASalidaAsync(modelo);
            }

            var usuarioId = await ObtenerUsuarioActualIdAsync();
            if (usuarioId == null)
            {
                ModelState.AddModelError(string.Empty, "No se pudo identificar al usuario que registra la salida. Cierra sesión y vuelve a entrar.");
                return await VolverASalidaAsync(modelo);
            }

            var repuestoId = modelo.RepuestoId!.Value;
            var cantidad = modelo.Cantidad!.Value;
            var ordenId = modelo.OrdenServicioId!.Value;

            // --- Validaciones de negocio, con mensajes claros ---
            var repuesto = await Repuestos.AsNoTracking().FirstOrDefaultAsync(r => r.RepuestoId == repuestoId);

            if (repuesto == null || !repuesto.EstaActivo)
            {
                ModelState.AddModelError(nameof(modelo.RepuestoId), "El repuesto no existe o está dado de baja.");
                return await VolverASalidaAsync(modelo);
            }

            if (repuesto.CantidadStock < cantidad)
            {
                ModelState.AddModelError(nameof(modelo.Cantidad),
                    $"Existencias insuficientes: solo hay {repuesto.CantidadStock} unidades de \"{repuesto.Nombre}\".");
                return await VolverASalidaAsync(modelo);
            }

            var orden = await Ordenes.AsNoTracking().FirstOrDefaultAsync(o => o.OrdenServicioId == ordenId);

            if (orden == null)
            {
                ModelState.AddModelError(nameof(modelo.OrdenServicioId), "La orden de servicio no existe.");
                return await VolverASalidaAsync(modelo);
            }

            if (orden.Estado != "Abierta" && orden.Estado != "EnProceso")
            {
                ModelState.AddModelError(nameof(modelo.OrdenServicioId),
                    $"La orden está \"{orden.Estado}\". Solo se pueden registrar salidas en órdenes abiertas o en proceso.");
                return await VolverASalidaAsync(modelo);
            }

            // --- Registro: resta de stock + movimiento, todo o nada ---
            await using var transaccion = await _contexto.Database.BeginTransactionAsync();

            // La resta solo se aplica si todavía hay existencias suficientes. Si otra persona
            // usó las unidades un instante antes, no se modifica nada (y el stock nunca queda negativo).
            var filasAfectadas = await Repuestos
                .Where(r => r.RepuestoId == repuestoId && r.EstaActivo && r.CantidadStock >= cantidad)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.CantidadStock, r => r.CantidadStock - cantidad));

            if (filasAfectadas == 0)
            {
                ModelState.AddModelError(string.Empty, "Las existencias cambiaron mientras registrabas la salida. Revisa el stock e intenta de nuevo.");
                return await VolverASalidaAsync(modelo);
            }

            Movimientos.Add(new MovimientosInventario
            {
                RepuestoId = repuestoId,
                TipoMovimiento = "Salida",
                Cantidad = cantidad,
                OrdenServicioId = ordenId,
                UsuarioId = usuarioId.Value,
                Motivo = modelo.Motivo,
                FechaMovimiento = DateTime.Now
            });

            await _contexto.SaveChangesAsync();

            var stockRestante = await Repuestos.AsNoTracking()
                .Where(r => r.RepuestoId == repuestoId)
                .Select(r => r.CantidadStock)
                .FirstAsync();

            await transaccion.CommitAsync();

            TempData["Exito"] = $"Salida registrada: {cantidad} × \"{repuesto.Nombre}\" en la orden #{ordenId:D4}. Existencias restantes: {stockRestante}.";
            return RedirectToAction(nameof(Salida));
        }

        // =====================================================
        // Auxiliares
        // =====================================================
        private async Task<IActionResult> VolverAEntradaAsync(MovimientoEntradaViewModel modelo)
        {
            modelo.Repuestos = await ObtenerOpcionesAsync(soloActivos: true);
            return View(modelo);
        }

        private async Task<IActionResult> VolverASalidaAsync(MovimientoSalidaViewModel modelo)
        {
            modelo.Repuestos = await ObtenerOpcionesAsync(soloActivos: true);
            modelo.Ordenes = await ObtenerOrdenesAbiertasAsync();
            return View(modelo);
        }

        private async Task<List<RepuestoOpcionViewModel>> ObtenerOpcionesAsync(bool soloActivos)
        {
            var consulta = Repuestos.AsNoTracking().AsQueryable();

            if (soloActivos)
            {
                consulta = consulta.Where(r => r.EstaActivo);
            }

            return await consulta
                .OrderBy(r => r.Nombre)
                .Select(r => new RepuestoOpcionViewModel
                {
                    RepuestoId = r.RepuestoId,
                    Nombre = r.Nombre,
                    CantidadStock = r.CantidadStock
                })
                .ToListAsync();
        }

        private async Task<List<OrdenOpcionViewModel>> ObtenerOrdenesAbiertasAsync()
        {
            var ordenes = await Ordenes.AsNoTracking()
                .Where(o => o.Estado == "Abierta" || o.Estado == "EnProceso")
                .OrderByDescending(o => o.FechaIngreso)
                .Select(o => new { o.OrdenServicioId, o.Estado, o.Descripcion })
                .ToListAsync();

            return ordenes
                .Select(o =>
                {
                    var estado = o.Estado == "EnProceso" ? "En proceso" : o.Estado;
                    var descripcion = string.IsNullOrWhiteSpace(o.Descripcion)
                        ? ""
                        : " · " + (o.Descripcion.Length > 40 ? o.Descripcion[..40] + "…" : o.Descripcion);

                    return new OrdenOpcionViewModel
                    {
                        OrdenServicioId = o.OrdenServicioId,
                        Etiqueta = $"#{o.OrdenServicioId:D4} · {estado}{descripcion}"
                    };
                })
                .ToList();
        }

        // Identifica quién registra el movimiento (queda guardado para auditoría).
        // Busca primero el Id en los claims de la sesión y, si no está, por nombre de usuario o correo.
        private async Task<int?> ObtenerUsuarioActualIdAsync()
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(claimId, out var id) &&
                await _contexto.Usuarios.AnyAsync(u => u.UsuarioId == id))
            {
                return id;
            }

            var nombre = User.Identity?.Name;

            if (!string.IsNullOrWhiteSpace(nombre))
            {
                var usuario = await _contexto.Usuarios.AsNoTracking().FirstOrDefaultAsync(u =>
                    u.NombreUsuario == nombre || u.Correo == nombre || u.NombreCompleto == nombre);

                return usuario?.UsuarioId;
            }

            return null;
        }
    }
}