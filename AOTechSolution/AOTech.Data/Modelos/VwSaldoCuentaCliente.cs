using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class VwSaldoCuentaCliente
{
    public int ClienteId { get; set; }

    public decimal TotalCargos { get; set; }

    public decimal TotalPagado { get; set; }

    public decimal? Saldo { get; set; }
}
