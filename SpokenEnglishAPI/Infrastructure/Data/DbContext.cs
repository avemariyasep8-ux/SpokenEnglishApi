using System.Data;
using Npgsql;

namespace SpokenEnglishAPI.Infrastructure.Data
{
    public class DbContext
    {
        private readonly IConfiguration _config;

        public DbContext(IConfiguration config)
        {
            _config = config;
        }

        public IDbConnection CreateConnection()
            => new NpgsqlConnection(_config.GetConnectionString("SE_DB"));
    }
}
