using System;

namespace EcoCityWaste.Models
{
    /// <summary>
    /// Representa um utilizador da plataforma. Centraliza os dados de autenticação 
    /// e define o nível de acesso (Role) que a pessoa tem no sistema.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Chave primária do utilizador.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome escolhido pelo utilizador para apresentação no seu perfil.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Endereço de email associado à conta.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Guarda a palavra-passe de forma segura. É gravada como uma "hash" e nunca em texto limpo.
        /// </summary>
        public string PasswordHash { get; set; }

        /// <summary>
        /// Token gerado temporariamente para operações de segurança, como recuperar a palavra-passe esquecida.
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// Data e hora limite de validade do token (para o link de recuperação expirar).
        /// </summary>
        public DateTime? TokenExpiry { get; set; }

        /// <summary>
        /// Identifica qual a plataforma usada para login externo, caso o utilizador não use as credenciais locais (ex: "Google").
        /// </summary>
        public string? AuthProvider { get; set; }

        /// <summary>
        /// O identificador único devolvido pelo serviço de login externo (ex: o ID da conta do Google).
        /// </summary>
        public string? ProviderUserId { get; set; }

        /// <summary>
        /// Define o nível de permissões do utilizador (ex: Admin, Funcionario, Cidadao). 
        /// O perfil por defeito no registo é o de Cidadão.
        /// </summary>
        public string Role { get; set; } = "Cidadao";
    }
}