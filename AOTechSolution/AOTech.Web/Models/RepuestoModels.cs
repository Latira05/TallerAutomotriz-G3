using System.ComponentModel.DataAnnotations;

namespace AOTech.Web.Models;

// Una fila del listado de repuestos
public class RepuestoListaViewModel
{
    public int RepuestoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal PrecioUnitario { get; set; }
    public int CantidadStock { get; set; }
    public int StockMinimo { get; set; }
    public bool EstaActivo { get; set; }

    // "baja" | "agotado" | "bajo" | "ok"  (lo usa la vista para elegir la etiqueta de color)
    public string EstadoStock =>
        !EstaActivo ? "baja" :
        CantidadStock == 0 ? "agotado" :
        CantidadStock <= StockMinimo ? "bajo" :
        "ok";
}

// Todo lo que necesita la pantalla del listado
public class RepuestoIndexViewModel
{
    public List<RepuestoListaViewModel> Repuestos { get; set; } = new();
    public string? Buscar { get; set; }

    // "activos" | "bajo" | "baja"
    public string Vista { get; set; } = "activos";

    public int TotalActivos { get; set; }
    public int TotalBajoStock { get; set; }
    public int TotalDeBaja { get; set; }
}

// Registro de un repuesto nuevo. Las existencias NO se capturan aquí:
// empiezan en 0 y se cargan con "Registrar ingreso", para que cada unidad
// quede respaldada por un movimiento en el historial.
public class RepuestoCrearViewModel
{
    [Required(ErrorMessage = "Escribe el nombre del repuesto.")]
    [StringLength(100)]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "Escribe el precio unitario.")]
    [Range(0, 99999999.99, ErrorMessage = "El precio no puede ser negativo.")]
    [Display(Name = "Precio unitario")]
    public decimal? PrecioUnitario { get; set; }

    [Required(ErrorMessage = "Escribe el stock mínimo.")]
    [Range(0, 1000000, ErrorMessage = "El stock mínimo debe ser 0 o mayor.")]
    [Display(Name = "Stock mínimo")]
    public int? StockMinimo { get; set; }
}

// Edición: las existencias se muestran pero no se editan (solo cambian con movimientos)
public class RepuestoEditarViewModel
{
    public int RepuestoId { get; set; }

    // Solo para mostrar; nunca se usan para guardar
    public int CantidadStock { get; set; }
    public bool EstaActivo { get; set; }

    [Required(ErrorMessage = "Escribe el nombre del repuesto.")]
    [StringLength(100)]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "Escribe el precio unitario.")]
    [Range(0, 99999999.99, ErrorMessage = "El precio no puede ser negativo.")]
    [Display(Name = "Precio unitario")]
    public decimal? PrecioUnitario { get; set; }

    [Required(ErrorMessage = "Escribe el stock mínimo.")]
    [Range(0, 1000000, ErrorMessage = "El stock mínimo debe ser 0 o mayor.")]
    [Display(Name = "Stock mínimo")]
    public int? StockMinimo { get; set; }
}

// Opción para los menús desplegables de repuestos
public class RepuestoOpcionViewModel
{
    public int RepuestoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int CantidadStock { get; set; }

    public string Etiqueta => $"{Nombre} — {CantidadStock} en stock";
}

// Pantalla "Validar existencias"
public class ValidarExistenciasViewModel
{
    public int? RepuestoId { get; set; }
    public int? Cantidad { get; set; }
    public List<RepuestoOpcionViewModel> Repuestos { get; set; } = new();

    // Resultado (solo cuando ya se consultó)
    public bool Consultado { get; set; }
    public bool Disponible { get; set; }
    public string? NombreRepuesto { get; set; }
    public int StockActual { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}