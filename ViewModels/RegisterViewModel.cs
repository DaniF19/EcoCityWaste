using System.ComponentModel.DataAnnotations;

namespace EcoCityWaste.ViewModels
{
    /// <summary>
    /// ViewModel responsável pelo processo de registo de novos cidadãos na plataforma.
    /// Contém regras de validação rigorosas para garantir a segurança das contas e a integridade dos dados.
    /// </summary>
    public class RegisterViewModel
    {
        /// <summary>
        /// Nome de utilizador para identificação no sistema.
        /// </summary>
        [Required(ErrorMessage = "O nome de utilizador é obrigatório.")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Endereço de e-mail que servirá para login e comunicações do sistema.
        /// Validado para garantir um formato de correio eletrónico legítimo.
        /// </summary>
        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Introduza um e-mail válido.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Palavra-passe definida pelo utilizador.
        /// Impõe um comprimento mínimo de 6 caracteres para reforçar a segurança da conta.
        /// </summary>
        [Required(ErrorMessage = "A password é obrigatória.")]
        [MinLength(6, ErrorMessage = "A password deve ter pelo menos 6 caracteres.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Campo de confirmação da palavra-passe.
        /// Utiliza o atributo Compare para garantir, no lado do cliente e do servidor, que ambos os campos são idênticos.
        /// </summary>
        [Required(ErrorMessage = "A confirmação da password é obrigatória.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "As passwords não coincidem.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        /// <summary>
        /// Nome opcional do cidadão para personalização do perfil e comunicações.
        /// </summary>
        public string? Name { get; set; }
    }
}