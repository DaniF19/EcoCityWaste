using System.ComponentModel.DataAnnotations;

namespace EcoCityWaste.ViewModels
{
    /// <summary>
    /// ViewModel responsável por capturar e validar as credenciais de acesso durante o processo de autenticação.
    /// Atua como intermediário entre o formulário de login e o serviço de segurança.
    /// </summary>
    public class LoginViewModel
    {
        /// <summary>
        /// Endereço de correio eletrónico utilizado como identificador da conta.
        /// Inclui validação de presença e verificação de formato para garantir a integridade do pedido.
        /// </summary>
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Palavra-passe da conta em texto limpo para verificação contra a Hash guardada.
        /// O atributo DataType garante que, na interface, os caracteres sejam mascarados.
        /// </summary>
        [Required(ErrorMessage = "A palavra-passe é obrigatória")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}