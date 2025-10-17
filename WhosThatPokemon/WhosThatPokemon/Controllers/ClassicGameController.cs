using Microsoft.AspNetCore.Mvc;
using WhosThatPokemon.Models.ViewModels;
using WhosThatPokemon.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace WhosThatPokemon.Controllers
{
    public class PokemonSearchViewModel
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }
    }

    public class ClassicGameController : Controller
    {
        private readonly IPokemonApiService _pokemonApiService;
        private static List<string> _allPokemonNames;

        public ClassicGameController(IPokemonApiService pokemonApiService)
        {
            _pokemonApiService = pokemonApiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (_allPokemonNames == null || !_allPokemonNames.Any())
            {
                _allPokemonNames = await _pokemonApiService.GetAllPokemonNamesAsync();
            }

            var random = new Random();
            var mysteryPokemonName = _allPokemonNames[random.Next(_allPokemonNames.Count)];

            HttpContext.Session.SetString("MysteryPokemonName", mysteryPokemonName);
            HttpContext.Session.Remove("CurrentGuesses");

            var gameSession = new GameSessionViewModel
            {
                Guesses = new List<GuessResultViewModel>()
            };

            return View(gameSession);
        }

        [HttpPost]
        public async Task<IActionResult> Guess(GameSessionViewModel model)
        {
            var guessedPokemonName = model.CurrentGuessName;
            if (string.IsNullOrWhiteSpace(guessedPokemonName))
            {
                TempData["ErrorMessage"] = "Por favor, introduce un nombre de Pokémon.";
                return RedirectToAction(nameof(Index));
            }

            var mysteryPokemonName = HttpContext.Session.GetString("MysteryPokemonName");
            if (string.IsNullOrEmpty(mysteryPokemonName))
            {
                return RedirectToAction(nameof(Index));
            }

            var mysteryPokemon = await _pokemonApiService.GetPokemonDataAsync(mysteryPokemonName);
            var guessedPokemon = await _pokemonApiService.GetPokemonDataAsync(guessedPokemonName);
            var currentGuesses = HttpContext.Session.Get<List<GuessResultViewModel>>("CurrentGuesses") ?? new List<GuessResultViewModel>();

            if (guessedPokemon == null || mysteryPokemon == null)
            {
                TempData["ErrorMessage"] = $"Hubo un error al obtener los datos de un Pokémon. Inténtalo de nuevo.";
                model.Guesses = currentGuesses;
                return View("Index", model);
            }

            var guessResult = ComparePokemon(guessedPokemon, mysteryPokemon);
            currentGuesses.Insert(0, guessResult);
            HttpContext.Session.Set("CurrentGuesses", currentGuesses);

            model.Guesses = currentGuesses;
            model.HasGuessedCorrectly = guessedPokemon.Name.Equals(mysteryPokemon.Name, System.StringComparison.OrdinalIgnoreCase);

            if (model.HasGuessedCorrectly)
            {
                HttpContext.Session.Remove("MysteryPokemonName");
            }
            else
            {
                model.CurrentGuessName = "";
            }

            return View("Index", model);
        }

        [HttpGet]
        public async Task<IActionResult> SearchPokemon(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 2)
            {
                return Json(new List<PokemonSearchViewModel>());
            }

            if (_allPokemonNames == null || !_allPokemonNames.Any())
            {
                _allPokemonNames = await _pokemonApiService.GetAllPokemonNamesAsync();
            }

            var filteredNames = _allPokemonNames
                .Where(p => p.StartsWith(term, System.StringComparison.OrdinalIgnoreCase))
                .Take(7)
                .ToList();

            var results = new List<PokemonSearchViewModel>();
            foreach (var name in filteredNames)
            {
                var pokemonData = await _pokemonApiService.GetPokemonDataAsync(name);
                if (pokemonData != null)
                {
                    results.Add(new PokemonSearchViewModel { Name = name, ImageUrl = pokemonData.ImageUrl });
                }
            }

            return Json(results);
        }

        private GuessResultViewModel ComparePokemon(PokemonViewModel guessed, PokemonViewModel mystery)
        {
            var result = new GuessResultViewModel { Name = guessed.Name, ImageUrl = guessed.ImageUrl, Type1 = guessed.Type1, Type2 = guessed.Type2, EvolutionStage = guessed.EvolutionStage, Color = guessed.Color, Generation = guessed.Generation, Height = guessed.Height, Weight = guessed.Weight };
            if (!string.IsNullOrEmpty(guessed.Type1) && !string.IsNullOrEmpty(mystery.Type1) && guessed.Type1.Equals(mystery.Type1, System.StringComparison.OrdinalIgnoreCase)) result.Type1Color = "green";
            else if (!string.IsNullOrEmpty(guessed.Type1) && !string.IsNullOrEmpty(mystery.Type2) && guessed.Type1.Equals(mystery.Type2, System.StringComparison.OrdinalIgnoreCase)) result.Type1Color = "yellow";
            else result.Type1Color = "red";
            if (string.IsNullOrEmpty(guessed.Type2) && string.IsNullOrEmpty(mystery.Type2)) result.Type2Color = "green";
            else if (!string.IsNullOrEmpty(guessed.Type2) && !string.IsNullOrEmpty(mystery.Type2) && guessed.Type2.Equals(mystery.Type2, System.StringComparison.OrdinalIgnoreCase)) result.Type2Color = "green";
            else if (!string.IsNullOrEmpty(guessed.Type2) && !string.IsNullOrEmpty(mystery.Type1) && mystery.Type1.Equals(guessed.Type2, System.StringComparison.OrdinalIgnoreCase)) result.Type2Color = "yellow";
            else result.Type2Color = "red";
            if (!string.IsNullOrEmpty(guessed.Color) && !string.IsNullOrEmpty(mystery.Color) && guessed.Color.Equals(mystery.Color, System.StringComparison.OrdinalIgnoreCase)) result.ColorColor = "green";
            else result.ColorColor = "red";
            result.EvolutionStageColor = guessed.EvolutionStage == mystery.EvolutionStage ? "green" : "red";
            result.GenerationColor = guessed.Generation == mystery.Generation ? "green" : "red";
            if (System.Math.Abs(guessed.Height - mystery.Height) < 0.1) result.HeightColor = "green";
            else if (guessed.Height < mystery.Height) result.HeightColor = "red-up";
            else result.HeightColor = "red-down";
            if (System.Math.Abs(guessed.Weight - mystery.Weight) < 0.5) result.WeightColor = "green";
            else if (guessed.Weight < mystery.Weight) result.WeightColor = "red-up";
            else result.WeightColor = "red-down";
            return result;
        }
    }

    public static class SessionExtensions
    {
        public static void Set<T>(this ISession session, string key, T value) { session.SetString(key, JsonConvert.SerializeObject(value)); }
        public static T Get<T>(this ISession session, string key) { var value = session.GetString(key); return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value); }
    }
}

