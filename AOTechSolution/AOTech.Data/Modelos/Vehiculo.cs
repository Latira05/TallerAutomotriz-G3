using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class Vehiculo
{
    public int VehiculoId { get; set; }

    public int ClienteId { get; set; }

    public string Placa { get; set; } = null!;

    public string Marca { get; set; } = null!;

    public string Modelo { get; set; } = null!;

    public short? Anio { get; set; }

    public int Kilometraje { get; set; }

    public bool EstaActivo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual ICollection<OrdenesServicio> OrdenesServicios { get; set; } = new List<OrdenesServicio>();
}
