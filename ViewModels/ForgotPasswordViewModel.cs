using System.ComponentModel.DataAnnotations;

namespace EcoCityWaste.ViewModels
{
    /// <summary>
    /// ViewModel utilizado para o processo de recuperação de acesso à conta.
    /// Captura o endereço de e-mail do utilizador para o envio das instruções de redefinição de password.
    /// </summary>
    public class ForgotPasswordViewModel
    {
        /// <summary>
        /// Endereço de correio eletrónico associado à conta do utilizador.
        /// Inclui validação obrigatória e verificação de formato de e-mail para evitar submissões inválidas.
        /// </summary>
        [Required(ErrorMessage = "O preenchimento do e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Introduza um endereço de e-mail válido.")]
        public string Email { get; set; } = string.Empty;
    }
}