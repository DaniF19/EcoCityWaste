using System;

namespace EcoCityWaste.Models
{
    /// <summary>
    /// Tabela para registar os erros e exceções não tratadas que acontecem no sistema.
    /// Útil para os developers conseguirem fazer debugging e resolver problemas futuros.
    /// </summary>
    public class FailureLog
    {
        /// <summary>
        /// Chave primária do registo do erro.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Data e hora exatas em que o erro ocorreu.
        /// </summary>
        public DateTime OccurredAt { get; set; } = DateTime.Now;

        /// <summary>
        /// A mensagem principal da exceção ou problema capturado.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// O rasto completo do erro para ajudar a descobrir em que linha o código falhou.
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Nome do controlador onde a exceção foi lançada (ex: OccurrencesController).
        /// </summary>
        public string? Controller { get; set; }

        /// <summary>
        /// Ação ou método específico (endpoint) que estava a ser executado quando deu erro.
        /// </summary>
        public string? Action { get; set; }

        /// <summary>
        /// Username do utilizador que estava autenticado no momento do erro. 
        /// Ajuda a perceber os passos que levaram à falha.
        /// </summary>
        public string? UserName { get; set; }
    }
}