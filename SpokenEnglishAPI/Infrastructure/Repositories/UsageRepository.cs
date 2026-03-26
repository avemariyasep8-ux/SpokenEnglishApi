using Dapper;
using SpokenEnglishAPI.Infrastructure.Data;

namespace SpokenEnglishAPI.Infrastructure.Repositories;

public class UsageRepository
{
    private readonly DbContext _context;

    public UsageRepository(DbContext context)
    {
        _context = context;
    }

    public void TrackUsage(Guid userGuid, string endpoint)
    {
        using var con = _context.CreateConnection();
        con.Execute("sp_Insert_ApiUsage",
            new { UserGuid = userGuid, Endpoint = endpoint },
            commandType: System.Data.CommandType.StoredProcedure);
    }
}
