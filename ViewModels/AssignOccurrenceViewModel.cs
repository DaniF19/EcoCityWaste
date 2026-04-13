using EcoCityWaste.Models;

namespace EcoCityWaste.ViewModels
{
    /// <summary>
    /// ModelView para a funcionalidade de atribuição de ocorrências a funcionários.
    /// Transporta a lista de anomalias pendentes, a lista de colaboradores disponíveis 
    /// e métricas de carga de trabalho para apoiar a decisão do administrador.
    /// </summary>
    public class AssignOccurrenceViewModel
    {
        /// <summary>
        /// ID da ocorrência selecionada no formulário para ser atribuída.
        /// </summary>
        public int SelectedOccurrenceId { get; set; }

        /// <summary>
        /// ID do funcionário escolhido para resolver a ocorrência.
        /// </summary>
        public int SelectedEmployeeId { get; set; }

        /// <summary>
        /// Lista de ocorrências que ainda não foram atribuídas a nenhum funcionário.
        /// </summary>
        public List<Occurrence>? Occurrences { get; set; }

        /// <summary>
        /// Lista de utilizadores com a Role "Funcionario" que podem receber tarefas.
        /// </summary>
        public List<User>? Employees { get; set; }

        /// <summary>
        /// Dicionário que mapeia o ID do Funcionário ao número de tarefas que ele já tem em mãos.
        /// Ajuda o administrador a distribuir o trabalho de forma equilibrada.
        /// </summary>
        public Dictionary<int, int>? EmployeeOccurrenceCounts { get; set; }
    }
}