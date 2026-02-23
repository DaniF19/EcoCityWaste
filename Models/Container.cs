namespace EcoCityWaste.Models
{
    public class Container
    {
        public int Id { get; set; }
        public string Location { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int FillLevel { get; set; }
        public DateTime? InstallationDate { get; set; }
        public DateTime? LastUpdated { get; set; }
        public bool IsActive { get; set; }
    }
}
