using EcoCityWaste.Models;

namespace EcoCityWaste.Helpers
{
    public static class IncidentStatusExtensions
    {
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
