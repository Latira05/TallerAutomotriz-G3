using System.ComponentModel.DataAnnotations;

namespace AOTech.Web.Models;

// Representa una fila del listado de órdenes de servicio
public class OrdenServicioListaViewModel
{
    public int OrdenServicioId { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string Vehiculo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaIngreso { get; set; }
    public DateTime? FechaEntregaEstimada { get; set; }
    public decimal MontoTotal { get; set; }
    public string Recepcionista { get; set; } = string.Empty;
}

// Opción de cliente para el formulario de creación
public class ClienteSelectViewModel
{
    public int ClienteId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
}

// Opción de vehículo para el formulario de creación
public class VehiculoSelectViewModel
{
    public int VehiculoId { get; set; }
    public int ClienteId { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
}

// Formulario de creación de orden de servicio
public class OrdenServicioCrearViewModel
{
    [Required(ErrorMessage = "Selecciona un cliente.")]
    [Display(Name = "Cliente")]
    public int ClienteId { get; set; }

    [Required(ErrorMessage = "Selecciona un vehículo.")]
    [Display(Name = "Vehículo")]
    public int VehiculoId { get; set; }

    [Required(ErrorMessage = "Escribe la descripción del problema.")]
    [StringLength(500)]
    [Display(Name = "Descripción del problema")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indica la fecha de ingreso.")]
    [Display(Name = "Fecha de ingreso")]
    [DataType(DataType.Date)]
    public DateTime FechaIngreso { get; set; } = DateTime.Now;

    [Display(Name = "Fecha de entrega estimada")]
    [DataType(DataType.Date)]
    public DateTime? FechaEntregaEstimada { get; set; }

    public List<ClienteSelectViewModel> ClientesDisponibles { get; set; } = new();

    public List<VehiculoSelectViewModel> VehiculosDisponibles { get; set; } = new();
}

// Formulario de edición de orden de servicio (MO-02)
// Solo expone los campos que se permite modificar luego de registrada la orden.
public class OrdenServicioEditarViewModel
{
    public int OrdenServicioId { get; set; }

    // Datos de solo lectura, mostrados como contexto en la vista.
    public string Cliente { get; set; } = string.Empty;
    public string Vehiculo { get; set; } = string.Empty;
    public DateTime FechaIngreso { get; set; }

    [Required(ErrorMessage = "Escribe la descripción del problema.")]
    [StringLength(500)]
    [Display(Name = "Descripción del problema")]
    public string Descripcion { get; set; } = string.Empty;

    [Display(Name = "Fecha de entrega estimada")]
    [DataType(DataType.Date)]
    public DateTime? FechaEntregaEstimada { get; set; }

    [Required(ErrorMessage = "Selecciona un estado.")]
    [Display(Name = "Estado")]
    public string Estado { get; set; } = string.Empty;

    public List<string> EstadosDisponibles { get; set; } = new();
}
