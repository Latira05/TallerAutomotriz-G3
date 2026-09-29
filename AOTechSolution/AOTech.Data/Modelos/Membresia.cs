using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class Membresia
{
    public int MembresiaId { get; set; }

    public int ClienteId { get; set; }

    public string CodigoBp { get; set; } = null!;

    public string TipoCliente { get; set; } = null!;

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaFin { get; set; }

    public bool EstaActivo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual ICollection<PagosMembresium> PagosMembresia { get; set; } = new List<PagosMembresium>();
}
