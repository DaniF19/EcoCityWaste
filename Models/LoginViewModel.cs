using System.ComponentModel.DataAnnotations;

namespace EcoCityWaste.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "A palavra-passe é obrigatória")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

    }
}
