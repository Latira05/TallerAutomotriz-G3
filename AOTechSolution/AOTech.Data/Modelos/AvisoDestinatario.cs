using System;

namespace AOTech.Data.Modelos;

public partial class AvisoDestinatario
{
    public int AvisoId { get; set; }

    public int UsuarioId { get; set; }

    public DateTime? FechaLectura { get; set; }

    public virtual Aviso Aviso { get; set; } = null!;
}
