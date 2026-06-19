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
        con.Execute(
            @"INSERT INTO apiusage (userguid, endpoint, requestcount, usagedate)
              VALUES (@UserGuid, @Endpoint, 1, CURRENT_DATE)
              ON CONFLICT (userguid, endpoint, usagedate)
              DO UPDATE SET requestcount = apiusage.requestcount + 1",
            new { UserGuid = userGuid, Endpoint = endpoint });
    }
}
