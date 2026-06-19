using Dapper;
using SpokenEnglishAPI.Domain.Entities;
using SpokenEnglishAPI.Infrastructure.Data;
using System.Data;

namespace SpokenEnglishAPI.Infrastructure.Repositories;

public class UserRepository
{
    private readonly DbContext _context;

    public UserRepository(DbContext context) => _context = context;

    public User GetByEmail(string emailOrMobile)
    {
        using var con = _context.CreateConnection();
        return con.QueryFirstOrDefault<User>(
            "SELECT * FROM sp_user_login(@p_logininput)",
            new { p_logininput = emailOrMobile });
    }

    public bool IsValidApiKey(string apiKey)
    {
        using var con = _context.CreateConnection();
        return con.ExecuteScalar<Guid?>(
            "SELECT sp_validate_apikey(@p_apikey)",
            new { p_apikey = apiKey }) != null;
    }

    public void CreateUser(string email, string passwordHash, string apiKey)
    {
        using var con = _context.CreateConnection();
        con.Execute(
            "SELECT sp_user_create(@p_email, @p_passwordhash, @p_apikey)",
            new { p_email = email, p_passwordhash = passwordHash, p_apikey = apiKey });
    }

    public void SaveRefreshToken(int userId, string token, DateTime expiresAt)
    {
        using var con = _context.CreateConnection();
        con.Execute(
            "SELECT sp_save_refreshtoken(@p_userid, @p_token, @p_expiresat)",
            new { p_userid = userId, p_token = token, p_expiresat = expiresAt });
    }

    public User ValidateRefreshToken(string token)
    {
        using var con = _context.CreateConnection();
        return con.QueryFirstOrDefault<User>(
            "SELECT * FROM sp_validate_refreshtoken(@p_token)",
            new { p_token = token });
    }

    public void UpdatePassword(int userId, string passwordHash)
    {
        using var con = _context.CreateConnection();
        con.Execute(
            "UPDATE users SET passwordhash = @PasswordHash, modifydate = NOW() WHERE id = @UserId",
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
