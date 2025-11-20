using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using WhosThatPokemon.Models.ViewModels; 

namespace WhosThatPokemon.Services
{
    public interface IPokemonListService
    {
        bool IsDataLoaded { get; }
        Task InitializeDataAsync();

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
        private List<PokemonViewModel> _localPokemonList = new List<PokemonViewModel>();

        public bool IsDataLoaded { get; private set; } = false;

        public PokemonListService(IMemoryCache cache, IPokemonApiService api)
        {
            _cache = cache;
            _api = api;
        }
        public async Task InitializeDataAsync()
        {
            if (IsDataLoaded && _localPokemonList.Any()) return;

            if (_cache.TryGetValue(CacheKey, out List<PokemonViewModel> cachedList))
            {
                _localPokemonList = cachedList;
                IsDataLoaded = true;
                return;
            }

            var allPokemon = await _api.GetAllPokemonAsync();

            if (allPokemon != null && allPokemon.Any())
            {
                // Guardamos en memoria caché 
                var options = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(12));
                _cache.Set(CacheKey, allPokemon, options);

                _localPokemonList = allPokemon;
                IsDataLoaded = true;
            }
        }

        public async Task<List<PokemonViewModel>> GetAllAsync()
        {
            if (IsDataLoaded && _localPokemonList.Any())
            {
                return _localPokemonList;
            }

            await InitializeDataAsync();
            return _localPokemonList;
        }
        // Filtrado por generaciones 
        public async Task<List<PokemonViewModel>> FilterByGenerationsAsync(List<int> gens)
        {
            var all = await GetAllAsync();
            // Si gens viene vacío o nulo, devolvemos todo, si no, filtramos
            if (gens == null || !gens.Any()) return all;
            
            return all.Where(p => gens.Contains(p.Generation)).ToList();
        }

        public async Task<PokemonViewModel?> GetRandomAsync(List<int> gens)
        {
            var list = await FilterByGenerationsAsync(gens);
            if (!list.Any()) return null;

            var rand = new Random();
            return list[rand.Next(list.Count)];
        }

        public async Task<List<PokemonViewModel>> SearchAsync(string term, List<int> gens)
        {
            if (string.IsNullOrWhiteSpace(term)) return new List<PokemonViewModel>();
            
            term = term.ToLower().Trim();
            var list = await FilterByGenerationsAsync(gens);

            return list
                .Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    (p.Type1 != null && p.Type1.ToLower().Contains(term)) ||
                    (p.Type2 != null && p.Type2.ToLower().Contains(term)) ||
                    (p.Color != null && p.Color.ToLower().Contains(term)) ||
                    p.Generation.ToString() == term ||
                    p.EvolutionStage.ToString() == term
                )
                .Take(20)
                .ToList();
        }
    }
}