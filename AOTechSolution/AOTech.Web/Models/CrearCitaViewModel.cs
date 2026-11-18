using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AOTech.Web.Models
{
    public class CrearCitaViewModel
    {
        [Required(ErrorMessage = "Selecciona un cliente.")]
        [Display(Name = "Cliente")]
        public int? ClienteId { get; set; }

        [Required(ErrorMessage = "Selecciona un vehículo.")]
        [Display(Name = "Vehículo")]
        public int? VehiculoId { get; set; }

        [Required(ErrorMessage = "Selecciona un servicio.")]
        [Display(Name = "Servicio")]
        public int? ServicioId { get; set; }

        [Required(ErrorMessage = "Selecciona una fecha.")]
        [Display(Name = "Fecha solicitada")]
        public DateOnly? FechaSolicitada { get; set; }

        [Required(ErrorMessage = "Selecciona una hora.")]
        [Display(Name = "Hora solicitada")]
        public TimeOnly? HoraSolicitada { get; set; }

        [StringLength(
            500,
            ErrorMessage = "Las notas no pueden superar los 500 caracteres.")]
        [Display(Name = "Notas adicionales")]
        public string? Notas { get; set; }

        // Listas para los selectores del formulario
        public List<SelectListItem> Clientes { get; set; } = new();

        public List<SelectListItem> Vehiculos { get; set; } = new();

        public List<SelectListItem> Servicios { get; set; } = new();
    }
}