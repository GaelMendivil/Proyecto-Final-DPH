using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WhosThatPokemon.Models;

namespace WhosThatPokemon.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        // ✅ AÑADE ESTA LÍNEA AQUÍ
        // Esto le asigna la clase CSS "home-page" al <body> de la página de inicio.
        ViewData["BodyClass"] = "home-page";
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

