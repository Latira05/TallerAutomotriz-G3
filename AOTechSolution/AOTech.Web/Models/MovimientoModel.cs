using System.ComponentModel.DataAnnotations;

namespace AOTech.Web.Models;

// Una fila del historial de movimientos
public class MovimientoListaViewModel
{
    public int MovimientoId { get; set; }
    public DateTime FechaMovimiento { get; set; }
    public string TipoMovimiento { get; set; } = string.Empty;   // "Entrada" | "Salida"
    public string NombreRepuesto { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public int? OrdenServicioId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string? Motivo { get; set; }
}

// Pantalla del historial con sus filtros
public class MovimientoIndexViewModel
{
    public List<MovimientoListaViewModel> Movimientos { get; set; } = new();

    // Filtros
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int? RepuestoId { get; set; }
    public int? OrdenId { get; set; }
    public string? Tipo { get; set; }

    public List<RepuestoOpcionViewModel> Repuestos { get; set; } = new();

    public int TotalCoincidencias { get; set; }
    public int Limite { get; set; }

    public bool HayFiltros =>
        Desde.HasValue || Hasta.HasValue || RepuestoId.HasValue || OrdenId.HasValue || !string.IsNullOrEmpty(Tipo);
}

// Registrar ingreso de repuestos (compra, devolución, etc.)
public class MovimientoEntradaViewModel
{
    [Required(ErrorMessage = "Selecciona un repuesto.")]
    [Display(Name = "Repuesto")]
    public int? RepuestoId { get; set; }

    [Required(ErrorMessage = "Escribe la cantidad.")]
    [Range(1, 1000000, ErrorMessage = "La cantidad debe ser al menos 1.")]
    [Display(Name = "Cantidad que ingresa")]
    public int? Cantidad { get; set; }

    [StringLength(200)]
    [Display(Name = "Motivo o referencia")]
    public string? Motivo { get; set; }

    public List<RepuestoOpcionViewModel> Repuestos { get; set; } = new();
}

// Registrar salida de repuestos usados en una orden de servicio
public class MovimientoSalidaViewModel
{
    [Required(ErrorMessage = "Selecciona un repuesto.")]
    [Display(Name = "Repuesto")]
    public int? RepuestoId { get; set; }

    [Required(ErrorMessage = "Escribe la cantidad.")]
    [Range(1, 1000000, ErrorMessage = "La cantidad debe ser al menos 1.")]
    [Display(Name = "Cantidad utilizada")]
    public int? Cantidad { get; set; }

    [Required(ErrorMessage = "Selecciona la orden de servicio.")]
    [Display(Name = "Orden de servicio")]
    public int? OrdenServicioId { get; set; }

    [StringLength(200)]
    [Display(Name = "Observación")]
    public string? Motivo { get; set; }

    public List<RepuestoOpcionViewModel> Repuestos { get; set; } = new();
    public List<OrdenOpcionViewModel> Ordenes { get; set; } = new();
}

public class OrdenOpcionViewModel
{
    public int OrdenServicioId { get; set; }
    public string Etiqueta { get; set; } = string.Empty;
}