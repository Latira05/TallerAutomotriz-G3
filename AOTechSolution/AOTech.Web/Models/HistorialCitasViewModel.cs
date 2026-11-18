using AOTech.Data.Modelos;
namespace AOTech.Web.Models


{
    public class HistorialCitasViewModel
    {
        public int? ClienteId { get; set; }

        public string? Estado { get; set; }

        public DateOnly? FechaDesde { get; set; }

        public DateOnly? FechaHasta { get; set; }

        public List<Cita> Citas { get; set; } = new();
    }
}
