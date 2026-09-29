using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class OrdenesServicio
{
    public int OrdenServicioId { get; set; }

    public int VehiculoId { get; set; }

    public int ClienteId { get; set; }

    public int RecepcionistaUsuarioId { get; set; }

    public int? MecanicoUsuarioId { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaIngreso { get; set; }

    public DateTime? FechaEntregaEstimada { get; set; }

    public string? Descripcion { get; set; }

    public decimal MontoTotal { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual ICollection<DetalleOrdenServicio> DetalleOrdenServicios { get; set; } = new List<DetalleOrdenServicio>();

    public virtual Usuario? MecanicoUsuario { get; set; }

    public virtual ICollection<MovimientosInventario> MovimientosInventarios { get; set; } = new List<MovimientosInventario>();

    public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();

    public virtual Usuario RecepcionistaUsuario { get; set; } = null!;

    public virtual ICollection<RespuestasEncuestum> RespuestasEncuesta { get; set; } = new List<RespuestasEncuestum>();

    public virtual Vehiculo Vehiculo { get; set; } = null!;
}
