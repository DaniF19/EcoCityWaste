namespace EcoCityWaste.ViewModels
{
    public class DashboardViewModel
    {
        // Ocorrências
        public int TotalOcorrencias { get; set; }
        public int Pendente { get; set; }
        public int EmAnalise { get; set; }
        public int EmResolucao { get; set; }
        public int Resolvido { get; set; }
        public int Rejeitado { get; set; }

        // Contentores
        public int TotalContentores { get; set; }
        public int ContentoresCriticos { get; set; }

        // Indicadores temporais
        public int OcorrenciasHoje { get; set; }
        public int OcorrenciasSemana { get; set; }

        // Indicadores Ambientais
        public double NivelMedioEnchimento { get; set; }
        public double PercentagemCriticos { get; set; }
        public Dictionary<string, double> NivelMedioPorTipo { get; set; } = new();
    }
}
