using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AOTech.Data.Contexto;
using AOTech.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AOTech.Web.Controllers;

public class AccountController : Controller
{
    private readonly AOTechDbContext _contexto;

    public AccountController(AOTechDbContext contexto)
    {
        _contexto = contexto;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel modelo, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var usuario = await _contexto.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.NombreUsuario == modelo.NombreUsuario);

        if (usuario == null || !usuario.EstaActivo)
        {
            ModelState.AddModelError(string.Empty, "Nombre de usuario o contraseña incorrectos.");
            return View(modelo);
        }

        byte[] passwordBytes = Encoding.UTF8.GetBytes(modelo.Contrasena);
        byte[] computedHash = SHA256.HashData(passwordBytes.Concat(usuario.ContrasenaSalt).ToArray());

        if (!CryptographicOperations.FixedTimeEquals(computedHash, usuario.ContrasenaHash))
        {
            ModelState.AddModelError(string.Empty, "Nombre de usuario o contraseña incorrectos.");
            return View(modelo);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
            new Claim(ClaimTypes.Name, usuario.NombreUsuario),
            new Claim("NombreCompleto", usuario.NombreCompleto ?? string.Empty)
        };

        if (usuario.Rol != null && !string.IsNullOrWhiteSpace(usuario.Rol.NombreRol))
        {
            claims.Add(new Claim(ClaimTypes.Role, usuario.Rol.NombreRol));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
