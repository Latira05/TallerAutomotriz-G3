using System.ComponentModel.DataAnnotations;

namespace AOTech.Web.Models;

public class AvisoFormularioViewModel
{
    public int AvisoId { get; set; }

    [Required(ErrorMessage = "Escribe un título.")]
    [StringLength(150, ErrorMessage = "El título no puede exceder 150 caracteres.")]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe el mensaje.")]
    [Display(Name = "Mensaje")]
    public string Mensaje { get; set; } = string.Empty;

    [Display(Name = "Enviar a todos los clientes activos")]
    public bool TodosClientesActivos { get; set; }

    public List<int> ClienteIds { get; set; } = new();

    public List<ClienteAvisoOpcionViewModel> ClientesDisponibles { get; set; } = new();
}

public class ClienteAvisoOpcionViewModel
{
    public int ClienteId { get; set; }
    public int UsuarioId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string CodigoClienteFrecuente { get; set; } = string.Empty;
}

public class AvisoListadoViewModel
{
    public string? Buscar { get; set; }
    public string? Estado { get; set; }
    public List<AvisoListaViewModel> Avisos { get; set; } = new();
}

public class AvisoListaViewModel
{
    public int AvisoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int CantidadDestinatarios { get; set; }
    public int CantidadLeidos { get; set; }
}

public class AvisoDetalleViewModel : AvisoListaViewModel
{
    public string Mensaje { get; set; } = string.Empty;
    public string NombreCreador { get; set; } = string.Empty;
    public List<AvisoDestinatarioViewModel> Destinatarios { get; set; } = new();
}

public class AvisoDestinatarioViewModel
{
    public int UsuarioId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string CodigoClienteFrecuente { get; set; } = string.Empty;
    public DateTime? FechaLectura { get; set; }
}

public class AvisoHistorialViewModel
{
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public int? UsuarioDestinatarioId { get; set; }
    public List<ClienteAvisoOpcionViewModel> DestinatariosDisponibles { get; set; } = new();
    public List<AvisoListaViewModel> Avisos { get; set; } = new();
}

public class AvisoBandejaViewModel
{
    public int AvisoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public DateTime FechaEnvio { get; set; }
    public DateTime? FechaLectura { get; set; }
}

public class AvisoBandejaDetalleViewModel
{
    public int AvisoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public DateTime FechaEnvio { get; set; }
    public DateTime? FechaLectura { get; set; }
}
