using System.Security.Claims;
using System.Data;
using AOTech.Data.Contexto;
using AOTech.Data.Modelos;
using AOTech.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AOTech.Web.Controllers;

public class AvisosController : Controller
{
    private const string Borrador = "Borrador";
    private const string Enviado = "Enviado";
    private readonly AOTechDbContext _contexto;
    private readonly ILogger<AvisosController> _logger;

    public AvisosController(AOTechDbContext contexto, ILogger<AvisosController> logger)
    {
        _contexto = contexto;
        _logger = logger;
    }

    [Authorize(Roles = "Administrador,Recepcionista")]
    public async Task<IActionResult> Index(string? buscar, string? estado)
    {
        var consulta = _contexto.Avisos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var termino = buscar.Trim();
            consulta = consulta.Where(a => a.Titulo.Contains(termino));
        }

        if (estado == Borrador || estado == Enviado)
        {
            consulta = consulta.Where(a => a.Estado == estado);
        }

        var modelo = new AvisoListadoViewModel
        {
            Buscar = buscar,
            Estado = estado,
            Avisos = await consulta
                .OrderByDescending(a => a.FechaCreacion)
                .Select(a => new AvisoListaViewModel
                {
                    AvisoId = a.AvisoId,
                    Titulo = a.Titulo,
                    FechaCreacion = a.FechaCreacion,
                    FechaEnvio = a.FechaEnvio,
                    Estado = a.Estado,
                    CantidadDestinatarios = a.Destinatarios.Count(),
                    CantidadLeidos = a.Destinatarios.Count(d => d.FechaLectura != null)
                })
                .ToListAsync()
        };

        return View(modelo);
    }

    [Authorize(Roles = "Administrador,Recepcionista")]
    public async Task<IActionResult> Crear()
    {
        var modelo = new AvisoFormularioViewModel();
        await CargarClientesDisponiblesAsync(modelo);
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Recepcionista")]
    public async Task<IActionResult> Crear(AvisoFormularioViewModel entrada)
    {
        var usuarioCreadorId = ObtenerUsuarioAutenticadoId();
        if (usuarioCreadorId == null)
        {
            return Forbid();
        }

        var destinatarios = await ObtenerDestinatariosAsync(entrada);
        if (!ModelState.IsValid)
        {
            await CargarClientesDisponiblesAsync(entrada);
            return View(entrada);
        }

        var aviso = new Aviso
        {
            Titulo = entrada.Titulo.Trim(),
            Mensaje = entrada.Mensaje.Trim(),
            UsuarioCreadorId = usuarioCreadorId.Value,
            FechaCreacion = DateTime.UtcNow,
            Estado = Borrador
        };

        await using var transaccion = await _contexto.Database.BeginTransactionAsync();
        try
        {
            _contexto.Avisos.Add(aviso);
            await _contexto.SaveChangesAsync();

            _contexto.AvisosDestinatarios.AddRange(destinatarios.Select(usuarioId => new AvisoDestinatario
            {
                AvisoId = aviso.AvisoId,
                UsuarioId = usuarioId
            }));

            await _contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (DbUpdateException ex)
        {
            await transaccion.RollbackAsync();
            _logger.LogError(ex, "Error al crear el aviso y sus destinatarios.");
            ModelState.AddModelError(string.Empty, "No se pudo guardar el aviso. Revisa los datos e inténtalo de nuevo.");
            await CargarClientesDisponiblesAsync(entrada);
            return View(entrada);
        }

        TempData["Exito"] = "Aviso guardado como borrador.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador,Recepcionista")]
    public async Task<IActionResult> Editar(int id)
    {
        var aviso = await _contexto.Avisos
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AvisoId == id);

        if (aviso == null)
        {
            TempData["Error"] = "El aviso no existe.";
            return RedirectToAction(nameof(Index));
        }

        if (aviso.Estado != Borrador)
        {
            TempData["Error"] = "Los avisos enviados no se pueden modificar.";
            return RedirectToAction(nameof(Detalles), new { id });
        }

        var modelo = new AvisoFormularioViewModel
        {
            AvisoId = aviso.AvisoId,
            Titulo = aviso.Titulo,
            Mensaje = aviso.Mensaje,
            ClienteIds = await _contexto.AvisosDestinatarios
                .Where(d => d.AvisoId == id)
                .Join(_contexto.Clientes, d => d.UsuarioId, c => c.UsuarioId, (d, c) => c.ClienteId)
                .ToListAsync()
        };

        await CargarClientesDisponiblesAsync(modelo);
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Recepcionista")]
    public async Task<IActionResult> Editar(AvisoFormularioViewModel entrada)
    {
        var destinatarios = await ObtenerDestinatariosAsync(entrada);
        if (!ModelState.IsValid)
        {
            await CargarClientesDisponiblesAsync(entrada);
            return View(entrada);
        }

        await using var transaccion = await _contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var aviso = await _contexto.Avisos
                .FirstOrDefaultAsync(a => a.AvisoId == entrada.AvisoId);

            if (aviso == null)
            {
                await transaccion.RollbackAsync();
                TempData["Error"] = "El aviso no existe.";
                return RedirectToAction(nameof(Index));
            }

            if (aviso.Estado != Borrador)
            {
                await transaccion.RollbackAsync();
                TempData["Error"] = "Los avisos enviados no se pueden modificar.";
                return RedirectToAction(nameof(Detalles), new { id = entrada.AvisoId });
            }

            var destinatariosActuales = await _contexto.AvisosDestinatarios
                .Where(d => d.AvisoId == entrada.AvisoId)
                .ToListAsync();
            var idsActuales = destinatariosActuales.Select(d => d.UsuarioId).ToHashSet();
            var idsNuevos = destinatarios.ToHashSet();

            _contexto.AvisosDestinatarios.RemoveRange(
                destinatariosActuales.Where(d => !idsNuevos.Contains(d.UsuarioId)));
            _contexto.AvisosDestinatarios.AddRange(
                idsNuevos.Where(usuarioId => !idsActuales.Contains(usuarioId)).Select(usuarioId => new AvisoDestinatario
                {
                    AvisoId = aviso.AvisoId,
                    UsuarioId = usuarioId
                }));

            aviso.Titulo = entrada.Titulo.Trim();
            aviso.Mensaje = entrada.Mensaje.Trim();
            await _contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (DbUpdateException ex)
        {
            await transaccion.RollbackAsync();
            _logger.LogError(ex, "Error al actualizar el aviso {AvisoId}.", entrada.AvisoId);
            ModelState.AddModelError(string.Empty, "No se pudieron guardar los cambios del aviso.");
            await CargarClientesDisponiblesAsync(entrada);
            return View(entrada);
        }

        TempData["Exito"] = "Borrador actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Recepcionista")]
    public async Task<IActionResult> Enviar(int id)
    {
        await using var transaccion = await _contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var aviso = await _contexto.Avisos.FirstOrDefaultAsync(a => a.AvisoId == id);

            if (aviso == null)
            {
                await transaccion.RollbackAsync();
                TempData["Error"] = "El aviso no existe.";
                return RedirectToAction(nameof(Index));
            }

            if (aviso.Estado != Borrador)
            {
                await transaccion.RollbackAsync();
                TempData["Error"] = "Este aviso ya fue enviado y no se puede volver a enviar.";
                return RedirectToAction(nameof(Detalles), new { id });
            }

            if (!await _contexto.AvisosDestinatarios.AnyAsync(d => d.AvisoId == id))
            {
                await transaccion.RollbackAsync();
                TempData["Error"] = "El aviso no tiene destinatarios y no se puede enviar.";
                return RedirectToAction(nameof(Detalles), new { id });
            }

            var fechaEnvio = DateTime.UtcNow;
            var actualizados = await _contexto.Avisos
                .Where(a => a.AvisoId == id && a.Estado == Borrador)
                .ExecuteUpdateAsync(actualizacion => actualizacion
                    .SetProperty(a => a.Estado, Enviado)
                    .SetProperty(a => a.FechaEnvio, fechaEnvio));

            if (actualizados != 1)
            {
                await transaccion.RollbackAsync();
                TempData["Error"] = "El aviso ya fue enviado o cambió de estado.";
                return RedirectToAction(nameof(Detalles), new { id });
            }

            await transaccion.CommitAsync();
            TempData["Exito"] = "Notificación interna enviada a sus destinatarios.";
            return RedirectToAction(nameof(Detalles), new { id });
        }
        catch (Exception ex)
        {
            await transaccion.RollbackAsync();
            _logger.LogError(ex, "Error al enviar internamente el aviso {AvisoId}.", id);
            TempData["Error"] = "No se pudo enviar el aviso. Intenta de nuevo.";
            return RedirectToAction(nameof(Detalles), new { id });
        }
    }

    [Authorize(Roles = "Administrador,Recepcionista")]
    public async Task<IActionResult> Detalles(int id)
    {
        var aviso = await _contexto.Avisos.AsNoTracking()
            .FirstOrDefaultAsync(a => a.AvisoId == id);

        if (aviso == null)
        {
            TempData["Error"] = "El aviso no existe.";
            return RedirectToAction(nameof(Index));
        }

        var modelo = new AvisoDetalleViewModel
        {
            AvisoId = aviso.AvisoId,
            Titulo = aviso.Titulo,
            Mensaje = aviso.Mensaje,
            FechaCreacion = aviso.FechaCreacion,
            FechaEnvio = aviso.FechaEnvio,
            Estado = aviso.Estado,
            NombreCreador = await _contexto.Usuarios
                .Where(u => u.UsuarioId == aviso.UsuarioCreadorId)
                .Select(u => u.NombreCompleto)
                .FirstOrDefaultAsync() ?? "Usuario no disponible",
            Destinatarios = await _contexto.AvisosDestinatarios.AsNoTracking()
                .Where(d => d.AvisoId == id)
                .Join(_contexto.Usuarios, d => d.UsuarioId, u => u.UsuarioId, (d, u) => new { d, u })
                .Select(x => new AvisoDestinatarioViewModel
                {
                    UsuarioId = x.u.UsuarioId,
                    NombreCompleto = x.u.NombreCompleto,
                    CodigoClienteFrecuente = _contexto.Clientes
                        .Where(c => c.UsuarioId == x.u.UsuarioId)
                        .Select(c => c.CodigoClienteFrecuente)
                        .FirstOrDefault() ?? "—",
                    FechaLectura = x.d.FechaLectura
                })
                .OrderBy(d => d.NombreCompleto)
                .ToListAsync()
        };

        modelo.CantidadDestinatarios = modelo.Destinatarios.Count;
        modelo.CantidadLeidos = modelo.Destinatarios.Count(d => d.FechaLectura != null);
        return View(modelo);
    }

    [Authorize(Roles = "Administrador,Recepcionista")]
    public async Task<IActionResult> Historial(DateTime? fechaDesde, DateTime? fechaHasta, int? usuarioDestinatarioId)
    {
        var consulta = _contexto.Avisos.AsNoTracking().Where(a => a.Estado == Enviado);

        if (fechaDesde.HasValue)
        {
            consulta = consulta.Where(a => a.FechaEnvio >= fechaDesde.Value.Date);
        }

        if (fechaHasta.HasValue)
        {
            var hastaExclusivo = fechaHasta.Value.Date.AddDays(1);
            consulta = consulta.Where(a => a.FechaEnvio < hastaExclusivo);
        }

        if (usuarioDestinatarioId.HasValue)
        {
            consulta = consulta.Where(a => a.Destinatarios.Any(d => d.UsuarioId == usuarioDestinatarioId.Value));
        }

        var modelo = new AvisoHistorialViewModel
        {
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            UsuarioDestinatarioId = usuarioDestinatarioId,
            DestinatariosDisponibles = await ObtenerTodosLosClientesAsync(),
            Avisos = await consulta
                .OrderByDescending(a => a.FechaEnvio)
                .Select(a => new AvisoListaViewModel
                {
                    AvisoId = a.AvisoId,
                    Titulo = a.Titulo,
                    FechaCreacion = a.FechaCreacion,
                    FechaEnvio = a.FechaEnvio,
                    Estado = a.Estado,
                    CantidadDestinatarios = a.Destinatarios.Count(),
                    CantidadLeidos = a.Destinatarios.Count(d => d.FechaLectura != null)
                })
                .ToListAsync()
        };

        return View(modelo);
    }

    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> MisAvisos()
    {
        var usuarioId = ObtenerUsuarioAutenticadoId();
        if (usuarioId == null)
        {
            return Forbid();
        }

        var avisos = await _contexto.AvisosDestinatarios.AsNoTracking()
            .Where(d => d.UsuarioId == usuarioId.Value && d.Aviso.Estado == Enviado && d.Aviso.FechaEnvio != null)
            .OrderByDescending(d => d.Aviso.FechaEnvio)
            .Select(d => new AvisoBandejaViewModel
            {
                AvisoId = d.AvisoId,
                Titulo = d.Aviso.Titulo,
                FechaEnvio = d.Aviso.FechaEnvio!.Value,
                FechaLectura = d.FechaLectura
            })
            .ToListAsync();

        return View(avisos);
    }

    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> MiDetalle(int id)
    {
        var usuarioId = ObtenerUsuarioAutenticadoId();
        if (usuarioId == null)
        {
            return Forbid();
        }

        var modelo = await _contexto.AvisosDestinatarios.AsNoTracking()
            .Where(d => d.AvisoId == id && d.UsuarioId == usuarioId.Value && d.Aviso.Estado == Enviado)
            .Select(d => new AvisoBandejaDetalleViewModel
            {
                AvisoId = d.AvisoId,
                Titulo = d.Aviso.Titulo,
                Mensaje = d.Aviso.Mensaje,
                FechaEnvio = d.Aviso.FechaEnvio ?? d.Aviso.FechaCreacion,
                FechaLectura = d.FechaLectura
            })
            .FirstOrDefaultAsync();

        if (modelo == null)
        {
            return NotFound();
        }

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> MarcarLeido(int id)
    {
        var usuarioId = ObtenerUsuarioAutenticadoId();
        if (usuarioId == null)
        {
            return Forbid();
        }

        var esDestinatario = await _contexto.AvisosDestinatarios
            .AnyAsync(d => d.AvisoId == id && d.UsuarioId == usuarioId.Value && d.Aviso.Estado == Enviado);

        if (!esDestinatario)
        {
            return NotFound();
        }

        try
        {
            var fechaLectura = DateTime.UtcNow;
            await _contexto.AvisosDestinatarios
                .Where(d => d.AvisoId == id && d.UsuarioId == usuarioId.Value && d.FechaLectura == null)
                .ExecuteUpdateAsync(actualizacion => actualizacion.SetProperty(d => d.FechaLectura, fechaLectura));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar lectura del aviso {AvisoId} para el usuario {UsuarioId}.", id, usuarioId.Value);
            TempData["Error"] = "No se pudo actualizar el estado de lectura. Intenta de nuevo.";
            return RedirectToAction(nameof(MiDetalle), new { id });
        }

        TempData["Exito"] = "Aviso marcado como leído.";
        return RedirectToAction(nameof(MiDetalle), new { id });
    }

    private async Task<List<int>> ObtenerDestinatariosAsync(AvisoFormularioViewModel entrada)
    {
        if (!entrada.TodosClientesActivos && (entrada.ClienteIds == null || entrada.ClienteIds.Count == 0))
        {
            ModelState.AddModelError(nameof(entrada.ClienteIds), "Selecciona uno o más destinatarios, o elige todos los clientes activos.");
            return new List<int>();
        }

        if (entrada.TodosClientesActivos)
        {
            var todos = await _contexto.Clientes
                .Where(c => c.EstaActivo && c.Usuario.EstaActivo)
                .Select(c => c.UsuarioId)
                .Distinct()
                .ToListAsync();

            if (todos.Count == 0)
            {
                ModelState.AddModelError(nameof(entrada.TodosClientesActivos), "No hay clientes activos disponibles para recibir el aviso.");
            }

            return todos;
        }

        var clienteIds = entrada.ClienteIds.Distinct().ToList();
        var seleccionados = await _contexto.Clientes
            .Where(c => clienteIds.Contains(c.ClienteId) && c.EstaActivo && c.Usuario.EstaActivo)
            .Select(c => new { c.ClienteId, c.UsuarioId })
            .ToListAsync();

        if (seleccionados.Count != clienteIds.Count)
        {
            ModelState.AddModelError(nameof(entrada.ClienteIds), "Uno o más clientes seleccionados ya no están activos o no existen.");
        }

        return seleccionados.Select(c => c.UsuarioId).Distinct().ToList();
    }

    private async Task CargarClientesDisponiblesAsync(AvisoFormularioViewModel modelo)
    {
        modelo.ClientesDisponibles = await _contexto.Clientes
            .Where(c => c.EstaActivo && c.Usuario.EstaActivo)
            .OrderBy(c => c.Usuario.NombreCompleto)
            .Select(c => new ClienteAvisoOpcionViewModel
            {
                ClienteId = c.ClienteId,
                UsuarioId = c.UsuarioId,
                NombreCompleto = c.Usuario.NombreCompleto,
                CodigoClienteFrecuente = c.CodigoClienteFrecuente
            })
            .ToListAsync();
    }

    private async Task<List<ClienteAvisoOpcionViewModel>> ObtenerTodosLosClientesAsync()
    {
        return await _contexto.Clientes
            .OrderBy(c => c.Usuario.NombreCompleto)
            .Select(c => new ClienteAvisoOpcionViewModel
            {
                ClienteId = c.ClienteId,
                UsuarioId = c.UsuarioId,
                NombreCompleto = c.Usuario.NombreCompleto,
                CodigoClienteFrecuente = c.CodigoClienteFrecuente
            })
            .ToListAsync();
    }

    private int? ObtenerUsuarioAutenticadoId()
    {
        var valor = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(valor, out var usuarioId) ? usuarioId : null;
    }
}
