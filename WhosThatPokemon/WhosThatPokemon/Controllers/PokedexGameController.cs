using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace WhosThatPokemon.Controllers
{
    [Route("[controller]")]
    [Authorize]
    public class PokedexGameController : Controller
    {
        private readonly HttpClient _httpClient;

        public PokedexGameController()
        {
            _httpClient = new HttpClient();
        }

        private static string currentPokemon = "";
        private static string description = "";
        private static string spriteUrl = "";
        private static string type = "";
        private static int attempts = 0;

        [HttpGet]
        [Route("/Pokedex")]
        public async Task<IActionResult> Index()
        {
            await GetRandomPokemon();
            ViewBag.Description = description;
            ViewBag.Attempts = attempts;
            ViewBag.ShowType = false;
            ViewBag.Sprite = null;
            ViewBag.Message = null;
            return View("~/Views/PokedexGame/Index.cshtml");
        }

// Autocomplete
[HttpGet]
[Route("/PokedexGame/SearchPokemon")]
public async Task<IActionResult> SearchPokemon(string term)
{
    if (string.IsNullOrEmpty(term) || term.Length < 2)
        return Json(new List<object>());

    string url = "https://pokeapi.co/api/v2/pokemon?limit=2000";

    var response = await _httpClient.GetStringAsync(url);
    var json = JObject.Parse(response);

    var results = json["results"]
        .Where(p => p["name"].ToString()
        .IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
        .Select(p => new {
            name = p["name"].ToString(),
            image = $"https://img.pokemondb.net/sprites/home/normal/{p["name"]}.png"
        })
        .Take(10)
        .ToList();

    return Json(results);
}



//Adivinar Pokémon
        [HttpPost]
        [Route("/Pokedex/Guess")]
        public IActionResult Guess(string guess)
        {
            if (attempts >= 6)
            {
                ViewBag.Message = $"Has perdido. El Pokémon era {currentPokemon.ToUpper()}";
                ViewBag.Sprite = spriteUrl;
                ViewBag.Type = type;
                ViewBag.Description = description;
                ViewBag.ShowType = true;
                return View("~/Views/PokedexGame/Index.cshtml");
            }

            attempts++;

            if (guess?.Trim().ToLower() == currentPokemon.ToLower())
            {
                ViewBag.Message = $"¡Correcto! Era {currentPokemon.ToUpper()}";
                ViewBag.Sprite = spriteUrl;
                ViewBag.Type = type;
                ViewBag.ShowType = true;
                attempts = 0;
            }
            else if (attempts == 2)
            {
                ViewBag.Message = $"Segunda pista: es tipo '{type}'";
                ViewBag.Description = description;
                ViewBag.ShowType = true;
            }
            else if (attempts == 6)
            {
                ViewBag.Message = $"Has perdido. El Pokémon era {currentPokemon.ToUpper()}";
                ViewBag.Sprite = spriteUrl;
                ViewBag.Type = type;
                ViewBag.ShowType = true;
            }
            else
            {
                ViewBag.Message = "No es correcto. ¡Intenta otra vez!";
                ViewBag.ShowType = false;
            }

            ViewBag.Description = description;
            ViewBag.Attempts = attempts;

            return View("~/Views/PokedexGame/Index.cshtml");
        }

        private async Task GetRandomPokemon()
        {
            var random = new Random();
            int id = random.Next(1, 151);

            string url = $"https://pokeapi.co/api/v2/pokemon-species/{id}/";
            string pokeUrl = $"https://pokeapi.co/api/v2/pokemon/{id}/";

            var response = await _httpClient.GetStringAsync(url);
            var json = JObject.Parse(response);

            description = json["flavor_text_entries"]
                .First(x => x["language"]["name"].ToString() == "en")["flavor_text"]
                .ToString()
                .Replace("\n", " ")
                .Replace("\f", " ");

            var pokeResponse = await _httpClient.GetStringAsync(pokeUrl);
            var pokeJson = JObject.Parse(pokeResponse);

            spriteUrl = pokeJson["sprites"]["front_default"].ToString();
            currentPokemon = pokeJson["name"].ToString();
            type = pokeJson["types"][0]["type"]["name"].ToString();

            attempts = 0;
        }
    }

}
