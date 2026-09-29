using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class DetalleOrdenServicio
{
    public int DetalleOrdenServicioId { get; set; }

    public int OrdenServicioId { get; set; }

    public int? ServicioId { get; set; }

    public string Descripcion { get; set; } = null!;

    public decimal PrecioUnitario { get; set; }

    public int Cantidad { get; set; }

    public decimal? Subtotal { get; set; }

    public virtual OrdenesServicio OrdenServicio { get; set; } = null!;

    public virtual CatalogoServicio? Servicio { get; set; }
}
