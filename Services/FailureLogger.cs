using EcoCityWaste.Data;
using EcoCityWaste.Models;

namespace EcoCityWaste.Services
{
    /// <summary>
    /// Serviço responsável por registar falhas críticas e exceções do sistema.
    /// Permite aos administradores diagnosticar erros que ocorreram em produção sem expor detalhes sensíveis ao utilizador final.
    /// </summary>
    public class FailureLogger
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Injeta o contexto da base de dados para permitir a persistência dos logs de erro.
        /// </summary>
        public FailureLogger(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Grava de forma assíncrona os detalhes de uma exceção na tabela de FailureLogs.
        /// Captura a mensagem de erro, o stack trace, o local exato da falha e o utilizador afetado.
        /// </summary>
        /// <param name="ex">A exceção capturada pelo bloco try-catch.</param>
        /// <param name="controller">O nome do controlador onde o erro ocorreu.</param>
        /// <param name="action">O nome da ação/método que despoletou a falha.</param>
        /// <param name="user">O nome do utilizador que estava autenticado no momento (opcional).</param>
        /// <returns>Uma Task que representa a operação de escrita do log.</returns>
        public async Task LogAsync(Exception ex, string controller, string action, string? user)
        {
            try
            {
                var log = new FailureLog
                {
                    Message = ex.Message,
                    // Guarda o stack trace completo para facilitar o "debug" pelos programadores
                    StackTrace = ex.ToString(),
                    Controller = controller,
                    Action = action,
                    UserName = user
                };

                _context.FailureLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Garante que, se a base de dados estiver em baixo 
                // e o logger falhar, o sistema não entra num ciclo infinito de erros.
            }
        }
    }
}