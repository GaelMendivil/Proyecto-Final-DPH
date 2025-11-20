using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;


namespace WhosThatPokemon.Controllers
{
    public class ZoomGameController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ZoomGameController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient();

            var random = new Random();
            int id = random.Next(1, 898);

            var response = await client.GetStringAsync($"https://pokeapi.co/api/v2/pokemon/{id}/");
            var json = JsonDocument.Parse(response);

            string sprite = json.RootElement
                .GetProperty("sprites")
                .GetProperty("other")
                .GetProperty("official-artwork")
                .GetProperty("front_default")
                .GetString();

            HttpContext.Session.SetInt32("PokemonId", id);
            HttpContext.Session.SetInt32("Attempts", 0);
            HttpContext.Session.SetInt32("ZoomLevel", 10); 
            HttpContext.Session.SetString("PokemonName", json.RootElement.GetProperty("name").GetString());

            ViewBag.Sprite = sprite;
            ViewBag.Attempts = 0;
            ViewBag.ZoomLevel = 10;

            return View("Index");
        }
[HttpGet]
[Route("/ZoomGame/SearchPokemon")]
public async Task<IActionResult> SearchPokemon(string term)
{
    if (string.IsNullOrEmpty(term) || term.Length < 2)
        return Json(new List<PokemonSearchViewModel>());

    string url = "https://pokeapi.co/api/v2/pokemon?limit=2000";

    var client = _httpClientFactory.CreateClient();
    var response = await client.GetStringAsync(url);

    var json = JObject.Parse(response);

    var results = json["results"]
        .Where(p => p["name"].ToString().IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
        .Select(p => new PokemonSearchViewModel
        {
            Name = p["name"].ToString(),
            ImageUrl = $"https://img.pokemondb.net/sprites/home/normal/{p["name"]}.png"
        })
        .Take(7)
        .ToList();

    return Json(results);
}



        [HttpPost]
        public async Task<IActionResult> Guess(string guess)
        {
            int? id = HttpContext.Session.GetInt32("PokemonId");
            int? attempts = HttpContext.Session.GetInt32("Attempts");
            int? zoom = HttpContext.Session.GetInt32("ZoomLevel");
            string pokemonName = HttpContext.Session.GetString("PokemonName");

            if (id == null)
                return RedirectToAction("Index");

            attempts++;
            HttpContext.Session.SetInt32("Attempts", attempts.Value);

            bool correct = string.Equals(guess, pokemonName, StringComparison.OrdinalIgnoreCase);

            if (correct)
            {
                ViewBag.Message = $"¡Correcto! Era {pokemonName}.";
                ViewBag.ZoomLevel = 0;
            }
            else
            {
                zoom = Math.Max(0, zoom.Value - 2);
                HttpContext.Session.SetInt32("ZoomLevel", zoom.Value);

                if (attempts >= 6)
                    ViewBag.Message = $"Se acabaron los intentos. Era {pokemonName}.";
                else
                    ViewBag.Message = $"Incorrecto. Intenta de nuevo.";
            }

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetStringAsync($"https://pokeapi.co/api/v2/pokemon/{id}/");
            var json = JsonDocument.Parse(response);

            string sprite = json.RootElement
                .GetProperty("sprites")
                .GetProperty("other")
                .GetProperty("official-artwork")
                .GetProperty("front_default")
                .GetString();

            ViewBag.Sprite = sprite;
            ViewBag.ZoomLevel = zoom;
            ViewBag.Attempts = attempts;

            return View("Index");
        }
    }
}
