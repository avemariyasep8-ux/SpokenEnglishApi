using System.Data;
using Microsoft.Data.SqlClient;

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
            => new SqlConnection(_config.GetConnectionString("SE_DB"));
    }
}
