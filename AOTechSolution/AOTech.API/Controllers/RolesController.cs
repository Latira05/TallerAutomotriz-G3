using AOTech.Data.Contexto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly AOTechDbContext _contexto;

    public RolesController(AOTechDbContext contexto) => _contexto = contexto;

    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await _contexto.Roles.ToListAsync());
}

