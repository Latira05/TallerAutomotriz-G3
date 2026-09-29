using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class CatalogoServicio
{
    public int ServicioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public decimal PrecioRegular { get; set; }

    public decimal PrecioMecanico { get; set; }

    public int DuracionEstimadaMinutos { get; set; }

    public bool EstaActivo { get; set; }

    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();

    public virtual ICollection<DetalleOrdenServicio> DetalleOrdenServicios { get; set; } = new List<DetalleOrdenServicio>();

    public virtual ICollection<ReglasAgendum> ReglasAgenda { get; set; } = new List<ReglasAgendum>();
}
