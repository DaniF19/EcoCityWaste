namespace EcoCityWaste.Dtos
{
    /// <summary>
    /// Objeto de Transferência de Dados usado para simplificar a atualização do estado de um contentor.
    /// Serve para enviar apenas a informação essencial, otimizando o tráfego de dados.
    /// </summary>
    public class UpdateContainerStatusDto
    {
        /// <summary>
        /// Identificador único do contentor que se pretende atualizar.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// O novo estado físico a atribuir (ex: "Bom", "Cheio", "Avariado"). 
        /// É enviado como string para ser posteriormente validado e convertido para o Enum correspondente.
        /// </summary>
        public string Status { get; set; }
    }
}