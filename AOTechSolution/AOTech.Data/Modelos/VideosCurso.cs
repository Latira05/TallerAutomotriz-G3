using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class VideosCurso
{
    public int VideoId { get; set; }

    public int CursoId { get; set; }

    public string Titulo { get; set; } = null!;

    public decimal Precio { get; set; }

    public string UrlVideo { get; set; } = null!;

    public int? DuracionMinutos { get; set; }

    public virtual ICollection<ComprasVideo> ComprasVideos { get; set; } = new List<ComprasVideo>();

    public virtual Curso Curso { get; set; } = null!;
}
