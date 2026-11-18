using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class Aviso
{
    public int AvisoId { get; set; }

    public string Titulo { get; set; } = null!;

    public string Mensaje { get; set; } = null!;

    public int UsuarioCreadorId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime? FechaEnvio { get; set; }

    public virtual ICollection<AvisoDestinatario> Destinatarios { get; set; } = new List<AvisoDestinatario>();
}
