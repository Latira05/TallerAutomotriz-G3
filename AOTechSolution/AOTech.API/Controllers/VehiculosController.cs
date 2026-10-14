
using AOTech.Data.Contexto;
using AOTech.Data.Modelos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AOTech.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiculosController : ControllerBase
    {
        private readonly AOTechDbContext _contexto;

        public VehiculosController(AOTechDbContext contexto)
        {
            _contexto = contexto;
        }

        // MC-04: Registro de un vehículo
        [HttpPost]
        public async Task<IActionResult> RegistrarVehiculo(
            [FromBody] Vehiculo vehiculo)
        {
            if (vehiculo.ClienteId <= 0 ||
                string.IsNullOrWhiteSpace(vehiculo.Placa) ||
                string.IsNullOrWhiteSpace(vehiculo.Marca) ||
                string.IsNullOrWhiteSpace(vehiculo.Modelo))
            {
                return BadRequest("Debe completar los datos obligatorios del vehículo.");
            }

            if (vehiculo.Kilometraje < 0)
            {
                return BadRequest("El kilometraje no puede ser negativo.");
            }

            bool placaExiste = await _contexto.Vehiculos
                .AnyAsync(v => v.Placa == vehiculo.Placa);

            if (placaExiste)
            {
                return Conflict("Ya existe un vehículo registrado con esa placa.");
            }

            bool clienteExiste = await _contexto.Clientes
                .AnyAsync(c => c.ClienteId == vehiculo.ClienteId);

            if (!clienteExiste)
            {
                return BadRequest("El cliente indicado no existe.");
            }

            vehiculo.VehiculoId = 0;
            vehiculo.FechaCreacion = DateTime.UtcNow;
            vehiculo.EstaActivo = true;

            _contexto.Vehiculos.Add(vehiculo);
            await _contexto.SaveChangesAsync();

            return CreatedAtAction(
                nameof(ObtenerVehiculo),
                new { id = vehiculo.VehiculoId },
                vehiculo);
        }

        // Consulta de un vehículo por ID
        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerVehiculo(int id)
        {
            var vehiculo = await _contexto.Vehiculos
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.VehiculoId == id);

            if (vehiculo == null)
            {
                return NotFound("No se encontró el vehículo.");
            }

            return Ok(vehiculo);
        }

        // MC-05: Modificación de los datos de un vehículo
        [HttpPut("{id:int}")]
        public async Task<IActionResult> ModificarVehiculo(
            int id,
            [FromBody] Vehiculo datos)
        {
            var vehiculo = await _contexto.Vehiculos
                .FirstOrDefaultAsync(v => v.VehiculoId == id);

            if (vehiculo == null)
            {
                return NotFound("No se encontró el vehículo.");
            }

            if (string.IsNullOrWhiteSpace(datos.Placa) ||
                string.IsNullOrWhiteSpace(datos.Marca) ||
                string.IsNullOrWhiteSpace(datos.Modelo))
            {
                return BadRequest("La placa, marca y modelo son obligatorios.");
            }

            if (datos.Kilometraje < 0)
            {
                return BadRequest("El kilometraje no puede ser negativo.");
            }

            bool placaDuplicada = await _contexto.Vehiculos
                .AnyAsync(v => v.Placa == datos.Placa
                    && v.VehiculoId != id);

            if (placaDuplicada)
            {
                return Conflict("Otro vehículo ya tiene esa placa.");
            }

            vehiculo.Placa = datos.Placa.Trim();
            vehiculo.Marca = datos.Marca.Trim();
            vehiculo.Modelo = datos.Modelo.Trim();
            vehiculo.Kilometraje = datos.Kilometraje;

            await _contexto.SaveChangesAsync();

            return Ok(vehiculo);
        }
    }
}

