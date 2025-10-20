using Microsoft.AspNetCore.Mvc;

namespace WhosThatPokemon.Controllers
{
    public class GamesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult ClassicGame()
        {
            return View(); 
        }

        public IActionResult Pokedex()
        {
            return View(); 
        }
    }       
}