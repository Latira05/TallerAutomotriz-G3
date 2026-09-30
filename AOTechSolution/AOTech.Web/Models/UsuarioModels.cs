using System.ComponentModel.DataAnnotations;

namespace AOTech.Web.Models;

// Representa una fila del listado (coincide con lo que devuelve la API)
public class UsuarioListaViewModel
{
    public int UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public bool EstaActivo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public int RolId { get; set; }
    public string? Rol { get; set; }
}

public class RolViewModel
{
    public int RolId { get; set; }
    public string NombreRol { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}

// Formulario de creación
public class UsuarioCrearViewModel
{
    [Required(ErrorMessage = "Escribe un nombre de usuario.")]
    [StringLength(50)]
    [Display(Name = "Nombre de usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe un correo.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [Display(Name = "Correo")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe el nombre completo.")]
    [StringLength(150)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe una contraseña.")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecciona un rol.")]
    [Display(Name = "Rol")]
    public int RolId { get; set; }

    public List<RolViewModel> RolesDisponibles { get; set; } = new();
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Escribe un nombre de usuario.")]
    [Display(Name = "Nombre de usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe la contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = string.Empty;
}

// Formulario de edición (datos básicos + rol; el estado activo/inactivo
// se maneja aparte, con un botón directo en el listado)
public class UsuarioEditarViewModel
{
    public int UsuarioId { get; set; }

    [Required(ErrorMessage = "Escribe un nombre de usuario.")]
    [StringLength(50)]
    [Display(Name = "Nombre de usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe un correo.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [Display(Name = "Correo")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe el nombre completo.")]
    [StringLength(150)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecciona un rol.")]
    [Display(Name = "Rol")]
    public int RolId { get; set; }

    public bool EstaActivo { get; set; }

    public List<RolViewModel> RolesDisponibles { get; set; } = new();
}