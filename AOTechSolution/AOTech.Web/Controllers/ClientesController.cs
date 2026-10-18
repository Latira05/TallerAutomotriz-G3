using System.Security.Cryptography;
using System.Text;
using AOTech.Data.Contexto;
using AOTech.Data.Modelos;
using AOTech.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AOTech.Web.Controllers
{
    // [Authorize(Roles = "Administrador,Recepcionista")] DESACTIVADO PARA PRUEBAS
    public class ClientesController : Controller
    {
        private readonly AOTechDbContext _contexto;

        // Nombre del rol que se asigna automáticamente a la cuenta de cada cliente
        private const string NombreRolCliente = "Cliente";

        public ClientesController(AOTechDbContext contexto)
        {
            _contexto = contexto;
        }

        // =====================================================
        // LISTADO DE CLIENTES (con búsqueda opcional)
        // =====================================================
        public async Task<IActionResult> Index(string? buscar)
        {
            var consulta = _contexto.Clientes
                .Join(_contexto.Usuarios,
                      c => c.UsuarioId,
                      u => u.UsuarioId,
                      (c, u) => new { c, u });

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var termino = buscar.Trim();
                consulta = consulta.Where(x =>
                    x.c.CodigoClienteFrecuente.Contains(termino) ||
                    x.u.NombreCompleto.Contains(termino) ||
                    x.u.Correo.Contains(termino) ||
                    (x.c.Telefono != null && x.c.Telefono.Contains(termino)));
            }

            var clientes = await consulta
                .OrderBy(x => x.u.NombreCompleto)
                .Select(x => new ClienteListaViewModel
                {
                    ClienteId = x.c.ClienteId,
                    CodigoClienteFrecuente = x.c.CodigoClienteFrecuente,
                    NombreCompleto = x.u.NombreCompleto,
                    NombreUsuario = x.u.NombreUsuario,
                    Correo = x.u.Correo,
                    Telefono = x.c.Telefono,
                    Direccion = x.c.Direccion,
                    EstaActivo = x.c.EstaActivo,
                    FechaCreacion = x.c.FechaCreacion
                })
                .ToListAsync();

            ViewBag.Buscar = buscar;
            return View(clientes);
        }

        // =====================================================
        // REGISTRAR — GET
        // =====================================================
        public IActionResult Crear()
        {
            return View(new ClienteCrearViewModel());
        }

        // =====================================================
        // REGISTRAR — POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ClienteCrearViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var rolCliente = await _contexto.Roles
                .FirstOrDefaultAsync(r => r.NombreRol == NombreRolCliente);

            if (rolCliente == null)
            {
                ModelState.AddModelError(string.Empty,
                    "No existe el rol \"Cliente\" en la tabla Roles. Revisa los datos semilla de la base de datos.");
                return View(modelo);
            }

            if (await _contexto.Usuarios.AnyAsync(u => u.Correo == modelo.Correo))
            {
                ModelState.AddModelError(nameof(modelo.Correo), "El correo ya está registrado.");
                return View(modelo);
            }

            if (await _contexto.Usuarios.AnyAsync(u => u.NombreUsuario == modelo.NombreUsuario))
            {
                ModelState.AddModelError(nameof(modelo.NombreUsuario), "El nombre de usuario ya está en uso.");
                return View(modelo);
            }

            var (hash, salt) = GenerarHash(modelo.Contrasena);

            // Todo o nada: si algo falla a la mitad, no queda un usuario "huérfano" sin cliente.
            await using var transaccion = await _contexto.Database.BeginTransactionAsync();

            try
            {
                // 1) Cuenta de acceso
                var usuario = new Usuario
                {
                    RolId = rolCliente.RolId,
                    NombreUsuario = modelo.NombreUsuario,
                    Correo = modelo.Correo,
                    NombreCompleto = modelo.NombreCompleto,
                    ContrasenaHash = hash,
                    ContrasenaSalt = salt,
                    EstaActivo = true,
                    FechaCreacion = DateTime.Now
                };

                _contexto.Usuarios.Add(usuario);
                await _contexto.SaveChangesAsync();

                // 2) Cliente (con un código temporal único, porque la columna es NOT NULL)
                var cliente = new Cliente
                {
                    UsuarioId = usuario.UsuarioId,
                    CodigoClienteFrecuente = "TMP-" + Guid.NewGuid().ToString("N")[..12],
                    Telefono = modelo.Telefono,
                    Direccion = modelo.Direccion,
                    EstaActivo = true,
                    FechaCreacion = DateTime.Now
                };

                _contexto.Clientes.Add(cliente);
                await _contexto.SaveChangesAsync();

                // 3) Código definitivo de cliente frecuente, basado en el Id (CF-00001, CF-00002...)
                cliente.CodigoClienteFrecuente = $"CF-{cliente.ClienteId:D5}";
                await _contexto.SaveChangesAsync();

                await transaccion.CommitAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "No se pudo registrar el cliente. Intenta de nuevo.");
                return View(modelo);
            }

            TempData["Exito"] = "Cliente registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // EDITAR — GET
        // =====================================================
        public async Task<IActionResult> Editar(int id)
        {
            var cliente = await _contexto.Clientes.FirstOrDefaultAsync(c => c.ClienteId == id);

            if (cliente == null)
            {
                TempData["Error"] = "El cliente no existe.";
                return RedirectToAction(nameof(Index));
            }

            var usuario = await _contexto.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == cliente.UsuarioId);

            if (usuario == null)
            {
                TempData["Error"] = "El cliente no tiene una cuenta de usuario asociada.";
                return RedirectToAction(nameof(Index));
            }

            var modelo = new ClienteEditarViewModel
            {
                ClienteId = cliente.ClienteId,
                CodigoClienteFrecuente = cliente.CodigoClienteFrecuente,
                EstaActivo = cliente.EstaActivo,
                NombreCompleto = usuario.NombreCompleto,
                Telefono = cliente.Telefono,
                Direccion = cliente.Direccion,
                NombreUsuario = usuario.NombreUsuario,
                Correo = usuario.Correo
            };

            return View(modelo);
        }

        // =====================================================
        // EDITAR — POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(ClienteEditarViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var cliente = await _contexto.Clientes.FirstOrDefaultAsync(c => c.ClienteId == modelo.ClienteId);

            if (cliente == null)
            {
                TempData["Error"] = "El cliente no existe.";
                return RedirectToAction(nameof(Index));
            }

            var usuario = await _contexto.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == cliente.UsuarioId);

            if (usuario == null)
            {
                TempData["Error"] = "El cliente no tiene una cuenta de usuario asociada.";
                return RedirectToAction(nameof(Index));
            }

            // Correo y nombre de usuario únicos, ignorando al propio cliente
            if (await _contexto.Usuarios.AnyAsync(u => u.Correo == modelo.Correo && u.UsuarioId != usuario.UsuarioId))
            {
                ModelState.AddModelError(nameof(modelo.Correo), "El correo ya está registrado por otro usuario.");
                return View(modelo);
            }

            if (await _contexto.Usuarios.AnyAsync(u => u.NombreUsuario == modelo.NombreUsuario && u.UsuarioId != usuario.UsuarioId))
            {
                ModelState.AddModelError(nameof(modelo.NombreUsuario), "El nombre de usuario ya está en uso por otro usuario.");
                return View(modelo);
            }

            // Datos de la cuenta
            usuario.NombreCompleto = modelo.NombreCompleto;
            usuario.NombreUsuario = modelo.NombreUsuario;
            usuario.Correo = modelo.Correo;
            usuario.FechaActualizacion = DateTime.Now;

            // Datos del cliente
            cliente.Telefono = modelo.Telefono;
            cliente.Direccion = modelo.Direccion;

            // Un solo SaveChanges: se guardan ambos cambios juntos o ninguno
            await _contexto.SaveChangesAsync();

            TempData["Exito"] = "Datos del cliente actualizados correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // ACTIVAR / DESACTIVAR
        // Mantiene sincronizada la cuenta de acceso: un cliente
        // desactivado tampoco puede entrar al sistema.
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id, bool activarlo)
        {
            var cliente = await _contexto.Clientes.FirstOrDefaultAsync(c => c.ClienteId == id);

            if (cliente == null)
            {
                TempData["Error"] = "El cliente no existe.";
                return RedirectToAction(nameof(Index));
            }

            var usuario = await _contexto.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == cliente.UsuarioId);

            cliente.EstaActivo = activarlo;

            if (usuario != null)
            {
                usuario.EstaActivo = activarlo;
                usuario.FechaActualizacion = DateTime.Now;
            }

            await _contexto.SaveChangesAsync();

            TempData["Exito"] = activarlo
                ? "Cliente reactivado correctamente."
                : "Cliente desactivado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // Auxiliares
        // =====================================================
        private static (byte[] hash, byte[] salt) GenerarHash(string contrasena)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(contrasena);
            byte[] hash = SHA256.HashData(passwordBytes.Concat(salt).ToArray());
            return (hash, salt);
        }
    }
}