using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class Cliente
{
    public int ClienteId { get; set; }

    public int UsuarioId { get; set; }

    public string CodigoClienteFrecuente { get; set; } = null!;

    public string? Telefono { get; set; }

    public string? Direccion { get; set; }

    public bool EstaActivo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();

    public virtual ICollection<ComprasVideo> ComprasVideos { get; set; } = new List<ComprasVideo>();

    public virtual ICollection<Membresia> Membresia { get; set; } = new List<Membresia>();

    public virtual ICollection<OrdenesServicio> OrdenesServicios { get; set; } = new List<OrdenesServicio>();

    public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();

    public virtual ICollection<RespuestasEncuestum> RespuestasEncuesta { get; set; } = new List<RespuestasEncuestum>();

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
}
