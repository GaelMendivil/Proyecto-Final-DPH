using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using WhosThatPokemon.Models.ViewModels; 
using WhosThatPokemon.Services;

namespace WhosThatPokemon.Controllers
{
    [Authorize]
    public class ZoomGameController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IPokemonListService _pokemonListService;

        public ZoomGameController(IHttpClientFactory httpClientFactory, IPokemonListService pokemonListService)
        {
            _httpClientFactory = httpClientFactory;
            _pokemonListService = pokemonListService;
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
            ViewBag.IsGameOver = false; 

            return View("Index");
        }

        [HttpGet]
        [Route("/ZoomGame/SearchPokemon")]
        public async Task<IActionResult> SearchPokemon(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 2)
            {
                return Json(new List<object>());
            }
            var allGens = new List<int>(); 
            var results = await _pokemonListService.SearchAsync(term, allGens);

            var jsonResults = results
                .Select(p => new 
                {
                    name = p.Name,
                    imageUrl = p.ImageUrl 
                })
                .Take(10) 
                .ToList();

            return Json(jsonResults);
        }

        [HttpPost]
        public async Task<IActionResult> Guess(string guess)
        {
            int? id = HttpContext.Session.GetInt32("PokemonId");
            int? attempts = HttpContext.Session.GetInt32("Attempts");
            int? zoom = HttpContext.Session.GetInt32("ZoomLevel");
            string pokemonName = HttpContext.Session.GetString("PokemonName");
            bool isGameOver = false;

            if (id == null)
                return RedirectToAction("Index");

            guess = guess ?? ""; 

            bool correct = string.Equals(guess.Trim(), pokemonName, StringComparison.OrdinalIgnoreCase);

            if (correct)
            {
                ViewBag.Message = $"Correct! It was {pokemonName}.";
                
                zoom = 0; 
                HttpContext.Session.SetInt32("ZoomLevel", 0);
                
                isGameOver = true;
            }
            else
            {
                attempts++;
                HttpContext.Session.SetInt32("Attempts", attempts.Value);

                // Reducimos el zoom si falló
                zoom = Math.Max(0, zoom.Value - 2);
                HttpContext.Session.SetInt32("ZoomLevel", zoom.Value);

                if (attempts >= 6)
                {

                    ViewBag.Message = $"Out of attempts. It was {pokemonName}.";
                    isGameOver = true;
                    zoom = 0; 
                }
                else
                {

                    ViewBag.Message = $"Wrong! Try again.";
                }
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
            ViewBag.IsGameOver = isGameOver; 

            return View("Index");
        }
    }
}