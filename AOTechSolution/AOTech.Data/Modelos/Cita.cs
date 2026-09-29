using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class Cita
{
    public int CitaId { get; set; }

    public int ClienteId { get; set; }

    public int VehiculoId { get; set; }

    public int? ServicioId { get; set; }

    public DateOnly FechaSolicitada { get; set; }

    public TimeOnly HoraSolicitada { get; set; }

    public string Estado { get; set; } = null!;

    public int? AprobadaPorUsuarioId { get; set; }

    public string? Notas { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public virtual Usuario? AprobadaPorUsuario { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual CatalogoServicio? Servicio { get; set; }

    public virtual Vehiculo Vehiculo { get; set; } = null!;
}
