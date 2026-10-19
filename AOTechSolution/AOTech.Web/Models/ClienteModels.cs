using System.ComponentModel.DataAnnotations;

namespace AOTech.Web.Models;

// Una fila del listado de clientes
public class ClienteListaViewModel
{
    public int ClienteId { get; set; }
    public string CodigoClienteFrecuente { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public bool EstaActivo { get; set; }
    public DateTime FechaCreacion { get; set; }
}

// Formulario de registro: crea la cuenta de acceso (Usuario con rol Cliente)
// y el cliente en un solo paso.
public class ClienteCrearViewModel
{
    // --- Datos del cliente ---
    [Required(ErrorMessage = "Escribe el nombre completo.")]
    [StringLength(150)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [RegularExpression(@"^[0-9+\-\s()]{7,20}$",
        ErrorMessage = "El teléfono solo puede llevar números, espacios, +, - y paréntesis (mínimo 7 caracteres).")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [StringLength(250)]
    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    // --- Cuenta de acceso ---
    [Required(ErrorMessage = "Escribe un nombre de usuario.")]
    [StringLength(50)]
    [Display(Name = "Nombre de usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe un correo.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [Display(Name = "Correo")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe una contraseña.")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = string.Empty;
}

// Formulario de edición (no incluye contraseña; el código de cliente
// es de solo lectura porque lo genera el sistema)
public class ClienteEditarViewModel
{
    public int ClienteId { get; set; }

    // Solo se muestra en pantalla; nunca se usa para guardar.
    public string CodigoClienteFrecuente { get; set; } = string.Empty;

    public bool EstaActivo { get; set; }

    [Required(ErrorMessage = "Escribe el nombre completo.")]
    [StringLength(150)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [RegularExpression(@"^[0-9+\-\s()]{7,20}$",
        ErrorMessage = "El teléfono solo puede llevar números, espacios, +, - y paréntesis (mínimo 7 caracteres).")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [StringLength(250)]
    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    [Required(ErrorMessage = "Escribe un nombre de usuario.")]
    [StringLength(50)]
    [Display(Name = "Nombre de usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe un correo.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [Display(Name = "Correo")]
    public string Correo { get; set; } = string.Empty;
}