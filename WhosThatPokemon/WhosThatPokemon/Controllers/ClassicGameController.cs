    using Microsoft.AspNetCore.Mvc;
    using WhosThatPokemon.Models.ViewModels; // <-- AÑADIDO: Para usar ClassicGameFilterViewModel
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
            private readonly IPokemonListService _pokemonListService;

            public ClassicGameController(IPokemonListService pokemonListService)
            {
                _pokemonListService = pokemonListService;
            }

            // Helpers para filtros en sesión 
            private ClassicGameFilterViewModel GetFilters()
            {
                var filters = HttpContext.Session.Get<ClassicGameFilterViewModel>("Filters");
                if (filters == null)
                {
                    filters = new ClassicGameFilterViewModel();
                    // Poner todas las generaciones por defecto si la sesión está vacía
                    filters.SelectedGenerations = Enumerable.Range(1, 9).ToList();
                    HttpContext.Session.Set("Filters", filters);
                }
                // Asegurarse de que si la sesión existe pero sin gens, se pongan todas
                else if (!filters.SelectedGenerations.Any())
                {
                    filters.SelectedGenerations = Enumerable.Range(1, 9).ToList();
                }
                return filters;
            }

            private void SaveFilters(ClassicGameFilterViewModel filters)
            {
                HttpContext.Session.Set("Filters", filters);
            }
            // GET: INDEX 
            [HttpGet]
            public async Task<IActionResult> Index([FromQuery] List<int> gens)
            {
                var filters = GetFilters();

                // Si la URL trae 'gens', los usamos y guardamos en sesión.
                if (gens != null && gens.Any())
                {
                    filters.SelectedGenerations = gens;
                }
                // Si no, GetFilters() ya nos dio los de la sesión o todos por defecto.
                
                SaveFilters(filters); // Guardamos los filtros actualizados

                // Usar el servicio para obtener un Pokémon aleatorio filtrado
                var mysteryPokemon = await _pokemonListService.GetRandomAsync(filters.SelectedGenerations);

                // Fallback: Si el filtro no devuelve nada 
                if (mysteryPokemon == null)
                {
                    var allGens = Enumerable.Range(1, 9).ToList();
                    mysteryPokemon = await _pokemonListService.GetRandomAsync(allGens);

                    if (mysteryPokemon == null) {
                        TempData["ErrorMessage"] = "Error: No se pudo cargar ningún Pokémon de la API.";
                        return View(new GameSessionViewModel());
                    }
                }

                // Guardar el Pokémon misterioso
                HttpContext.Session.Set("MysteryPokemon", mysteryPokemon);
                HttpContext.Session.Remove("CurrentGuesses");

                var gameSession = new GameSessionViewModel
                {
                    Guesses = new List<GuessResultViewModel>()
                };

                // Pasar los filtros a la vista para que los switches se marquen
                ViewBag.SelectedGens = filters.SelectedGenerations;

                return View(gameSession);
            }



            // POST: GUESS 
            [HttpPost]
            public async Task<IActionResult> Guess(GameSessionViewModel model)
            {
                var filters = GetFilters();
                // Pasar los filtros al ViewBag en cada recarga de vista
                ViewBag.SelectedGens = filters.SelectedGenerations;

                var guessedPokemonName = model.CurrentGuessName;
                if (string.IsNullOrWhiteSpace(guessedPokemonName))
                {
                    TempData["ErrorMessage"] = "Por favor, introduce un nombre de Pokémon.";
                    // Al redirigir a Index se re-cargan los filtros correctos
                    return RedirectToAction(nameof(Index)); 
                }

                var mysteryPokemon = HttpContext.Session.Get<PokemonViewModel>("MysteryPokemon");
                if (mysteryPokemon == null)
                {
                    return RedirectToAction(nameof(Index)); // Sesión expirada
                }

                var currentGuesses = HttpContext.Session.Get<List<GuessResultViewModel>>("CurrentGuesses") ?? new List<GuessResultViewModel>();

                // Usar el caché en lugar de la API directamente
                var allPokemon = await _pokemonListService.GetAllAsync();
                var tempDataPokemon = allPokemon.FirstOrDefault(p => p.Name.Equals(guessedPokemonName, System.StringComparison.OrdinalIgnoreCase));

                if (tempDataPokemon == null)
                {
                    TempData["ErrorMessage"] = $"No se encontró un Pokémon con el nombre '{guessedPokemonName}'.";
                    model.Guesses = currentGuesses;
                    return View("Index", model); // Devuelve la vista con el error y los filtros
                }

                // Validación de generación 
                if (filters.SelectedGenerations.Any() &&
                    !filters.SelectedGenerations.Contains(tempDataPokemon.Generation))
                {
                    TempData["ErrorMessage"] = $"El Pokémon '{guessedPokemonName}' no pertenece a las generaciones seleccionadas.";
                    model.Guesses = currentGuesses;
                    return View("Index", model); // Devuelve la vista con el error y los filtros
                }

                var guessedPokemon = tempDataPokemon;
                var guessResult = ComparePokemon(guessedPokemon, mysteryPokemon); 
                currentGuesses.Insert(0, guessResult);

                HttpContext.Session.Set("CurrentGuesses", currentGuesses);

                model.Guesses = currentGuesses;
                model.HasGuessedCorrectly = guessedPokemon.Name.Equals(mysteryPokemon.Name, System.StringComparison.OrdinalIgnoreCase);

                if (model.HasGuessedCorrectly)
                {
                    HttpContext.Session.Remove("MysteryPokemon");
                }
                else
                {
                    model.CurrentGuessName = ""; // Limpiar el input
                }

                return View("Index", model);
            }

            // GET: AUTOCOMPLETE 
            [HttpGet]
            public async Task<IActionResult> SearchPokemon(string term)
            {
                if (string.IsNullOrEmpty(term) || term.Length < 2)
                {
                    return Json(new List<PokemonSearchViewModel>());
                }

                var filters = GetFilters();
                
                // Obtener la lista filtrada por generación desde el servicio
                var list = await _pokemonListService.FilterByGenerationsAsync(filters.SelectedGenerations);

                // Filtrar esa lista por el término de búsqueda
                var results = list
                    .Where(p => p.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    .Select(p => new PokemonSearchViewModel
                    {
                        Name = p.Name,
                        ImageUrl = p.ImageUrl
                    })
                    .Take(7) 
                    .ToList();

                return Json(results);
            }

            // ComparePokemon 
            private GuessResultViewModel ComparePokemon(PokemonViewModel guessed, PokemonViewModel mystery)
            {
                var result = new GuessResultViewModel
                {
                    Name = guessed.Name,
                    ImageUrl = guessed.ImageUrl,
                    Type1 = guessed.Type1,
                    Type2 = guessed.Type2,
                    EvolutionStage = guessed.EvolutionStage,
                    Color = guessed.Color,
                    Generation = guessed.Generation,
                    Height = guessed.Height,
                    Weight = guessed.Weight
                };

                
                if (!string.IsNullOrEmpty(guessed.Type1) && !string.IsNullOrEmpty(mystery.Type1) && guessed.Type1.Equals(mystery.Type1, System.StringComparison.OrdinalIgnoreCase))
                    result.Type1Color = "green";
                else if (!string.IsNullOrEmpty(guessed.Type1) && !string.IsNullOrEmpty(mystery.Type2) && guessed.Type1.Equals(mystery.Type2, System.StringComparison.OrdinalIgnoreCase))
                    result.Type1Color = "yellow";
                else
                    result.Type1Color = "red";

                if (string.IsNullOrEmpty(guessed.Type2) && string.IsNullOrEmpty(mystery.Type2))
                    result.Type2Color = "green";
                else if (!string.IsNullOrEmpty(guessed.Type2) && !string.IsNullOrEmpty(mystery.Type2) && guessed.Type2.Equals(mystery.Type2, System.StringComparison.OrdinalIgnoreCase))
                    result.Type2Color = "green";
                else if (!string.IsNullOrEmpty(guessed.Type2) && !string.IsNullOrEmpty(mystery.Type1) && mystery.Type1.Equals(guessed.Type2, System.StringComparison.OrdinalIgnoreCase))
                    result.Type2Color = "yellow";
                else
                    result.Type2Color = "red";

                if (!string.IsNullOrEmpty(guessed.Color) && !string.IsNullOrEmpty(mystery.Color) && guessed.Color.Equals(mystery.Color, System.StringComparison.OrdinalIgnoreCase))
                    result.ColorColor = "green";
                else
                    result.ColorColor = "red";

                result.EvolutionStageColor = guessed.EvolutionStage == mystery.EvolutionStage ? "green" : "red";
                result.GenerationColor = guessed.Generation == mystery.Generation ? "green" : "red";

                if (System.Math.Abs(guessed.Height - mystery.Height) < 0.1)
                    result.HeightColor = "green";
                else if (guessed.Height < mystery.Height)
                    result.HeightColor = "red-up"; 
                else
                    result.HeightColor = "red-down";

                if (System.Math.Abs(guessed.Weight - mystery.Weight) < 0.5)
                    result.WeightColor = "green";
                else if (guessed.Weight < mystery.Weight)
                    result.WeightColor = "red-up";
                else
                    result.WeightColor = "red-down";


                return result;
            }
        }

        // Extensiones de sesion 
        public static class SessionExtensions
        {
            public static void Set<T>(this ISession session, string key, T value)
            {
                session.SetString(key, JsonConvert.SerializeObject(value));
            }

            public static T Get<T>(this ISession session, string key)
            {
                var value = session.GetString(key);
                return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value);
            }
        }
    }