using AOTech.Data.Contexto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly AOTechDbContext _contexto;

    public ClientesController(AOTechDbContext contexto) => _contexto = contexto;

    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await _contexto.Clientes.ToListAsync());
}