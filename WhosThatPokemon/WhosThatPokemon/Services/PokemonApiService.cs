using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WhosThatPokemon.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace WhosThatPokemon.Services
{
    public class PokemonApiService : IPokemonApiService
    {
        private readonly HttpClient _httpClient;

        public PokemonApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PokemonViewModel> GetPokemonDataAsync(string pokemonName)
        {
            var pokeApiUrl = $"https://pokeapi.co/api/v2/pokemon/{pokemonName.ToLowerInvariant()}";
            var speciesApiUrl = $"https://pokeapi.co/api/v2/pokemon-species/{pokemonName.ToLowerInvariant()}";

            var pokemonResponse = await _httpClient.GetAsync(pokeApiUrl);
            var speciesResponse = await _httpClient.GetAsync(speciesApiUrl);

            if (!pokemonResponse.IsSuccessStatusCode || !speciesResponse.IsSuccessStatusCode)
            {
                return null; // Pokémon no encontrado
            }

            var pokemonContent = await pokemonResponse.Content.ReadAsStringAsync();
            var speciesContent = await speciesResponse.Content.ReadAsStringAsync();

            dynamic pokemonData = JsonConvert.DeserializeObject(pokemonContent);
            dynamic speciesData = JsonConvert.DeserializeObject(speciesContent);

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
                EvolutionStage = await GetEvolutionStage((string)pokemonData.name, (string)speciesData.evolution_chain.url)
            };
            
            return pokemonViewModel;
        }

        private int GetGenerationFromUrl(string url)
        {
            var parts = url.Split('/');
            return int.Parse(parts[parts.Length - 2]);
        }

        // Función para obtener la etapa evolutiva
        private async Task<int> GetEvolutionStage(string pokemonName, string evolutionChainUrl)
        {
            try
            {
                var evolutionResponse = await _httpClient.GetAsync(evolutionChainUrl);
                if (!evolutionResponse.IsSuccessStatusCode) return 1; // Devuelve 1 por defecto si la cadena falla

                var evolutionContent = await evolutionResponse.Content.ReadAsStringAsync();
                dynamic evolutionData = JsonConvert.DeserializeObject(evolutionContent);

                // Inicia la búsqueda recursiva en la cadena evolutiva
                int stage = FindStageInChain(evolutionData.chain, pokemonName, 1);

                // Si por alguna razón no se encuentra devuelve 1
                return stage == 0 ? 1 : stage;
            }
            catch
            {
                return 1; // Devuelve 1 por defecto en caso de cualquier error
            }
        }

        private int FindStageInChain(dynamic chainLink, string pokemonName, int currentStage)
        {
            // Comprueba si el Pokémon actual en la cadena es el que buscamos
            if (chainLink.species.name == pokemonName)
            {
                return currentStage;
            }

            // Si hay evoluciones, sigue buscando en ellas
            if (chainLink.evolves_to != null && chainLink.evolves_to.Count > 0)
            {
                foreach (var nextLink in chainLink.evolves_to)
                {
                    // Llama a la función de nuevo para la siguiente etapa, incrementando el nivel
                    int foundStage = FindStageInChain(nextLink, pokemonName, currentStage + 1);
                    if (foundStage > 0)
                    {
                        // Devuelve la etapa encontrada
                        return foundStage;
                    }
                }
            }
            return 0;
        }


        public async Task<List<string>> GetAllPokemonNamesAsync()
        {
            var response = await _httpClient.GetAsync("https://pokeapi.co/api/v2/pokemon?limit=10000");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(content);

            List<string> pokemonNames = new List<string>();
            foreach (var result in data.results)
            {
                pokemonNames.Add((string)result.name);
            }
            return pokemonNames;
        }
    }
}
