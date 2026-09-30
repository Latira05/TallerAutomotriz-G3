using AOTech.Data.Contexto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AOTech.Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : Controller
    {
        private readonly AOTechDbContext _contexto;

        public UsuariosController(AOTechDbContext contexto)
        {
            _contexto = contexto;
        }

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