using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class PagosMembresium
{
    public int PagoMembresiaId { get; set; }

    public int MembresiaId { get; set; }

    public decimal Monto { get; set; }

    public DateTime FechaPago { get; set; }

    public virtual Membresia Membresia { get; set; } = null!;
}
