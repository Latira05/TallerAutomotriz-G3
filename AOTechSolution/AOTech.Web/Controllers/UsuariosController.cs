using System.Security.Cryptography;
using System.Text;
using AOTech.Data.Contexto;
using AOTech.Data.Modelos;
using AOTech.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AOTech.Web.Controllers
{
    // [Authorize(Roles = "Administrador")] DESACTIVADO PARA PRUEBAS
    public class UsuariosController : Controller
    {
        private readonly AOTechDbContext _contexto;

        public UsuariosController(AOTechDbContext contexto)
        {
            _contexto = contexto;
        }

        // =====================================================
        // LISTADO DE USUARIOS
        // =====================================================
        public async Task<IActionResult> Index()
        {
            var usuarios = await _contexto.Usuarios
                .Include(u => u.Rol)
                .OrderBy(u => u.NombreCompleto)
                .Select(u => new UsuarioListaViewModel
                {
                    UsuarioId = u.UsuarioId,
                    NombreUsuario = u.NombreUsuario,
                    Correo = u.Correo,
                    NombreCompleto = u.NombreCompleto,
                    EstaActivo = u.EstaActivo,
                    FechaCreacion = u.FechaCreacion,
                    FechaActualizacion = u.FechaActualizacion,
                    RolId = u.RolId,
                    Rol = u.Rol != null ? u.Rol.NombreRol : "Sin rol"
                })
                .ToListAsync();

            return View(usuarios);
        }

        // =====================================================
        // CREAR — GET
        // =====================================================
        public async Task<IActionResult> Crear()
        {
            var modelo = new UsuarioCrearViewModel
            {
                RolesDisponibles = await ObtenerRolesAsync()
            };
            return View(modelo);
        }

        // =====================================================
        // CREAR — POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(UsuarioCrearViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                modelo.RolesDisponibles = await ObtenerRolesAsync();
                return View(modelo);
            }

            var rolExiste = await _contexto.Roles.AnyAsync(r => r.RolId == modelo.RolId);
            if (!rolExiste)
            {
                ModelState.AddModelError(string.Empty, "El rol seleccionado no es válido.");
                modelo.RolesDisponibles = await ObtenerRolesAsync();
                return View(modelo);
            }

            var correoEnUso = await _contexto.Usuarios.AnyAsync(u => u.Correo == modelo.Correo);
            if (correoEnUso)
            {
                ModelState.AddModelError(nameof(modelo.Correo), "El correo ya está registrado.");
                modelo.RolesDisponibles = await ObtenerRolesAsync();
                return View(modelo);
            }

            var usuarioEnUso = await _contexto.Usuarios.AnyAsync(u => u.NombreUsuario == modelo.NombreUsuario);
            if (usuarioEnUso)
            {
                ModelState.AddModelError(nameof(modelo.NombreUsuario), "El nombre de usuario ya está en uso.");
                modelo.RolesDisponibles = await ObtenerRolesAsync();
                return View(modelo);
            }

            var (hash, salt) = GenerarHash(modelo.Contrasena);

            var usuario = new Usuario
            {
                RolId = modelo.RolId,
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

            TempData["Exito"] = "Usuario creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // EDITAR — GET
        // =====================================================
        public async Task<IActionResult> Editar(int id)
        {
            var usuario = await _contexto.Usuarios
                .FirstOrDefaultAsync(u => u.UsuarioId == id);

            if (usuario == null)
            {
                TempData["Error"] = "El usuario no existe.";
                return RedirectToAction(nameof(Index));
            }

            var modelo = new UsuarioEditarViewModel
            {
                UsuarioId = usuario.UsuarioId,
                NombreUsuario = usuario.NombreUsuario,
                Correo = usuario.Correo,
                NombreCompleto = usuario.NombreCompleto,
                RolId = usuario.RolId,
                EstaActivo = usuario.EstaActivo,
                RolesDisponibles = await ObtenerRolesAsync()
            };

            return View(modelo);
        }

        // =====================================================
        // EDITAR — POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, UsuarioEditarViewModel modelo)
        {
            if (id != modelo.UsuarioId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                modelo.RolesDisponibles = await ObtenerRolesAsync();
                return View(modelo);
            }

            var usuario = await _contexto.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == id);
            if (usuario == null)
            {
                TempData["Error"] = "El usuario no existe.";
                return RedirectToAction(nameof(Index));
            }

            var rolExiste = await _contexto.Roles.AnyAsync(r => r.RolId == modelo.RolId);
            if (!rolExiste)
            {
                ModelState.AddModelError(string.Empty, "El rol seleccionado no es válido.");
                modelo.RolesDisponibles = await ObtenerRolesAsync();
                return View(modelo);
            }

            var correoEnUso = await _contexto.Usuarios
                .AnyAsync(u => u.Correo == modelo.Correo && u.UsuarioId != id);
            if (correoEnUso)
            {
                ModelState.AddModelError(nameof(modelo.Correo), "El correo ya está registrado por otro usuario.");
                modelo.RolesDisponibles = await ObtenerRolesAsync();
                return View(modelo);
            }

            var nombreEnUso = await _contexto.Usuarios
                .AnyAsync(u => u.NombreUsuario == modelo.NombreUsuario && u.UsuarioId != id);
            if (nombreEnUso)
            {
                ModelState.AddModelError(nameof(modelo.NombreUsuario), "El nombre de usuario ya está en uso por otro usuario.");
                modelo.RolesDisponibles = await ObtenerRolesAsync();
                return View(modelo);
            }

            usuario.NombreUsuario = modelo.NombreUsuario;
            usuario.Correo = modelo.Correo;
            usuario.NombreCompleto = modelo.NombreCompleto;
            usuario.RolId = modelo.RolId;
            usuario.FechaActualizacion = DateTime.Now;

            await _contexto.SaveChangesAsync();

            TempData["Exito"] = "Usuario actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // CAMBIAR ESTADO (activar / desactivar)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id, bool activarlo)
        {
            Console.WriteLine("======================================");
            Console.WriteLine($"ID recibido: {id}");
            Console.WriteLine($"activarlo recibido: {activarlo}");

            var usuario = await _contexto.Usuarios
                .FirstOrDefaultAsync(u => u.UsuarioId == id);

            if (usuario == null)
            {
                Console.WriteLine("USUARIO NO ENCONTRADO");

                TempData["Error"] = "El usuario no existe.";
                return RedirectToAction(nameof(Index));
            }

            Console.WriteLine($"EstaActivo ANTES: {usuario.EstaActivo}");

            usuario.EstaActivo = activarlo;

            Console.WriteLine($"EstaActivo DESPUÉS: {usuario.EstaActivo}");

            usuario.FechaActualizacion = DateTime.Now;

            await _contexto.SaveChangesAsync();

            Console.WriteLine("CAMBIOS GUARDADOS");
            Console.WriteLine("======================================");

            TempData["Exito"] = activarlo
                ? "Usuario reactivado correctamente."
                : "Usuario desactivado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // Auxiliares
        // =====================================================
        private async Task<List<RolViewModel>> ObtenerRolesAsync()
        {
            return await _contexto.Roles
                .Select(r => new RolViewModel
                {
                    RolId = r.RolId,
                    NombreRol = r.NombreRol,
                    Descripcion = r.Descripcion
                })
                .ToListAsync();
        }

        private static (byte[] hash, byte[] salt) GenerarHash(string contrasena)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(contrasena);
            byte[] hash = SHA256.HashData(passwordBytes.Concat(salt).ToArray());
            return (hash, salt);
        }
    }
}