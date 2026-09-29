using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class TokensRecuperacionContrasena
{
    public int TokenId { get; set; }

    public int UsuarioId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime FechaExpiracion { get; set; }

    public DateTime? FechaUso { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
