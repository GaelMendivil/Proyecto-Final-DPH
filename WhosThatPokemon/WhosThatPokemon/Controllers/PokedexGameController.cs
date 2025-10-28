using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace WhosThatPokemon.Controllers
{
    [Route("[controller]")]
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
        [Route("/Pokedex")] // 
        public async Task<IActionResult> Index()
        {
            await GetRandomPokemon();
            ViewBag.Description = description;
            ViewBag.Attempts = attempts;
            ViewBag.ShowType = false;
            ViewBag.Sprite = null;
            return View("~/Views/PokedexGame/Index.cshtml");
        }

        [HttpPost]
        [Route("/Pokedex/Guess")] 
        public IActionResult Guess(string guess)
        {
            attempts++;

            if (guess?.Trim().ToLower() == currentPokemon.ToLower())
            {
                ViewBag.Message = $"¡Correcto! Era {currentPokemon.ToUpper()} 🎉";
                ViewBag.Sprite = spriteUrl;
                ViewBag.Description = description;
                ViewBag.Type = type;
                ViewBag.ShowType = true;
                attempts = 0;
            }
            else if (attempts == 2)
            {
                ViewBag.Message = "Segunda pista: el tipo es " + type;
                ViewBag.Description = description;
                ViewBag.ShowType = true;
            }
            else
            {
                ViewBag.Message = "No es correcto. ¡Intenta otra vez!";
                ViewBag.Description = description;
                ViewBag.ShowType = false;
            }

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
            var flavor = json["flavor_text_entries"]
                .First(x => x["language"]["name"].ToString() == "en")["flavor_text"]
                .ToString()
                .Replace("\n", " ")
                .Replace("\f", " ");

            var pokeResponse = await _httpClient.GetStringAsync(pokeUrl);
            var pokeJson = JObject.Parse(pokeResponse);

            spriteUrl = pokeJson["sprites"]["front_default"].ToString();
            currentPokemon = pokeJson["name"].ToString();
            type = pokeJson["types"][0]["type"]["name"].ToString();
            description = flavor;
            attempts = 0;
        }
    }
}
