using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class Pago
{
    public int PagoId { get; set; }

    public int OrdenServicioId { get; set; }

    public int ClienteId { get; set; }

    public decimal Monto { get; set; }

    public string MetodoPago { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public DateTime FechaReporte { get; set; }

    public int? ValidadoPorUsuarioId { get; set; }

    public DateTime? FechaValidacion { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual OrdenesServicio OrdenServicio { get; set; } = null!;

    public virtual Usuario? ValidadoPorUsuario { get; set; }
}
