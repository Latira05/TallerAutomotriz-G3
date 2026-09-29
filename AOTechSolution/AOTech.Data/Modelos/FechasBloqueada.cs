using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class FechasBloqueada
{
    public int FechaBloqueadaId { get; set; }

    public DateOnly FechaBloqueada { get; set; }

    public string? Motivo { get; set; }
}
