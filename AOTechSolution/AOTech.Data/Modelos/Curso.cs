using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class Curso
{
    public int CursoId { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool EstaPublicado { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<VideosCurso> VideosCursos { get; set; } = new List<VideosCurso>();
}
