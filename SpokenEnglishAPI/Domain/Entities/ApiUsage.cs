namespace SpokenEnglishAPI.Domain.Entities
{
    public class ApiUsage
    {
        public Guid UserGuid { get; set; }
        public string Endpoint { get; set; }
        public int RequestCount { get; set; }
    }
}
