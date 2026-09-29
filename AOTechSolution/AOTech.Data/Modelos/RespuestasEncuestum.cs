using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class RespuestasEncuestum
{
    public int RespuestaId { get; set; }

    public int EncuestaId { get; set; }

    public int ClienteId { get; set; }

    public int? OrdenServicioId { get; set; }

    public decimal Calificacion { get; set; }

    public string? Comentarios { get; set; }

    public DateTime FechaRespuesta { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual Encuesta Encuesta { get; set; } = null!;

    public virtual OrdenesServicio? OrdenServicio { get; set; }
}
