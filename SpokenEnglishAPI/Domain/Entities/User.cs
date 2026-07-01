namespace SpokenEnglishAPI.Domain.Entities
{
    //public class User
    //{
    //    public int ID { get; set; }
    //    public Guid UserGuid { get; set; }
    //    public string Email { get; set; }
    //    public string PasswordHash { get; set; }
    //    public string ApiKey { get; set; }
    //    public bool IsActive { get; set; }
    //}
    public class User
    {
        public int ID { get; set; }
        public Guid UserGuid { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string PasswordHash { get; set; }
        public string ApiKey { get; set; }
        public string Role { get; set; }
        public string Level { get; set; }
        public bool IsActive { get; set; }
    }

}


