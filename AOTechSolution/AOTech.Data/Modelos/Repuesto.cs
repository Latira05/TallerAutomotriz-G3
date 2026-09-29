using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class Repuesto
{
    public int RepuestoId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public decimal PrecioUnitario { get; set; }

    public int CantidadStock { get; set; }

    public int StockMinimo { get; set; }

    public bool EstaActivo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<MovimientosInventario> MovimientosInventarios { get; set; } = new List<MovimientosInventario>();
}
