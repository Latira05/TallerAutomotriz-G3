using System;
using System.Collections.Generic;

namespace AOTech.Data.Modelos;

public partial class ReglasAgendum
{
    public int ReglaId { get; set; }

    public byte DiaSemana { get; set; }

    public TimeOnly HoraApertura { get; set; }

    public TimeOnly HoraCierre { get; set; }

    public int CapacidadBahias { get; set; }

    public int? ServicioId { get; set; }

    public virtual CatalogoServicio? Servicio { get; set; }
}
