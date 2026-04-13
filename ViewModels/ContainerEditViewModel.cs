using EcoCityWaste.Models;

namespace EcoCityWaste.ViewModels
{
    /// <summary>
    /// ViewModel utilizado para a edição de contentores existentes.
    /// Define os campos que podem ser modificados por um administrador, isolando a lógica de edição da entidade de base de dados.
    /// </summary>
    public class ContainerEditViewModel
    {
        /// <summary>
        /// Identificador único do contentor a ser editado.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Descrição textual da localização ou morada do contentor.
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de resíduos que o contentor aceita (ex: Vidro, Papel, Plástico, Indiferenciado).
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Estado físico atual do contentor, baseado no enumerador definido no modelo principal.
        /// Permite alterar entre estados como "Bom", "Avariado" ou "Manutenção".
        /// </summary>
        public Container.ContainerStatus Status { get; set; }
    }
}