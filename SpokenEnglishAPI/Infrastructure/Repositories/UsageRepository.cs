using Dapper;
using SpokenEnglishAPI.Infrastructure.Data;

namespace SpokenEnglishAPI.Infrastructure.Repositories;

public class UsageRepository
{
    private readonly DbContext _context;

    public UsageRepository(DbContext context) => _context = context;

    public void TrackUsage(Guid userGuid, string endpoint)
    {
        using var con = _context.CreateConnection();
        con.Execute(
            "SELECT sp_insert_apiusage(@p_userguid, @p_endpoint)",
            new { p_userguid = userGuid, p_endpoint = endpoint });
    }
}
