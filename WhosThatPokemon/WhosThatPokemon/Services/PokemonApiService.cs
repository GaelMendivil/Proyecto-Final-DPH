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
            pokemonName = pokemonName.ToLower().Trim().Replace(" ", "-");


            // Intentar obtener POKEMON

            var pokemonResponse = await _httpClient.GetAsync($"https://pokeapi.co/api/v2/pokemon/{pokemonName}");

            string basePokemonName = pokemonName;

            if (!pokemonResponse.IsSuccessStatusCode)
            {

                // Intentar obtenerlo como FORM
    
                var formResponse = await _httpClient.GetAsync($"https://pokeapi.co/api/v2/pokemon-form/{pokemonName}");

                if (!formResponse.IsSuccessStatusCode)
                {
                    return null; // No existe
                }

                var formJson = await formResponse.Content.ReadAsStringAsync();
                dynamic formData = JsonConvert.DeserializeObject(formJson);

                // Obtener Pokémon base
                basePokemonName = formData.pokemon.name;

                // volver a hacer request del pokemon base
                pokemonResponse = await _httpClient.GetAsync($"https://pokeapi.co/api/v2/pokemon/{basePokemonName}");
            }

            if (!pokemonResponse.IsSuccessStatusCode)
                return null;

            // Pokemon data JSON
            var pokemonJson = await pokemonResponse.Content.ReadAsStringAsync();
            dynamic pokemonData = JsonConvert.DeserializeObject(pokemonJson);

            // Obtener SPECIES del Pokémon base
            var speciesResponse = await _httpClient.GetAsync($"https://pokeapi.co/api/v2/pokemon-species/{basePokemonName}");

            if (!speciesResponse.IsSuccessStatusCode)
                return null;

            var speciesJson = await speciesResponse.Content.ReadAsStringAsync();
            dynamic speciesData = JsonConvert.DeserializeObject(speciesJson);

            // Descripción 
            string description = null;
            foreach (var entry in speciesData.flavor_text_entries)
            {
                if (entry.language.name == "es")
                {
                    description = ((string)entry.flavor_text).Replace("\n", " ").Replace("\f", " ");
                    break;
                }
            }
            if (description == null)
            {
                foreach (var entry in speciesData.flavor_text_entries)
                {
                    if (entry.language.name == "en")
                    {
                        description = ((string)entry.flavor_text).Replace("\n", " ").Replace("\f", " ");
                        break;
                    }
                }
            }

            return new PokemonViewModel
            {
                Name = pokemonName, // nombre ingresado por el usuario 
                ImageUrl = pokemonData.sprites.front_default,
                Type1 = pokemonData.types[0].type.name,
                Type2 = pokemonData.types.Count > 1 ? pokemonData.types[1].type.name : null,
                Height = (double)pokemonData.height / 10,
                Weight = (double)pokemonData.weight / 10,
                Generation = GetGenerationFromUrl((string)speciesData.generation.url),
                Color = (string)speciesData.color.name,
                EvolutionStage = await GetEvolutionStage((string)basePokemonName, (string)speciesData.evolution_chain.url),
                Description = description
            };
        }

        private int GetGenerationFromUrl(string url)
        {
            var parts = url.Split('/');
            return int.Parse(parts[parts.Length - 2]);
        }

        // Obtener la etapa evolutiva del Pokémon
       private async Task<int> GetEvolutionStage(string pokemonName, string evolutionChainUrl)
        {
            string cacheKey = $"evo_{evolutionChainUrl}";

            // Usamos 'object' para guardar los datos en el caché
            if (!_cache.TryGetValue(cacheKey, out object evolutionDataObj))
            {
                // --- CORRECCIÓN AQUÍ ---
                try
                {
                    var response = await _httpClient.GetAsync(evolutionChainUrl);
                    if (!response.IsSuccessStatusCode)
                        return 1;

                    dynamic data = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                    return FindStageInChain(data.chain, pokemonName, 1);
                }
                catch // Ahora este 'catch' es válido porque sigue al 'try'
                {
                    return 1;
                }
                // --- FIN DE LA CORRECCIÓN ---
            }

            // Convertir el 'object' a 'dynamic' para poder leerlo
            dynamic evolutionData = (dynamic)evolutionDataObj;

            int stage = FindStageInChain(evolutionData.chain, pokemonName, 1);
            return stage == 0 ? 1 : stage;
        }

        private int FindStageInChain(dynamic chain, string name, int stage)
        {
            if (chain.species.name == name)
                return stage;

            foreach (var evo in chain.evolves_to)
            {
                int found = FindStageInChain(evo, name, stage + 1);
                if (found > 0)
                    return found;
            }

            return 0;
        }

        public async Task<List<string>> GetAllPokemonNamesAsync()
        {
            var response = await _httpClient.GetAsync("https://pokeapi.co/api/v2/pokemon?limit=1025");
            var json = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(json);

            var list = new List<string>();
            foreach (var item in data.results)
                list.Add((string)item.name);

            return list;
        }
    }
}