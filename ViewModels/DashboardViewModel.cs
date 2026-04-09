namespace EcoCityWaste.ViewModels
{
    /// <summary>
    /// ViewModel centralizador para o painel de controlo da aplicação.
    /// Reúne estatísticas consolidadas e indicadores de performance para visualização rápida pela administração.
    /// </summary>
    public class DashboardViewModel
    {
        // --- Secção de Ocorrências ---

        /// <summary> Número total de ocorrências registadas no sistema. </summary>
        public int TotalOcorrencias { get; set; }

        /// <summary> Quantidade de ocorrências que ainda aguardam triagem inicial. </summary>
        public int Pendente { get; set; }

        /// <summary> Quantidade de ocorrências que já foram atribuídas ou estão sob verificação técnica. </summary>
        public int EmAnalise { get; set; }

        /// <summary> Quantidade de ocorrências com intervenção no terreno em curso. </summary>
        public int EmResolucao { get; set; }

        /// <summary> Número de anomalias que foram dadas como solucionadas. </summary>
        public int Resolvido { get; set; }

        /// <summary> Número de reportes que foram arquivados ou considerados inválidos. </summary>
        public int Rejeitado { get; set; }


        // --- Secção de Contentores ---

        /// <summary> Inventário total de contentores registados no município. </summary>
        public int TotalContentores { get; set; }

        /// <summary> Número de contentores que atingiram o limite de enchimento (>= 90%) e requerem recolha urgente. </summary>
        public int ContentoresCriticos { get; set; }


        // --- Indicadores Temporais ---

        /// <summary> Volume de novos reportes submetidos nas últimas 24 horas. </summary>
        public int OcorrenciasHoje { get; set; }

        /// <summary> Volume de reportes submetidos nos últimos 7 dias. </summary>
        public int OcorrenciasSemana { get; set; }


        // --- Indicadores Ambientais e de Eficiência ---

        /// <summary> 
        /// Média global de enchimento de todos os contentores ativos. 
        /// Usado para medir a pressão sobre o sistema de gestão de resíduos.
        /// </summary>
        public double NivelMedioEnchimento { get; set; }

        /// <summary> Relação percentual entre contentores críticos e o total instalado. </summary>
        public double PercentagemCriticos { get; set; }

        /// <summary> 
        /// Dicionário que mapeia o nível médio de enchimento para cada categoria de resíduo (ex: Vidro: 45%, Papel: 70%). 
        /// Permite identificar quais os fluxos de lixo com maior rotatividade.
        /// </summary>
        public Dictionary<string, double> NivelMedioPorTipo { get; set; } = new();
    }
}