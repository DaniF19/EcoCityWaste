namespace EcoCityWaste.Helpers
{
    public static class IncidentStatusExtensions
    {
        public static string ToDisplayName(this string status)
        {
            return status switch
            {
                "Pendente" => "Pendente",
                "EmAnalise" => "Em Análise",
                "EmResolucao" => "Em Resolução",
                "Resolvido" => "Resolvido",
                "Rejeitado" => "Rejeitado",
                _ => status
            };
        }
    }
}
