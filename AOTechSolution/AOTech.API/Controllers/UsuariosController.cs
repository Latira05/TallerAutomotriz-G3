using AOTech.Data.Contexto;
using AOTech.Data.Modelos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

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

    // =========================================================
    // OBTENER USUARIOS
    // =========================================================
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
                u.FechaActualizacion,
                Rol = u.Rol != null ? u.Rol.NombreRol : null
            })
            .ToListAsync();

        return Ok(usuarios);
    }


    // =========================================================
    // CREAR USUARIO
    // =========================================================
    [HttpPost]
    public async Task<IActionResult> CrearUsuario(Usuario usuario)
    {
        // Validar que el rol exista
        var rol = await _contexto.Roles
            .FirstOrDefaultAsync(r => r.RolId == usuario.RolId);

        if (rol == null)
        {
            return BadRequest(new
            {
                mensaje = "El rol seleccionado no es válido."
            });
        }

        // Validar que el correo no esté registrado
        var existeCorreo = await _contexto.Usuarios
            .AnyAsync(u => u.Correo == usuario.Correo);

        if (existeCorreo)
        {
            return BadRequest(new
            {
                mensaje = "El correo ya está registrado."
            });
        }

        // Generar salt
        byte[] salt = RandomNumberGenerator.GetBytes(16);

        // Generar hash
        using var sha256 = SHA256.Create();

        byte[] passwordBytes =
            Encoding.UTF8.GetBytes(usuario.ContrasenaHash.ToString());

        byte[] hash = sha256.ComputeHash(
            passwordBytes.Concat(salt).ToArray()
        );

        usuario.ContrasenaSalt = salt;
        usuario.ContrasenaHash = hash;

        // Datos iniciales
        usuario.EstaActivo = true;
        usuario.FechaCreacion = DateTime.Now;

        _contexto.Usuarios.Add(usuario);

        await _contexto.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Usuario creado correctamente.",
            usuarioId = usuario.UsuarioId,
            rol = rol.NombreRol
        });
    }


    // =========================================================
    // CAMBIAR ROL
    // =========================================================
    [HttpPut("{id}/rol")]
    public async Task<IActionResult> CambiarRol(int id, int rolId)
    {
        // Validar que el usuario exista
        var usuario = await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.UsuarioId == id);

        if (usuario == null)
        {
            return NotFound(new
            {
                mensaje = "El usuario no existe."
            });
        }

        // Validar que el nuevo rol exista
        var rol = await _contexto.Roles
            .FirstOrDefaultAsync(r => r.RolId == rolId);

        if (rol == null)
        {
            return BadRequest(new
            {
                mensaje = "El rol seleccionado no es válido."
            });
        }

        // Cambiar rol
        usuario.RolId = rolId;
        usuario.FechaActualizacion = DateTime.Now;

        await _contexto.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Rol cambiado correctamente.",
            usuarioId = usuario.UsuarioId,
            usuario = usuario.NombreUsuario,
            nuevoRol = rol.NombreRol
        });
    }


    // =========================================================
    // DESACTIVAR USUARIO
    // =========================================================
    [HttpPut("{id}/desactivar")]
    public async Task<IActionResult> DesactivarUsuario(int id)
    {
        // Validar que el usuario exista
        var usuario = await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.UsuarioId == id);

        if (usuario == null)
        {
            return NotFound(new
            {
                mensaje = "El usuario no existe."
            });
        }

        // Validar que esté activo
        if (!usuario.EstaActivo)
        {
            return BadRequest(new
            {
                mensaje = "El usuario ya se encuentra desactivado."
            });
        }

        // Desactivar
        usuario.EstaActivo = false;
        usuario.FechaActualizacion = DateTime.Now;

        await _contexto.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Usuario desactivado correctamente.",
            usuarioId = usuario.UsuarioId,
            usuario = usuario.NombreUsuario,
            estaActivo = usuario.EstaActivo
        });
    }
}