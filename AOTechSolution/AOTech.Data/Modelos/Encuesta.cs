using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class Encuesta
{
    public int EncuestaId { get; set; }

    public string Titulo { get; set; } = null!;

    public string TipoCalificacion { get; set; } = null!;

    public bool EstaPublicada { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<RespuestasEncuestum> RespuestasEncuesta { get; set; } = new List<RespuestasEncuestum>();
}
