using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using WhosThatPokemon.Services;

namespace WhosThatPokemon.Controllers
{
    [Route("[controller]")]
    [Authorize]
    public class PokedexGameController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IPokemonListService _pokemonListService;

        public PokedexGameController(IPokemonListService pokemonListService)
        {
            _httpClient = new HttpClient();
            _pokemonListService = pokemonListService;
        }

        [HttpGet]
        [Route("/Pokedex")]
        public async Task<IActionResult> Index()
        {
            await StartNewGame();
            UpdateViewBag(0, false);
            return View("~/Views/PokedexGame/Index.cshtml");
        }

        [HttpGet]
        [Route("/PokedexGame/SearchPokemon")]
        public async Task<IActionResult> SearchPokemon(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 2) return Json(new List<object>());

            var allGens = new List<int>(); 
            var results = await _pokemonListService.SearchAsync(term, allGens);

            var jsonResults = results
                .Select(p => new { name = p.Name, imageUrl = p.ImageUrl })
                .Take(10).ToList();

            return Json(jsonResults);
        }

        [HttpPost]
        [Route("/Pokedex/Guess")]
        public IActionResult Guess(string guess)
        {
            string currentPokemon = HttpContext.Session.GetString("Poke_Name");
            int attempts = HttpContext.Session.GetInt32("Poke_Attempts") ?? 0;
            
            if (string.IsNullOrEmpty(currentPokemon)) return RedirectToAction("Index");

            // Lógica de intentos
            attempts++;
            HttpContext.Session.SetInt32("Poke_Attempts", attempts);

            bool isCorrect = false;
            if (!string.IsNullOrWhiteSpace(guess))
            {
                 isCorrect = guess.Trim().ToLower() == currentPokemon.ToLower();
            }
            
            bool isGameOver = false;

            if (isCorrect)
            {
                ViewBag.Message = $"Correct! It was {currentPokemon.ToUpper()}";
                isGameOver = true;
            }
            else if (attempts >= 6)
            {
                ViewBag.Message = $"Game Over. The Pokémon was {currentPokemon.ToUpper()}";
                isGameOver = true;
            }
            else
            {
                ViewBag.Message = "Incorrect. Try again!";
            }

            UpdateViewBag(attempts, isGameOver);
            return View("~/Views/PokedexGame/Index.cshtml");
        }

        private async Task StartNewGame()
        {
            var random = new Random();
            int id = random.Next(1, 1025); 

            string speciesUrl = $"https://pokeapi.co/api/v2/pokemon-species/{id}/";
            string pokeUrl = $"https://pokeapi.co/api/v2/pokemon/{id}/";

            var response = await _httpClient.GetStringAsync(speciesUrl);
            var json = JObject.Parse(response);

            string desc = json["flavor_text_entries"]
                .FirstOrDefault(x => x["language"]["name"].ToString() == "en")?["flavor_text"]
                .ToString()
                .Replace("\n", " ")
                .Replace("\f", " ") ?? "No description available.";
            var pokeResponse = await _httpClient.GetStringAsync(pokeUrl);
            var pokeJson = JObject.Parse(pokeResponse);

            string sprite = pokeJson["sprites"]["front_default"].ToString();
            string name = pokeJson["name"].ToString();
            
            var typesArray = pokeJson["types"].Select(t => t["type"]["name"].ToString().ToUpper()).ToList();
            string type = string.Join(" / ", typesArray);

            HttpContext.Session.SetString("Poke_Name", name);
            HttpContext.Session.SetString("Poke_Desc", desc);
            HttpContext.Session.SetString("Poke_Sprite", sprite);
            HttpContext.Session.SetString("Poke_Type", type);
            HttpContext.Session.SetInt32("Poke_Id", id);
            HttpContext.Session.SetInt32("Poke_Attempts", 0);
        }

        private void UpdateViewBag(int attempts, bool isGameOver)
        {
            string desc = HttpContext.Session.GetString("Poke_Desc");
            string type = HttpContext.Session.GetString("Poke_Type");
            string sprite = HttpContext.Session.GetString("Poke_Sprite");
            int? id = HttpContext.Session.GetInt32("Poke_Id");

            ViewBag.Description = desc; 

            // Pista 1 ID
            ViewBag.PokedexId = (attempts >= 1 || isGameOver) ? $"#{id:D3}" : null;
            ViewBag.ShowId = (attempts >= 1 || isGameOver);

            // Pista 2 Tipos
            ViewBag.Type = type;
            ViewBag.ShowType = (attempts >= 2 || isGameOver);

            ViewBag.Sprite = isGameOver ? sprite : null;
            ViewBag.Attempts = attempts;
            
            ViewBag.IsGameOver = isGameOver;
        }
    }
}