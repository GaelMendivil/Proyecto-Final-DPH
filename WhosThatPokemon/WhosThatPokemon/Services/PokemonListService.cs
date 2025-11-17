using System.Text.Json;
using WhosThatPokemon.Models;
using WhosThatPokemon.Models.ViewModels;
using Microsoft.Extensions.Caching.Memory;

namespace WhosThatPokemon.Services
{
    public interface IPokemonListService
    {
        Task<List<PokemonViewModel>> GetAllAsync();
        Task<List<PokemonViewModel>> FilterByGenerationsAsync(List<int> gens);
        Task<PokemonViewModel?> GetRandomAsync(List<int> gens);
        Task<List<PokemonViewModel>> SearchAsync(string term, List<int> gens);
    }

    public class PokemonListService : IPokemonListService
    {
        private readonly IMemoryCache _cache;
        private readonly IPokemonApiService _api;

        private const string CacheKey = "AllPokemonList";

        public PokemonListService(IMemoryCache cache, IPokemonApiService api)
        {
            _cache = cache;
            _api = api;
        }

        // Cargar toda la Pokédex una sola vez
        public async Task<List<PokemonViewModel>> GetAllAsync()
        {
            if (_cache.TryGetValue(CacheKey, out List<PokemonViewModel> cachedList))
                return cachedList;

            var allPokemon = await _api.GetAllPokemonAsync();

            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(12));

            _cache.Set(CacheKey, allPokemon, options);

            return allPokemon;
        }


        // Filtrar por generación
        public async Task<List<PokemonViewModel>> FilterByGenerationsAsync(List<int> gens)
        {
            var all = await GetAllAsync();
            return all.Where(p => gens.Contains(p.Generation)).ToList();
        }


        // Obtener Pokémon aleatorio filtrado
        public async Task<PokemonViewModel?> GetRandomAsync(List<int> gens)
        {
            var list = await FilterByGenerationsAsync(gens);
            if (!list.Any()) return null;

            var rand = new Random();
            return list[rand.Next(list.Count)];
        }


        // Autocomplete

        public async Task<List<PokemonViewModel>> SearchAsync(string term, List<int> gens)
        {
            term = term.ToLower().Trim();
            var list = await FilterByGenerationsAsync(gens);

            return list
                .Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    p.Type1!.ToLower().Contains(term) ||
                    (p.Type2 != null && p.Type2.ToLower().Contains(term)) ||
                    p.Color!.ToLower().Contains(term) ||
                    p.Generation.ToString() == term ||
                    p.EvolutionStage.ToString() == term ||
                    p.Height.ToString() == term ||
                    p.Weight.ToString() == term
                )
                .Take(20)
                .ToList();
        }
    }
}
