using EcoCityWaste.Models;

namespace EcoCityWaste.Helpers
{
    /// <summary>
    /// Classe de extensão para formatar os estados das ocorrências/incidentes.
    /// Como os estados são guardados na base de dados em formatos sem espaços (ex: "EmAnalise"),
    /// esta classe ajuda a convertê-los para um formato legível no portal.
    /// </summary>
    public static class IncidentStatusExtensions
    {
        /// <summary>
        /// Converte uma string de estado técnico para uma string de exibição formatada.
        /// Útil para garantir que o cidadão ou o administrador vêem o estado com a gramática correta.
        /// </summary>
        /// <param name="status">O estado vindo da base de dados.</param>
        /// <returns>O nome formatado (ex: de "EmResolucao" para "Em Resolução").</returns>
        public static string ToDisplayName(this OccurrenceStatus status)
        {
            return status switch
            {
                OccurrenceStatus.Pendente => "Pendente",
                OccurrenceStatus.EmAnalise => "Em Análise",
                OccurrenceStatus.EmResolucao => "Em Resolução",
                OccurrenceStatus.Resolvido => "Resolvido",
                OccurrenceStatus.Rejeitado => "Rejeitado",
                _ => status.ToString()
            };
        }
    }
}