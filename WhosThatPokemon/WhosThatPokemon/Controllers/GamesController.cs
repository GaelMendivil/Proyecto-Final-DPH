using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace WhosThatPokemon.Controllers
{
    [Authorize]
    public class GamesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Classic()
        {
            return View(); 
        }

        public IActionResult Pokedex()
        {
            return View(); 
        }
        public IActionResult Zoom()
        {
            return View(); 
        }
    }
}
