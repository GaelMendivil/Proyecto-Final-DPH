using WhosThatPokemon.Models.ViewModels;
using System.Threading.Tasks;
using System.Collections.Generic;
using WhosThatPokemon.Controllers; 

namespace WhosThatPokemon.Services
{
    public interface IPokemonApiService
    {
        Task<PokemonViewModel> GetPokemonDataAsync(string pokemonName);
        Task<List<string>> GetAllPokemonNamesAsync();
        Task<PokemonSearchViewModel> GetPokemonSearchDataAsync(string pokemonName);
    }
}