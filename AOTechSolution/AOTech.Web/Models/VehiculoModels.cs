using System.ComponentModel.DataAnnotations;

namespace AOTech.Web.Models;

public class VehiculoListaViewModel
{
    public int VehiculoId { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public short? Anio { get; set; }
    public int Kilometraje { get; set; }
    public bool EstaActivo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public string CodigoClienteFrecuente { get; set; } = string.Empty;
}

public class VehiculoCrearViewModel
{
    [Required(ErrorMessage = "Selecciona un cliente.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un cliente válido.")]
    [Display(Name = "Cliente")]
    public int ClienteId { get; set; }

    [Required(ErrorMessage = "Escribe la placa.")]
    [StringLength(15)]
    [Display(Name = "Placa")]
    public string Placa { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe la marca.")]
    [StringLength(50)]
    [Display(Name = "Marca")]
    public string Marca { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe el modelo.")]
    [StringLength(50)]
    [Display(Name = "Modelo")]
    public string Modelo { get; set; } = string.Empty;

    [Range(1900, 2100, ErrorMessage = "Escribe un año válido.")]
    [Display(Name = "Año")]
    public short? Anio { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El kilometraje no puede ser negativo.")]
    [Display(Name = "Kilometraje")]
    public int Kilometraje { get; set; }

    public List<ClienteViewModel> ClientesDisponibles { get; set; } = new();
}

public class VehiculoEditarViewModel
{
    public int VehiculoId { get; set; }

    [Required(ErrorMessage = "Selecciona un cliente.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un cliente válido.")]
    [Display(Name = "Cliente")]
    public int ClienteId { get; set; }

    [Required(ErrorMessage = "Escribe la placa.")]
    [StringLength(15)]
    [Display(Name = "Placa")]
    public string Placa { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe la marca.")]
    [StringLength(50)]
    [Display(Name = "Marca")]
    public string Marca { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe el modelo.")]
    [StringLength(50)]
    [Display(Name = "Modelo")]
    public string Modelo { get; set; } = string.Empty;

    [Range(1900, 2100, ErrorMessage = "Escribe un año válido.")]
    [Display(Name = "Año")]
    public short? Anio { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El kilometraje no puede ser negativo.")]
    [Display(Name = "Kilometraje")]
    public int Kilometraje { get; set; }

    public bool EstaActivo { get; set; }
    public List<ClienteViewModel> ClientesDisponibles { get; set; } = new();
}

public class VehiculoDetalleViewModel : VehiculoListaViewModel
{
}

public class ClienteViewModel
{
    public int ClienteId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string CodigoClienteFrecuente { get; set; } = string.Empty;
}
