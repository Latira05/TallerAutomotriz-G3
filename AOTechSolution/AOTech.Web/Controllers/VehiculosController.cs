using AOTech.Data.Contexto;
using AOTech.Data.Modelos;
using AOTech.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AOTech.Web.Controllers
{
    [Authorize(Roles = "Administrador,Recepcionista")]
    public class VehiculosController : Controller
    {
        private readonly AOTechDbContext _contexto;

        public VehiculosController(AOTechDbContext contexto)
        {
            _contexto = contexto;
        }

        public async Task<IActionResult> Index(string? buscar)
        {
            var consulta = _contexto.Vehiculos
                .Include(v => v.Cliente)
                .ThenInclude(c => c.Usuario)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var termino = buscar.Trim();
                consulta = consulta.Where(v =>
                    v.Placa.Contains(termino) ||
                    v.Marca.Contains(termino) ||
                    v.Modelo.Contains(termino) ||
                    v.Cliente.CodigoClienteFrecuente.Contains(termino) ||
                    v.Cliente.Usuario.NombreCompleto.Contains(termino));
            }

            var vehiculos = await consulta
                .OrderBy(v => v.Placa)
                .Select(v => new VehiculoListaViewModel
                {
                    VehiculoId = v.VehiculoId,
                    Placa = v.Placa,
                    Marca = v.Marca,
                    Modelo = v.Modelo,
                    Anio = v.Anio,
                    Kilometraje = v.Kilometraje,
                    EstaActivo = v.EstaActivo,
                    FechaCreacion = v.FechaCreacion,
                    NombreCliente = v.Cliente.Usuario.NombreCompleto,
                    CodigoClienteFrecuente = v.Cliente.CodigoClienteFrecuente
                })
                .ToListAsync();

            ViewBag.Buscar = buscar;
            return View(vehiculos);
        }

        public async Task<IActionResult> Crear()
        {
            var modelo = new VehiculoCrearViewModel
            {
                ClientesDisponibles = await ObtenerClientesAsync()
            };

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(VehiculoCrearViewModel datos)
        {
            Console.WriteLine($"POST Vehiculos/Crear: ClienteId={datos.ClienteId}, Placa={datos.Placa}, Marca={datos.Marca}, Modelo={datos.Modelo}, Anio={datos.Anio}, Kilometraje={datos.Kilometraje}");

            if (!ModelState.IsValid)
            {
                foreach (var item in ModelState)
                {
                    foreach (var error in item.Value.Errors)
                    {
                        Console.WriteLine($"Validación de vehículo ({item.Key}): {error.ErrorMessage}");
                    }
                }

                datos.ClientesDisponibles = await ObtenerClientesAsync();
                return View(datos);
            }

            if (!await _contexto.Clientes.AnyAsync(c => c.ClienteId == datos.ClienteId))
            {
                ModelState.AddModelError(nameof(datos.ClienteId), "El cliente seleccionado no existe.");
                datos.ClientesDisponibles = await ObtenerClientesAsync();
                return View(datos);
            }

            var placa = datos.Placa.Trim();

            if (await _contexto.Vehiculos.AnyAsync(v => v.Placa == placa))
            {
                ModelState.AddModelError(nameof(datos.Placa), "Ya existe un vehículo con esa placa.");
                datos.ClientesDisponibles = await ObtenerClientesAsync();
                return View(datos);
            }

            var vehiculo = new Vehiculo
            {
                ClienteId = datos.ClienteId,
                Placa = placa,
                Marca = datos.Marca.Trim(),
                Modelo = datos.Modelo.Trim(),
                Anio = datos.Anio,
                Kilometraje = datos.Kilometraje,
                EstaActivo = true,
                FechaCreacion = DateTime.UtcNow
            };

            try
            {
                _contexto.Vehiculos.Add(vehiculo);
                await _contexto.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"No se pudo registrar el vehículo: {ex.InnerException?.Message ?? ex.Message}");
                ModelState.AddModelError(string.Empty, "No se pudo registrar el vehículo. Intenta de nuevo.");
                datos.ClientesDisponibles = await ObtenerClientesAsync();
                return View(datos);
            }

            TempData["Exito"] = "Vehículo registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Editar(int id)
        {
            var vehiculo = await _contexto.Vehiculos.FirstOrDefaultAsync(v => v.VehiculoId == id);

            if (vehiculo == null)
            {
                TempData["Error"] = "El vehículo no existe.";
                return RedirectToAction(nameof(Index));
            }

            var modelo = new VehiculoEditarViewModel
            {
                VehiculoId = vehiculo.VehiculoId,
                ClienteId = vehiculo.ClienteId,
                Placa = vehiculo.Placa,
                Marca = vehiculo.Marca,
                Modelo = vehiculo.Modelo,
                Anio = vehiculo.Anio,
                Kilometraje = vehiculo.Kilometraje,
                EstaActivo = vehiculo.EstaActivo,
                ClientesDisponibles = await ObtenerClientesAsync()
            };

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(VehiculoEditarViewModel datos)
        {
            if (!ModelState.IsValid)
            {
                datos.ClientesDisponibles = await ObtenerClientesAsync();
                return View(datos);
            }

            var vehiculo = await _contexto.Vehiculos.FirstOrDefaultAsync(v => v.VehiculoId == datos.VehiculoId);

            if (vehiculo == null)
            {
                TempData["Error"] = "El vehículo no existe.";
                return RedirectToAction(nameof(Index));
            }

            if (!await _contexto.Clientes.AnyAsync(c => c.ClienteId == datos.ClienteId))
            {
                ModelState.AddModelError(nameof(datos.ClienteId), "El cliente seleccionado no existe.");
                datos.ClientesDisponibles = await ObtenerClientesAsync();
                return View(datos);
            }

            if (await _contexto.Vehiculos.AnyAsync(v => v.Placa == datos.Placa && v.VehiculoId != datos.VehiculoId))
            {
                ModelState.AddModelError(nameof(datos.Placa), "Ya existe otro vehículo con esa placa.");
                datos.ClientesDisponibles = await ObtenerClientesAsync();
                return View(datos);
            }

            vehiculo.ClienteId = datos.ClienteId;
            vehiculo.Placa = datos.Placa.Trim();
            vehiculo.Marca = datos.Marca.Trim();
            vehiculo.Modelo = datos.Modelo.Trim();
            vehiculo.Anio = datos.Anio;
            vehiculo.Kilometraje = datos.Kilometraje;

            try
            {
                await _contexto.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "No se pudo actualizar el vehículo. Intenta de nuevo.");
                datos.ClientesDisponibles = await ObtenerClientesAsync();
                return View(datos);
            }

            TempData["Exito"] = "Vehículo actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Detalles(int id)
        {
            var vehiculo = await _contexto.Vehiculos
                .Include(v => v.Cliente)
                .ThenInclude(c => c.Usuario)
                .FirstOrDefaultAsync(v => v.VehiculoId == id);

            if (vehiculo == null)
            {
                TempData["Error"] = "El vehículo no existe.";
                return RedirectToAction(nameof(Index));
            }

            var modelo = new VehiculoDetalleViewModel
            {
                VehiculoId = vehiculo.VehiculoId,
                Placa = vehiculo.Placa,
                Marca = vehiculo.Marca,
                Modelo = vehiculo.Modelo,
                Anio = vehiculo.Anio,
                Kilometraje = vehiculo.Kilometraje,
                EstaActivo = vehiculo.EstaActivo,
                FechaCreacion = vehiculo.FechaCreacion,
                NombreCliente = vehiculo.Cliente.Usuario.NombreCompleto,
                CodigoClienteFrecuente = vehiculo.Cliente.CodigoClienteFrecuente
            };

            return View(modelo);
        }

        private async Task<List<ClienteViewModel>> ObtenerClientesAsync()
        {
            return await _contexto.Clientes
                .Include(c => c.Usuario)
                .OrderBy(c => c.Usuario.NombreCompleto)
                .Select(c => new ClienteViewModel
                {
                    ClienteId = c.ClienteId,
                    NombreCompleto = c.Usuario.NombreCompleto,
                    CodigoClienteFrecuente = c.CodigoClienteFrecuente
                })
                .ToListAsync();
        }
    }
}
