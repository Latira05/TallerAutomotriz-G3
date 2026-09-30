using AOTech.Data.Contexto;
using Microsoft.AspNetCore.Authorization;
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

      
        // LISTADO DE USUARIOS
       
        public async Task<IActionResult> Index()
        {
            var usuarios = await _contexto.Usuarios
                .Include(u => u.Rol)
                .Select(u => new UsuarioViewModel
                {
                    UsuarioId = u.UsuarioId,
                    NombreUsuario = u.NombreUsuario,
                    Correo = u.Correo,
                    NombreCompleto = u.NombreCompleto,
                    EstaActivo = u.EstaActivo,
                    FechaCreacion = u.FechaCreacion,
                    NombreRol = u.Rol != null ? u.Rol.NombreRol : "Sin rol"
                })
                .ToListAsync();

            return View(usuarios);
        }


        // =====================================================
        // CAMBIAR ROL
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarRol(int id, int rolId)
        {
            // Buscar usuario
            var usuario = await _contexto.Usuarios
                .FirstOrDefaultAsync(u => u.UsuarioId == id);

            if (usuario == null)
            {
                TempData["Error"] = "El usuario no existe.";
                return RedirectToAction(nameof(Index));
            }

            // Validar rol
            var rol = await _contexto.Roles
                .FirstOrDefaultAsync(r => r.RolId == rolId);

            if (rol == null)
            {
                TempData["Error"] = "El rol seleccionado no es válido.";
                return RedirectToAction(nameof(Index));
            }

            // Cambiar rol
            usuario.RolId = rolId;
            usuario.FechaActualizacion = DateTime.Now;

            await _contexto.SaveChangesAsync();

            TempData["Mensaje"] = "El rol del usuario fue cambiado correctamente.";

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // DESACTIVAR USUARIO
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id)
        {
            // Buscar usuario
            var usuario = await _contexto.Usuarios
                .FirstOrDefaultAsync(u => u.UsuarioId == id);

            if (usuario == null)
            {
                TempData["Error"] = "El usuario no existe.";
                return RedirectToAction(nameof(Index));
            }

            // Validar si ya está desactivado
            if (!usuario.EstaActivo)
            {
                TempData["Error"] = "El usuario ya se encuentra desactivado.";
                return RedirectToAction(nameof(Index));
            }

            // Desactivar usuario
            usuario.EstaActivo = false;
            usuario.FechaActualizacion = DateTime.Now;

            await _contexto.SaveChangesAsync();

            TempData["Mensaje"] = "El usuario fue desactivado correctamente.";

            return RedirectToAction(nameof(Index));
        }
    }


    public class UsuarioViewModel
    {
        public int UsuarioId { get; set; }

        public string NombreUsuario { get; set; } = string.Empty;

        public string Correo { get; set; } = string.Empty;

        public string NombreCompleto { get; set; } = string.Empty;

        public bool EstaActivo { get; set; }

        public DateTime FechaCreacion { get; set; }

        public string NombreRol { get; set; } = string.Empty;
    }
}