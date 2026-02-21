namespace EcoCityWaste.Models
{
    public class Container
    {
        public int Id { get; set; }

        // Código único do contentor (ex.: CNT-00123)
        public string Code { get; set; }

        // Localização textual (ex.: "Rua Afonso de Albuquerque, Setúbal")
        public string Location { get; set; }

        // Coordenadas geográficas
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Tipo de contentor (ex.: Vidro, Papel)
        public string Type { get; set; }

        // Estado atual (ex.: Cheio, Vazio, Avariado)
        public string Status { get; set; }

        // Percentagem de enchimento (0–100)
        public int FillLevel { get; set; }

        // Data de instalação
        public DateTime InstallationDate { get; set; }

        // Última atualização (estado, nível, etc.)
        public DateTime LastUpdated { get; set; }

        // Se o contentor está ativo no sistema
        public bool IsActive { get; set; }
    }

}
