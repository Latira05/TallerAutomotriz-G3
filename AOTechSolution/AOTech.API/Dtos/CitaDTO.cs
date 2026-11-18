public class ModificarCitaDto
{
    public DateOnly? FechaSolicitada { get; set; }

    public TimeOnly? HoraSolicitada { get; set; }
}

public class AgendarCitaDto
{
    public int ClienteId { get; set; }

    public int VehiculoId { get; set; }

    public int? ServicioId { get; set; }

    public DateOnly FechaSolicitada { get; set; }

    public TimeOnly HoraSolicitada { get; set; }

    public string? Notas { get; set; }
}


