using AOTech.Data.Contexto;
using AOTech.Data.Modelos;
using AOTech.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AOTech.Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class RepuestosController : Controller
    {
        private readonly AOTechDbContext _contexto;

        public RepuestosController(AOTechDbContext contexto)
        {
            _contexto = contexto;
        }

        // Acceso corto a la tabla Repuestos
        private DbSet<Repuesto> Repuestos => _contexto.Set<Repuesto>();

        // =====================================================
        // LISTADO (activos / stock bajo / de baja) con búsqueda
        // =====================================================
        public async Task<IActionResult> Index(string? buscar, string? vista)
        {
            vista = vista is "bajo" or "baja" ? vista : "activos";

            // Contadores de las pestañas (no dependen de la búsqueda)
            var totalActivos = await Repuestos.CountAsync(r => r.EstaActivo);
            var totalBajo = await Repuestos.CountAsync(r => r.EstaActivo && r.CantidadStock <= r.StockMinimo);
            var totalBaja = await Repuestos.CountAsync(r => !r.EstaActivo);

            IQueryable<Repuesto> consulta = Repuestos.AsNoTracking();

            consulta = vista switch
            {
                "bajo" => consulta.Where(r => r.EstaActivo && r.CantidadStock <= r.StockMinimo),
                "baja" => consulta.Where(r => !r.EstaActivo),
                _ => consulta.Where(r => r.EstaActivo)
            };

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var termino = buscar.Trim();
                consulta = consulta.Where(r =>
                    r.Nombre.Contains(termino) ||
                    (r.Descripcion != null && r.Descripcion.Contains(termino)));
            }

            // En "stock bajo" lo más urgente (menos existencias respecto al mínimo) va primero
            IOrderedQueryable<Repuesto> ordenada = vista == "bajo"
                ? consulta.OrderBy(r => r.CantidadStock - r.StockMinimo).ThenBy(r => r.Nombre)
                : consulta.OrderBy(r => r.Nombre);

            var repuestos = await ordenada
                .Select(r => new RepuestoListaViewModel
                {
                    RepuestoId = r.RepuestoId,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion,
                    PrecioUnitario = r.PrecioUnitario,
                    CantidadStock = r.CantidadStock,
                    StockMinimo = r.StockMinimo,
                    EstaActivo = r.EstaActivo
                })
                .ToListAsync();

            return View(new RepuestoIndexViewModel
            {
                Repuestos = repuestos,
                Buscar = buscar,
                Vista = vista,
                TotalActivos = totalActivos,
                TotalBajoStock = totalBajo,
                TotalDeBaja = totalBaja
            });
        }

        // =====================================================
        // REGISTRAR — GET
        // =====================================================
        public IActionResult Crear()
        {
            return View(new RepuestoCrearViewModel { StockMinimo = 0 });
        }

        // =====================================================
        // REGISTRAR — POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(RepuestoCrearViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var nombre = modelo.Nombre.Trim();

            if (await Repuestos.AnyAsync(r => r.Nombre == nombre))
            {
                ModelState.AddModelError(nameof(modelo.Nombre), "Ya existe un repuesto con ese nombre.");
                return View(modelo);
            }

            var repuesto = new Repuesto
            {
                Nombre = nombre,
                Descripcion = modelo.Descripcion,
                PrecioUnitario = modelo.PrecioUnitario!.Value,
                CantidadStock = 0,                       // las existencias entran con "Registrar ingreso"
                StockMinimo = modelo.StockMinimo!.Value,
                EstaActivo = true,
                FechaCreacion = DateTime.Now
            };

            Repuestos.Add(repuesto);
            await _contexto.SaveChangesAsync();

            TempData["Exito"] = $"Repuesto \"{nombre}\" registrado. Usa \"Ingreso\" para cargar sus existencias.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // EDITAR — GET
        // =====================================================
        public async Task<IActionResult> Editar(int id)
        {
            var repuesto = await Repuestos.AsNoTracking().FirstOrDefaultAsync(r => r.RepuestoId == id);

            if (repuesto == null)
            {
                TempData["Error"] = "El repuesto no existe.";
                return RedirectToAction(nameof(Index));
            }

            return View(new RepuestoEditarViewModel
            {
                RepuestoId = repuesto.RepuestoId,
                Nombre = repuesto.Nombre,
                Descripcion = repuesto.Descripcion,
                PrecioUnitario = repuesto.PrecioUnitario,
                StockMinimo = repuesto.StockMinimo,
                CantidadStock = repuesto.CantidadStock,
                EstaActivo = repuesto.EstaActivo
            });
        }

        // =====================================================
        // EDITAR — POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(RepuestoEditarViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var repuesto = await Repuestos.FirstOrDefaultAsync(r => r.RepuestoId == modelo.RepuestoId);

            if (repuesto == null)
            {
                TempData["Error"] = "El repuesto no existe.";
                return RedirectToAction(nameof(Index));
            }

            var nombre = modelo.Nombre.Trim();

            if (await Repuestos.AnyAsync(r => r.Nombre == nombre && r.RepuestoId != repuesto.RepuestoId))
            {
                ModelState.AddModelError(nameof(modelo.Nombre), "Ya existe otro repuesto con ese nombre.");
                return View(modelo);
            }

            // Las existencias NO se tocan aquí: solo cambian con ingresos y salidas.
            repuesto.Nombre = nombre;
            repuesto.Descripcion = modelo.Descripcion;
            repuesto.PrecioUnitario = modelo.PrecioUnitario!.Value;
            repuesto.StockMinimo = modelo.StockMinimo!.Value;

            await _contexto.SaveChangesAsync();

            TempData["Exito"] = "Repuesto actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // DAR DE BAJA / REACTIVAR
        // (baja lógica: el repuesto y su historial se conservan)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id, bool activarlo)
        {
            var repuesto = await Repuestos.FirstOrDefaultAsync(r => r.RepuestoId == id);

            if (repuesto == null)
            {
                TempData["Error"] = "El repuesto no existe.";
                return RedirectToAction(nameof(Index));
            }

            repuesto.EstaActivo = activarlo;
            await _contexto.SaveChangesAsync();

            TempData["Exito"] = activarlo
                ? "Repuesto reactivado correctamente."
                : "Repuesto dado de baja. Su historial se conserva.";

            return RedirectToAction(nameof(Index), new { vista = activarlo ? "activos" : "baja" });
        }

        // =====================================================
        // VALIDAR EXISTENCIAS (antes de autorizar su uso en una orden)
        // Es una consulta (GET): no modifica nada.
        // =====================================================
        public async Task<IActionResult> Validar(int? repuestoId, int? cantidad)
        {
            var modelo = new ValidarExistenciasViewModel
            {
                RepuestoId = repuestoId,
                Cantidad = cantidad ?? 1,
                Repuestos = await Repuestos.AsNoTracking()
                    .Where(r => r.EstaActivo)
                    .OrderBy(r => r.Nombre)
                    .Select(r => new RepuestoOpcionViewModel
                    {
                        RepuestoId = r.RepuestoId,
                        Nombre = r.Nombre,
                        CantidadStock = r.CantidadStock
                    })
                    .ToListAsync()
            };

            // Solo se evalúa cuando el usuario ya eligió repuesto y cantidad
            if (!repuestoId.HasValue || !cantidad.HasValue)
            {
                return View(modelo);
            }

            modelo.Consultado = true;

            if (cantidad.Value < 1)
            {
                modelo.Disponible = false;
                modelo.Mensaje = "La cantidad debe ser al menos 1.";
                return View(modelo);
            }

            var repuesto = await Repuestos.AsNoTracking().FirstOrDefaultAsync(r => r.RepuestoId == repuestoId.Value);

            if (repuesto == null)
            {
                modelo.Disponible = false;
                modelo.Mensaje = "El repuesto seleccionado no existe.";
                return View(modelo);
            }

            modelo.NombreRepuesto = repuesto.Nombre;
            modelo.StockActual = repuesto.CantidadStock;

            if (!repuesto.EstaActivo)
            {
                modelo.Disponible = false;
                modelo.Mensaje = "Este repuesto está dado de baja y no se puede usar en órdenes de servicio.";
            }
            else if (repuesto.CantidadStock >= cantidad.Value)
            {
                modelo.Disponible = true;
                modelo.Mensaje = $"Hay {repuesto.CantidadStock} unidades en stock: alcanza para las {cantidad.Value} solicitadas. Puedes autorizar su uso.";
            }
            else
            {
                modelo.Disponible = false;
                var faltan = cantidad.Value - repuesto.CantidadStock;
                modelo.Mensaje = $"Solo hay {repuesto.CantidadStock} unidades en stock; faltan {faltan} para cubrir las {cantidad.Value} solicitadas.";
            }

            return View(modelo);
        }
    }
}