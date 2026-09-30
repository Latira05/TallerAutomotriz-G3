using AOTech.Data.Contexto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class UsuariosController : ControllerBase
{
    private readonly AOTechDbContext _contexto;

    public UsuariosController(AOTechDbContext contexto)
    {
        _contexto = contexto;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsuarios()
    {
        var usuarios = await _contexto.Usuarios
            .Include(u => u.Rol)
            .Select(u => new
            {
                u.UsuarioId,
                u.NombreUsuario,
                u.Correo,
                u.NombreCompleto,
                u.EstaActivo,
                u.FechaCreacion,
                Rol = u.Rol != null ? u.Rol.NombreRol : null
            })
            .ToListAsync();

        return Ok(usuarios);
    }
}