using System.ComponentModel.DataAnnotations;

namespace EcoCityWaste.Models
{
    /// <summary>
    /// Representa um contentor de lixo físico espalhado pelo município.
    /// </summary>
    public class Container
    {
        /// <summary>
        /// Define os vários estados em que o equipamento pode estar.
        /// </summary>
        public enum ContainerStatus
        {
            [Display(Name = "Bom")] Good,
            [Display(Name = "Avariado")] Broken,
            [Display(Name = "Manutenção")] Maintenance
        }

        /// <summary>
        /// Chave primária do contentor na base de dados.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Código único gerado para ser fácil de identificar na aplicação (ex: CNT-00123).
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Morada ou descrição do local onde o contentor está instalado.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Latitude para colocar o pino no mapa.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Longitude para colocar o pino no mapa.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Tipo de resíduo que o contentor leva (ex: Vidro, Papel, Indiferenciado).
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Estado físico atual do contentor (se está avariado, bom, em manutenção, etc.).
        /// </summary>
        public ContainerStatus Status { get; set; }

        /// <summary>
        /// Percentagem de ocupação (0 a 100%). Estes dados normalmente vêm dos sensores.
        /// </summary>
        public int FillLevel { get; set; }

        /// <summary>
        /// Data em que o contentor foi registado e instalado na rua.
        /// </summary>
        public DateTime InstallationDate { get; set; }

        /// <summary>
        /// Guarda a data e hora da última vez que o sistema ou o sensor atualizou os dados do contentor.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Se for falso, o contentor não aparece nas listas nem entra nas rotas, mas não perdemos o histórico.
        /// </summary>
        public bool IsActive { get; set; }
    }
}