namespace EcoCityWaste.ViewModels
{
    /// <summary>
    /// ViewModel utilizado para a criação manual de novos utilizadores pelo administrador.
    /// Este modelo facilita a captura de dados iniciais de conta antes do processo de hashing da password.
    /// </summary>
    public class CreateUserViewModel
    {
        /// <summary>
        /// Nome de utilizador único escolhido para a conta.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Endereço de correio eletrónico associado ao novo utilizador.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Password em texto limpo introduzida no formulário. 
        /// Esta propriedade nunca é gravada diretamente na base de dados; serve apenas para o processo de registo.
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }
}