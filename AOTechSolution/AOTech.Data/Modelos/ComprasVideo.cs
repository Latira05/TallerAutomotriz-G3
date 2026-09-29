using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class ComprasVideo
{
    public int CompraId { get; set; }

    public int ClienteId { get; set; }

    public int VideoId { get; set; }

    public decimal Monto { get; set; }

    public DateTime FechaCompra { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual VideosCurso Video { get; set; } = null!;
}
