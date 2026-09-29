using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class Usuario
{
    public int UsuarioId { get; set; }

    public int RolId { get; set; }

    public string NombreUsuario { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public byte[] ContrasenaHash { get; set; } = null!;

    public byte[] ContrasenaSalt { get; set; } = null!;

    public string NombreCompleto { get; set; } = null!;

    public bool EstaActivo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();

    public virtual Cliente? Cliente { get; set; }

    public virtual ICollection<MovimientosInventario> MovimientosInventarios { get; set; } = new List<MovimientosInventario>();

    public virtual ICollection<OrdenesServicio> OrdenesServicioMecanicoUsuarios { get; set; } = new List<OrdenesServicio>();

    public virtual ICollection<OrdenesServicio> OrdenesServicioRecepcionistaUsuarios { get; set; } = new List<OrdenesServicio>();

    public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();

    public virtual Role Rol { get; set; } = null!;

    public virtual ICollection<TokensRecuperacionContrasena> TokensRecuperacionContrasenas { get; set; } = new List<TokensRecuperacionContrasena>();
}
