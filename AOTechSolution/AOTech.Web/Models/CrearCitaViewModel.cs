using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AOTech.Web.Models
{
    public class CrearCitaViewModel
    {
        [Required(ErrorMessage = "Selecciona un cliente.")]
        public int? ClienteId { get; set; }

        [Required(ErrorMessage = "Selecciona un vehículo.")]
        public int? VehiculoId { get; set; }

        [Required(ErrorMessage = "Selecciona un servicio.")]
        public int? ServicioId { get; set; }

        [Required(ErrorMessage = "Selecciona una fecha.")]
        public DateOnly? FechaSolicitada { get; set; }

        [Required(ErrorMessage = "Selecciona una hora.")]
        public TimeOnly? HoraSolicitada { get; set; }

        [StringLength(500, ErrorMessage = "Las notas no pueden superar los 500 caracteres.")]
        public string? Notas { get; set; }

        public List<SelectListItem> Clientes { get; set; } = new();

        public List<SelectListItem> Vehiculos { get; set; } = new();

        public List<SelectListItem> Servicios { get; set; } = new();
    }
}