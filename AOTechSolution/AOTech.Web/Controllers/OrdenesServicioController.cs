using System.Security.Claims;
using AOTech.Data.Contexto;
using AOTech.Data.Modelos;
using AOTech.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AOTech.Web.Controllers
{
    [Authorize(Roles = "Administrador,Recepcionista")]
    public class OrdenesServicioController : Controller
    {
        private readonly AOTechDbContext _contexto;

        public OrdenesServicioController(AOTechDbContext contexto)
        {
            _contexto = contexto;
        }

        // =====================================================
        // LISTADO DE ÓRDENES DE SERVICIO
        // =====================================================
        public async Task<IActionResult> Index()
        {
            var ordenes = await _contexto.OrdenesServicios
                .Include(o => o.Cliente).ThenInclude(c => c.Usuario)
                .Include(o => o.Vehiculo)
                .Include(o => o.RecepcionistaUsuario)
                .OrderByDescending(o => o.FechaCreacion)
                .Select(o => new OrdenServicioListaViewModel
                {
                    OrdenServicioId = o.OrdenServicioId,
                    Cliente = o.Cliente.Usuario.NombreCompleto,
                    Vehiculo = o.Vehiculo.Placa + " - " + o.Vehiculo.Marca + " " + o.Vehiculo.Modelo,
                    Estado = o.Estado,
                    FechaIngreso = o.FechaIngreso,
                    FechaEntregaEstimada = o.FechaEntregaEstimada,
                    MontoTotal = o.MontoTotal,
                    Recepcionista = o.RecepcionistaUsuario.NombreCompleto
                })
                .ToListAsync();

            return View(ordenes);
        }

        // =====================================================
        // CREAR — GET
        // =====================================================
        public async Task<IActionResult> Crear()
        {
            var modelo = new OrdenServicioCrearViewModel
            {
                ClientesDisponibles = await ObtenerClientesAsync(),
                VehiculosDisponibles = await ObtenerVehiculosAsync()
            };
            return View(modelo);
        }

        // =====================================================
        // CREAR — POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(OrdenServicioCrearViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                modelo.ClientesDisponibles = await ObtenerClientesAsync();
                modelo.VehiculosDisponibles = await ObtenerVehiculosAsync();
                return View(modelo);
            }

            var clienteExiste = await _contexto.Clientes.AnyAsync(c => c.ClienteId == modelo.ClienteId);
            if (!clienteExiste)
            {
                ModelState.AddModelError(nameof(modelo.ClienteId), "El cliente seleccionado no es válido.");
                modelo.ClientesDisponibles = await ObtenerClientesAsync();
                modelo.VehiculosDisponibles = await ObtenerVehiculosAsync();
                return View(modelo);
            }

            var vehiculo = await _contexto.Vehiculos
                .FirstOrDefaultAsync(v => v.VehiculoId == modelo.VehiculoId);

            if (vehiculo == null)
            {
                ModelState.AddModelError(nameof(modelo.VehiculoId), "El vehículo seleccionado no es válido.");
                modelo.ClientesDisponibles = await ObtenerClientesAsync();
                modelo.VehiculosDisponibles = await ObtenerVehiculosAsync();
                return View(modelo);
            }

            if (vehiculo.ClienteId != modelo.ClienteId)
            {
                ModelState.AddModelError(nameof(modelo.VehiculoId), "El vehículo seleccionado no pertenece al cliente indicado.");
                modelo.ClientesDisponibles = await ObtenerClientesAsync();
                modelo.VehiculosDisponibles = await ObtenerVehiculosAsync();
                return View(modelo);
            }

            var recepcionistaUsuarioId = ObtenerUsuarioIdAutenticado();
            if (recepcionistaUsuarioId == null)
            {
                ModelState.AddModelError(string.Empty, "No se pudo identificar al usuario autenticado.");
                modelo.ClientesDisponibles = await ObtenerClientesAsync();
                modelo.VehiculosDisponibles = await ObtenerVehiculosAsync();
                return View(modelo);
            }

            var recepcionistaExiste = await _contexto.Usuarios
                .AnyAsync(u => u.UsuarioId == recepcionistaUsuarioId.Value && u.EstaActivo);
            if (!recepcionistaExiste)
            {
                ModelState.AddModelError(string.Empty, "El usuario autenticado no es válido o está inactivo.");
                modelo.ClientesDisponibles = await ObtenerClientesAsync();
                modelo.VehiculosDisponibles = await ObtenerVehiculosAsync();
                return View(modelo);
            }

            var orden = new OrdenesServicio
            {
                ClienteId = modelo.ClienteId,
                VehiculoId = modelo.VehiculoId,
                RecepcionistaUsuarioId = recepcionistaUsuarioId.Value,
                Descripcion = modelo.Descripcion,
                FechaIngreso = modelo.FechaIngreso,
                FechaEntregaEstimada = modelo.FechaEntregaEstimada,
                Estado = "Abierta",
                MontoTotal = 0,
                FechaCreacion = DateTime.Now
            };

            _contexto.OrdenesServicios.Add(orden);
            await _contexto.SaveChangesAsync();

            TempData["Exito"] = "Orden de servicio registrada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // EDITAR — GET
        // =====================================================
        public async Task<IActionResult> Editar(int id)
        {
            var orden = await _contexto.OrdenesServicios
                .Include(o => o.Cliente).ThenInclude(c => c.Usuario)
                .Include(o => o.Vehiculo)
                .FirstOrDefaultAsync(o => o.OrdenServicioId == id);

            if (orden == null)
            {
                TempData["Error"] = "La orden de servicio no existe.";
                return RedirectToAction(nameof(Index));
            }

            var modelo = new OrdenServicioEditarViewModel
            {
                OrdenServicioId = orden.OrdenServicioId,
                Cliente = orden.Cliente.Usuario.NombreCompleto,
                Vehiculo = orden.Vehiculo.Placa + " - " + orden.Vehiculo.Marca + " " + orden.Vehiculo.Modelo,
                FechaIngreso = orden.FechaIngreso,
                Descripcion = orden.Descripcion ?? string.Empty,
                FechaEntregaEstimada = orden.FechaEntregaEstimada,
                Estado = orden.Estado,
                EstadosDisponibles = ObtenerEstadosDisponibles()
            };

            return View(modelo);
        }

        // =====================================================
        // EDITAR — POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, OrdenServicioEditarViewModel modelo)
        {
            if (id != modelo.OrdenServicioId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                modelo.EstadosDisponibles = ObtenerEstadosDisponibles();
                return View(modelo);
            }

            if (!ObtenerEstadosDisponibles().Contains(modelo.Estado))
            {
                ModelState.AddModelError(nameof(modelo.Estado), "El estado seleccionado no es válido.");
                modelo.EstadosDisponibles = ObtenerEstadosDisponibles();
                return View(modelo);
            }

            var orden = await _contexto.OrdenesServicios
                .Include(o => o.Cliente).ThenInclude(c => c.Usuario)
                .Include(o => o.Vehiculo)
                .FirstOrDefaultAsync(o => o.OrdenServicioId == id);

            if (orden == null)
            {
                TempData["Error"] = "La orden de servicio no existe.";
                return RedirectToAction(nameof(Index));
            }

            // Solo se permite modificar descripción, fecha de entrega estimada y estado.
            // Cliente, vehículo, recepcionista, fecha de ingreso y monto no son editables aquí.
            orden.Descripcion = modelo.Descripcion;
            orden.FechaEntregaEstimada = modelo.FechaEntregaEstimada;
            orden.Estado = modelo.Estado;
            orden.FechaActualizacion = DateTime.Now;

            await _contexto.SaveChangesAsync();

            TempData["Exito"] = "Orden de servicio actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // Auxiliares
        // =====================================================
        private static List<string> ObtenerEstadosDisponibles()
        {
            return new List<string> { "Abierta", "En Proceso", "Finalizada", "Cancelada" };
        }

        private int? ObtenerUsuarioIdAutenticado()
        {
            var valor = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(valor, out var id) ? id : null;
        }

        private async Task<List<ClienteSelectViewModel>> ObtenerClientesAsync()
        {
            return await _contexto.Clientes
                .Include(c => c.Usuario)
                .Where(c => c.EstaActivo)
                .OrderBy(c => c.Usuario.NombreCompleto)
                .Select(c => new ClienteSelectViewModel
                {
                    ClienteId = c.ClienteId,
                    NombreCompleto = c.Usuario.NombreCompleto
                })
                .ToListAsync();
        }

        private async Task<List<VehiculoSelectViewModel>> ObtenerVehiculosAsync()
        {
            return await _contexto.Vehiculos
                .Where(v => v.EstaActivo)
                .OrderBy(v => v.Placa)
                .Select(v => new VehiculoSelectViewModel
                {
                    VehiculoId = v.VehiculoId,
                    ClienteId = v.ClienteId,
                    Placa = v.Placa,
                    Marca = v.Marca,
                    Modelo = v.Modelo
                })
                .ToListAsync();
        }
    }
}
