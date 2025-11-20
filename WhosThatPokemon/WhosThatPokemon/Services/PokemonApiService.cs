using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WhosThatPokemon.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using WhosThatPokemon.Controllers;

namespace WhosThatPokemon.Services
{
    public class PokemonApiService : IPokemonApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        public PokemonApiService(HttpClient httpClient, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _cache = memoryCache;
        }

        public async Task<PokemonViewModel> GetPokemonDataAsync(string pokemonName)
        {
            string cacheKey = $"pokemon_{pokemonName.ToLowerInvariant()}";
            if (_cache.TryGetValue(cacheKey, out PokemonViewModel cachedPokemon))
            {
                return cachedPokemon;
            }

            var pokeApiUrl = $"https://pokeapi.co/api/v2/pokemon/{pokemonName.ToLowerInvariant()}";
            var speciesApiUrl = $"https://pokeapi.co/api/v2/pokemon-species/{pokemonName.ToLowerInvariant()}";

            var pokemonResponse = await _httpClient.GetAsync(pokeApiUrl);
            var speciesResponse = await _httpClient.GetAsync(speciesApiUrl);

            if (!pokemonResponse.IsSuccessStatusCode || !speciesResponse.IsSuccessStatusCode)
            {
                return null;
            }

            var pokemonContent = await pokemonResponse.Content.ReadAsStringAsync();
            var speciesContent = await speciesResponse.Content.ReadAsStringAsync();

            dynamic pokemonData = JsonConvert.DeserializeObject(pokemonContent);
            dynamic speciesData = JsonConvert.DeserializeObject(speciesContent);

            string description = GetDescription(speciesData, "es") ?? GetDescription(speciesData, "en");

            var pokemonViewModel = new PokemonViewModel
            {
                Name = (string)pokemonData.name,
                ImageUrl = (string)pokemonData.sprites.front_default,
                Type1 = (string)pokemonData.types[0].type.name,
                Type2 = pokemonData.types.Count > 1 ? (string)pokemonData.types[1].type.name : null,
                Height = (double)pokemonData.height / 10,
                Weight = (double)pokemonData.weight / 10,
                Generation = GetGenerationFromUrl((string)speciesData.generation.url),
                Color = (string)speciesData.color.name,
                EvolutionStage = await GetEvolutionStage((string)pokemonData.name, (string)speciesData.evolution_chain.url),
                Description = description
            };

            _cache.Set(cacheKey, pokemonViewModel, TimeSpan.FromHours(1));

            return pokemonViewModel;
        }

        public async Task<PokemonSearchViewModel> GetPokemonSearchDataAsync(string pokemonName)
        {
            string cacheKey = $"search_{pokemonName.ToLowerInvariant()}";

            if (_cache.TryGetValue(cacheKey, out PokemonSearchViewModel cachedSearchData))
            {
                return cachedSearchData;
            }

            var pokeApiUrl = $"https://pokeapi.co/api/v2/pokemon/{pokemonName.ToLowerInvariant()}";
            var pokemonResponse = await _httpClient.GetAsync(pokeApiUrl);

            if (!pokemonResponse.IsSuccessStatusCode) return null;

            var pokemonContent = await pokemonResponse.Content.ReadAsStringAsync();
            dynamic pokemonData = JsonConvert.DeserializeObject(pokemonContent);

            var searchData = new PokemonSearchViewModel
            {
                Name = (string)pokemonData.name,
                ImageUrl = (string)pokemonData.sprites.front_default
            };

            _cache.Set(cacheKey, searchData, TimeSpan.FromHours(1));
            return searchData;
        }


        private string GetDescription(dynamic speciesData, string language)
        {
            foreach (var entry in speciesData.flavor_text_entries)
            {
                if (entry.language.name == language)
                {
                    return ((string)entry.flavor_text).Replace("\n", " ").Replace("\f", " ");
                }
            }
            return null;
        }

        private int GetGenerationFromUrl(string url)
        {
            var parts = url.Split('/');
            return int.Parse(parts[parts.Length - 2]);
        }

        // Función para obtener la etapa de evolución
        private async Task<int> GetEvolutionStage(string pokemonName, string evolutionChainUrl)
        {
            string cacheKey = $"evo_{evolutionChainUrl}";

            // Usamos 'object' para guardar los datos en el caché
            if (!_cache.TryGetValue(cacheKey, out object evolutionDataObj))
            {
                // No está en caché, se busca en la API
                try
                {
                    var evolutionResponse = await _httpClient.GetAsync(evolutionChainUrl);
                    if (!evolutionResponse.IsSuccessStatusCode) return 1;

                    var evolutionContent = await evolutionResponse.Content.ReadAsStringAsync();
                    
                    dynamic dynamicData = JsonConvert.DeserializeObject(evolutionContent);

                    _cache.Set(cacheKey, (object)dynamicData, TimeSpan.FromDays(1));

                    // Asignar el nuevo dato al objeto que usaremos
                    evolutionDataObj = dynamicData;
                }
                catch
                {
                    return 1;
                }
            }

            // Convertir el 'object' a 'dynamic' para poder leerlo
            dynamic evolutionData = (dynamic)evolutionDataObj;

            int stage = FindStageInChain(evolutionData.chain, pokemonName, 1);
            return stage == 0 ? 1 : stage;
        }

        private int FindStageInChain(dynamic chainLink, string pokemonName, int currentStage)
        {
            if (chainLink.species.name == pokemonName)
            {
                return currentStage;
            }

            if (chainLink.evolves_to != null && chainLink.evolves_to.Count > 0)
            {
                foreach (var nextLink in chainLink.evolves_to)
                {
                    int foundStage = FindStageInChain(nextLink, pokemonName, currentStage + 1);
                    if (foundStage > 0)
                    {
                        return foundStage;
                    }
                }
            }
            return 0;
        }
        public async Task<List<PokemonViewModel>> GetAllPokemonAsync()
        {
            const string cacheKey = "all_pokemon_data_full";
            if (_cache.TryGetValue(cacheKey, out List<PokemonViewModel> cachedAll))
            {
                return cachedAll;
            }

            // Obtener todos los nombres 
            var names = await GetAllPokemonNamesAsync();

            var results = new List<PokemonViewModel>();
            var semaphore = new SemaphoreSlim(10);
            var tasks = new List<Task>();

            foreach (var name in names)
            {
                await semaphore.WaitAsync();
                var t = Task.Run(async () =>
                {
                    try
                    {
                        var p = await GetPokemonDataAsync(name);
                        if (p != null)
                        {
                            lock (results)
                            {
                                results.Add(p);
                            }
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                tasks.Add(t);
            }

            await Task.WhenAll(tasks);

            // Guardar en cache por 12 horas 
            _cache.Set(cacheKey, results, TimeSpan.FromHours(12));

            return results;
        }


        public async Task<List<string>> GetAllPokemonNamesAsync()
        {
            string cacheKey = "all_pokemon_names";

            if (_cache.TryGetValue(cacheKey, out List<string> cachedNames))
            {
                return cachedNames;
            }

            var response = await _httpClient.GetAsync("https://pokeapi.co/api/v2/pokemon?limit=1025");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(content);

            List<string> pokemonNames = new List<string>();
            foreach (var result in data.results)
            {
                pokemonNames.Add((string)result.name);
            }

            _cache.Set(cacheKey, pokemonNames, TimeSpan.FromDays(1));
            return pokemonNames;
        }
    }
}