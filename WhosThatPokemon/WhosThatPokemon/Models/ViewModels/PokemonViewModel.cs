namespace WhosThatPokemon.Models.ViewModels
{
    public class PokemonViewModel
    {
        public string? Name { get; set; }
        public string ImageUrl { get; set; }
        public string? Type1 { get; set; }
        public string? Type2 { get; set; } // Puede ser null
        public int EvolutionStage { get; set; }
        public string? Color { get; set; }
        public int Generation { get; set; }
        public double Height { get; set; } // En metros
        public double Weight { get; set; } // En kilogramos

        public string Description { get; set; }
    }

    public class GuessResultViewModel : PokemonViewModel
    {
        // Propiedades para indicar el color de cada celda
        public string? Type1Color { get; set; } // "green", "red", "yellow"
        public string? Type2Color { get; set; }
        public string? EvolutionStageColor { get; set; }
        public string? ColorColor { get; set; }
        public string? GenerationColor { get; set; }
        public string? HeightColor { get; set; } // Puede ser "green", "red-up", "red-down"
        public string? WeightColor { get; set; } // Puede ser "green", "red-up", "red-down"
    }

    public class GameSessionViewModel
    {
        public PokemonViewModel? MysteryPokemon { get; set; } // El Pokémon a adivinar
        public List<GuessResultViewModel>? Guesses { get; set; } // Lista de intentos del usuario
        public bool HasGuessedCorrectly { get; set; } = false;
        public string? CurrentGuessName { get; set; } // Para el input del usuario
    }

    public class ClassicGameFilterViewModel
    {
        public List<int> SelectedGenerations { get; set; } = new();
    }

}