using AOTech.API.Dtos;
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
    // OBTENER USUARIOS (listado)
    // =========================================================
    [HttpGet]
    public async Task<IActionResult> GetUsuarios()
    {
        var usuarios = await _contexto.Usuarios
            .Include(u => u.Rol)
            .OrderBy(u => u.NombreCompleto)
            .Select(u => new
            {
                u.UsuarioId,
                u.NombreUsuario,
                u.Correo,
                u.NombreCompleto,
                u.EstaActivo,
                u.FechaCreacion,
                u.FechaActualizacion,
                u.RolId,
                Rol = u.Rol != null ? u.Rol.NombreRol : null
            })
            .ToListAsync();

        return Ok(usuarios);
    }

    // =========================================================
    // OBTENER UN USUARIO (para precargar el formulario de edición)
    // =========================================================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUsuario(int id)
    {
        var usuario = await _contexto.Usuarios
            .Include(u => u.Rol)
            .Where(u => u.UsuarioId == id)
            .Select(u => new
            {
                u.UsuarioId,
                u.NombreUsuario,
                u.Correo,
                u.NombreCompleto,
                u.EstaActivo,
                u.FechaCreacion,
                u.FechaActualizacion,
                u.RolId,
                Rol = u.Rol != null ? u.Rol.NombreRol : null
            })
            .FirstOrDefaultAsync();

        if (usuario == null)
        {
            return NotFound(new { mensaje = "El usuario no existe." });
        }

        return Ok(usuario);
    }

    // =========================================================
    // CREAR USUARIO
    // =========================================================
    [HttpPost]
    public async Task<IActionResult> CrearUsuario(UsuarioCrearDto datos)
    {
        // Validar que el rol exista
        var rol = await _contexto.Roles
            .FirstOrDefaultAsync(r => r.RolId == datos.RolId);

        if (rol == null)
        {
            return BadRequest(new { mensaje = "El rol seleccionado no es válido." });
        }

        // Validar que el correo no esté registrado
        var existeCorreo = await _contexto.Usuarios
            .AnyAsync(u => u.Correo == datos.Correo);

        if (existeCorreo)
        {
            return BadRequest(new { mensaje = "El correo ya está registrado." });
        }

        // Validar que el nombre de usuario no esté registrado
        var existeUsuario = await _contexto.Usuarios
            .AnyAsync(u => u.NombreUsuario == datos.NombreUsuario);

        if (existeUsuario)
        {
            return BadRequest(new { mensaje = "El nombre de usuario ya está en uso." });
        }

        if (string.IsNullOrWhiteSpace(datos.Contrasena) || datos.Contrasena.Length < 6)
        {
            return BadRequest(new { mensaje = "La contraseña debe tener al menos 6 caracteres." });
        }

        // Generar salt
        byte[] salt = RandomNumberGenerator.GetBytes(16);

        // Generar hash a partir de la contraseña real en texto plano + salt
        byte[] passwordBytes = Encoding.UTF8.GetBytes(datos.Contrasena);
        byte[] hash = SHA256.HashData(passwordBytes.Concat(salt).ToArray());

        var usuario = new Usuario
        {
            RolId = datos.RolId,
            NombreUsuario = datos.NombreUsuario,
            Correo = datos.Correo,
            NombreCompleto = datos.NombreCompleto,
            ContrasenaSalt = salt,
            ContrasenaHash = hash,
            EstaActivo = true,
            FechaCreacion = DateTime.Now
        };

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
    // ACTUALIZAR DATOS BÁSICOS (nombre de usuario, correo, nombre completo)
    // =========================================================
    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarUsuario(int id, UsuarioActualizarDto datos)
    {
        var usuario = await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.UsuarioId == id);

        if (usuario == null)
        {
            return NotFound(new { mensaje = "El usuario no existe." });
        }

        // Validar correo único (excluyendo al propio usuario)
        var correoEnUso = await _contexto.Usuarios
            .AnyAsync(u => u.Correo == datos.Correo && u.UsuarioId != id);

        if (correoEnUso)
        {
            return BadRequest(new { mensaje = "El correo ya está registrado por otro usuario." });
        }

        // Validar nombre de usuario único (excluyendo al propio usuario)
        var nombreEnUso = await _contexto.Usuarios
            .AnyAsync(u => u.NombreUsuario == datos.NombreUsuario && u.UsuarioId != id);

        if (nombreEnUso)
        {
            return BadRequest(new { mensaje = "El nombre de usuario ya está en uso por otro usuario." });
        }

        usuario.NombreUsuario = datos.NombreUsuario;
        usuario.Correo = datos.Correo;
        usuario.NombreCompleto = datos.NombreCompleto;
        usuario.FechaActualizacion = DateTime.Now;

        await _contexto.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Usuario actualizado correctamente.",
            usuarioId = usuario.UsuarioId
        });
    }

    // =========================================================
    // CAMBIAR ROL
    // =========================================================
    [HttpPut("{id}/rol")]
    public async Task<IActionResult> CambiarRol(int id, [FromBody] int rolId)
    {
        var usuario = await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.UsuarioId == id);

        if (usuario == null)
        {
            return NotFound(new { mensaje = "El usuario no existe." });
        }

        var rol = await _contexto.Roles
            .FirstOrDefaultAsync(r => r.RolId == rolId);

        if (rol == null)
        {
            return BadRequest(new { mensaje = "El rol seleccionado no es válido." });
        }

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
        var usuario = await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.UsuarioId == id);

        if (usuario == null)
        {
            return NotFound(new { mensaje = "El usuario no existe." });
        }

        if (!usuario.EstaActivo)
        {
            return BadRequest(new { mensaje = "El usuario ya se encuentra desactivado." });
        }

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

    // =========================================================
    // REACTIVAR USUARIO
    // =========================================================
    [HttpPut("{id}/activar")]
    public async Task<IActionResult> ActivarUsuario(int id)
    {
        var usuario = await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.UsuarioId == id);

        if (usuario == null)
        {
            return NotFound(new { mensaje = "El usuario no existe." });
        }

        if (usuario.EstaActivo)
        {
            return BadRequest(new { mensaje = "El usuario ya se encuentra activo." });
        }

        usuario.EstaActivo = true;
        usuario.FechaActualizacion = DateTime.Now;

        await _contexto.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Usuario reactivado correctamente.",
            usuarioId = usuario.UsuarioId,
            usuario = usuario.NombreUsuario,
            estaActivo = usuario.EstaActivo
        });
    }
}