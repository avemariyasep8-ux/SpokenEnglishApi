using Dapper;
using SpokenEnglishAPI.Domain.DTOs;
using SpokenEnglishAPI.Domain.Entities;
using SpokenEnglishAPI.Infrastructure.Data;
using System.Data;

namespace SpokenEnglishAPI.Infrastructure.Repositories;

public class UserRepository
{
    private readonly DbContext _context;

    public UserRepository(DbContext context)
    {
        _context = context;
    }

    public User GetByEmail(string emailOrMobile)
    {
        using var con = _context.CreateConnection();
        return con.QueryFirstOrDefault<User>(
            @"SELECT id, userguid, email, mobilenumber, passwordhash, apikey, isactive, role, refreshtoken, refreshtokenexpiry
              FROM users
              WHERE isactive = true AND (email = @Input OR mobilenumber = @Input)
              LIMIT 1",
            new { Input = emailOrMobile });
    }

    public bool IsValidApiKey(string apiKey)
    {
        using var con = _context.CreateConnection();
        return con.ExecuteScalar<Guid?>(
            "SELECT userguid FROM users WHERE apikey = @ApiKey AND isactive = true",
            new { ApiKey = apiKey }) != null;
    }

    public void CreateUser(string email, string passwordHash, string apiKey)
    {
        using var con = _context.CreateConnection();
        var exists = con.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM users WHERE email = @Email",
            new { Email = email });

        if (exists > 0)
            throw new InvalidOperationException("Email already exists");

        con.Execute(
            @"INSERT INTO users (userguid, email, passwordhash, apikey, isactive)
              VALUES (gen_random_uuid(), @Email, @PasswordHash, @ApiKey, true)",
            new { Email = email, PasswordHash = passwordHash, ApiKey = apiKey });
    }

    public void SaveRefreshToken(int userId, string token, DateTime expiresAt)
    {
        using var con = _context.CreateConnection();
        con.Execute(
            @"INSERT INTO refreshtokens (userid, token, expiresat, expirydate, isrevoked, createddate)
              VALUES (@UserId, @Token, @ExpiresAt, @ExpiresAt, false, NOW())
              ON CONFLICT (userid) DO UPDATE
              SET token = EXCLUDED.token, expiresat = EXCLUDED.expiresat,
                  expirydate = EXCLUDED.expirydate, isrevoked = false, createddate = NOW()",
            new { UserId = userId, Token = token, ExpiresAt = expiresAt });
    }

    public User ValidateRefreshToken(string token)
    {
        using var con = _context.CreateConnection();
        return con.QueryFirstOrDefault<User>(
            @"SELECT u.* FROM refreshtokens rt
              JOIN users u ON u.id = rt.userid
              WHERE rt.token = @Token AND rt.isrevoked = false AND rt.expirydate > NOW()",
            new { Token = token });
    }

    public void UpdatePassword(int userId, string passwordHash)
    {
        using var con = _context.CreateConnection();
        con.Execute(
            @"UPDATE users SET passwordhash = @PasswordHash, modifydate = NOW() WHERE id = @UserId",
            new { PasswordHash = passwordHash, UserId = userId });
    }

    public User GetByRefreshToken(string refreshToken)
    {
        using var con = _context.CreateConnection();
        return con.QueryFirstOrDefault<User>(
            "SELECT * FROM users WHERE refreshtoken = @RefreshToken",
            new { RefreshToken = refreshToken });
    }
}
