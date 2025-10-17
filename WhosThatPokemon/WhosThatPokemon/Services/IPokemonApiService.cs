using WhosThatPokemon.Models.ViewModels;
using System.Threading.Tasks;

namespace WhosThatPokemon.Services
{
    public interface IPokemonApiService
    {
        Task<PokemonViewModel> GetPokemonDataAsync(string pokemonName);
        Task<List<string>> GetAllPokemonNamesAsync(); // Para obtener una lista de todos los Pokémon
    }
}