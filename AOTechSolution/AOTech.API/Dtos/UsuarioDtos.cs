namespace AOTech.API.Dtos;

// Datos que llegan del cliente para crear un usuario.
// Nota: 'Contrasena' es texto plano SOLO en tránsito (HTTPS); nunca se guarda así.
public class UsuarioCrearDto
{
    public int RolId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
}

// Datos editables de un usuario ya existente (no incluye contraseña ni rol;
// el rol se cambia por su propio endpoint, como ya lo tenías).
public class UsuarioActualizarDto
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
}