namespace AOTech.Web.Models
{
    public class ReprogramarCitaViewModel
    {
        public int CitaId { get; set; }

        public DateOnly FechaSolicitada { get; set; }

        public TimeOnly HoraSolicitada { get; set; }
    }
}
