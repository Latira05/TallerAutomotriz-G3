using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class MovimientosInventario
{
    public int MovimientoId { get; set; }

    public int RepuestoId { get; set; }

    public string TipoMovimiento { get; set; } = null!;

    public int Cantidad { get; set; }

    public int? OrdenServicioId { get; set; }

    public int UsuarioId { get; set; }

    public string? Motivo { get; set; }

    public DateTime FechaMovimiento { get; set; }

    public virtual OrdenesServicio? OrdenServicio { get; set; }

    public virtual Repuesto Repuesto { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
