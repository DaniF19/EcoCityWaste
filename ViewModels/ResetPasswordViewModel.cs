using System.ComponentModel.DataAnnotations;

namespace EcoCityWaste.ViewModels
{
    /// <summary>
    /// ViewModel utilizado para a definição de uma nova palavra-passe após um pedido de recuperação.
    /// Contém o token de segurança necessário para validar a operação e os campos para a nova credencial.
    /// </summary>
    public class ResetPasswordViewModel
    {
        /// <summary>
        /// Token de segurança único gerado pelo sistema e enviado por e-mail.
        /// Serve para autenticar o pedido de redefinição sem exigir a password antiga.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// A nova palavra-passe pretendida pelo utilizador.
        /// Impõe um comprimento mínimo de 6 caracteres para garantir um nível básico de segurança.
        /// </summary>
        [Required(ErrorMessage = "A nova palavra-passe é obrigatória.")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "A palavra-passe deve ter pelo menos 6 caracteres.")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Campo de confirmação para garantir que o utilizador não cometeu erros.
        /// Deve ser idêntico ao campo NewPassword.
        /// </summary>
        [Required(ErrorMessage = "Confirme a sua palavra-passe.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "As palavras-passe não coincidem.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}