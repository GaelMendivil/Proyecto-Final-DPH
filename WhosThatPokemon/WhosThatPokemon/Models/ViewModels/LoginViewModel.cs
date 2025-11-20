using System.ComponentModel.DataAnnotations;

namespace WhosThatPokemon.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El correo o usuario es obligatorio")]
        public string EmailOrUser { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Recordarme")]
        public bool RememberMe { get; set; }
    }
}