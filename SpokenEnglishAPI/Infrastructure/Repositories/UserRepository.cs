using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using SpokenEnglishAPI.Domain.DTOs;
using SpokenEnglishAPI.Domain.Entities;
using SpokenEnglishAPI.Domain.Entities;
using SpokenEnglishAPI.Infrastructure.Data;
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

    public User GetByEmail(string EmailOrMobile)
    {
        using var con = _context.CreateConnection();
        return con.QuerySingleOrDefault<User>(
            "sp_User_Login",
            new { LoginInput = EmailOrMobile
            },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    public bool IsValidApiKey(string apiKey)
    {
        using var con = _context.CreateConnection();
        return con.ExecuteScalar<Guid?>(
            "sp_Validate_ApiKey",
            new { ApiKey = apiKey },
            commandType: System.Data.CommandType.StoredProcedure) != null;
    }

    public void CreateUser(string email, string passwordHash, string apiKey)
    {
        using var con = _context.CreateConnection();
        con.Execute(
            "sp_User_Create",
            new
            {
                Email = email,
                PasswordHash = passwordHash,
                ApiKey = apiKey
            },
            commandType: CommandType.StoredProcedure
        );
    }


    //public User GetByEmailOrMobile(string input)
    //{
    //    return _context.QueryFirstOrDefault<User>(
    //        @"SELECT * FROM Users 
    //          WHERE Email = @Input OR MobileNumber = @Input AND IsActive = 1",
    //        new { Input = input });
    //}

    public void SaveRefreshToken(int userId, string token, DateTime expiresAt)
    {
        using var con = _context.CreateConnection();

        con.Execute(
            @"IF EXISTS (SELECT 1 FROM RefreshTokens WHERE UserId = @UserId)
              UPDATE RefreshTokens SET Token = @Token, ExpiresAt = @ExpiresAt, ExpiryDate = @ExpiresAt, IsRevoked = 0, CreatedDate = GETDATE() WHERE UserId = @UserId
              ELSE
              INSERT INTO RefreshTokens (UserId, Token, ExpiresAt, ExpiryDate, IsRevoked, CreatedDate)
              VALUES (@UserId, @Token, @ExpiresAt, @ExpiresAt, 0, GETDATE())",
            new { UserId = userId, Token = token, ExpiresAt = expiresAt }
        );
    }

    public User ValidateRefreshToken(string token)
    {
        using var con = _context.CreateConnection();
        return con.QueryFirstOrDefault<User>(
            @"SELECT u.* FROM RefreshTokens rt
              JOIN Users u ON u.Id = rt.UserId
              WHERE rt.Token = @Token AND rt.IsRevoked = 0 AND rt.ExpiryDate > GETDATE()",
            new { Token = token });
    }

    public void UpdatePassword(int userId, string passwordHash)
    {
        using var con = _context.CreateConnection();
        con.Execute(
            @"UPDATE Users
          SET PasswordHash = @PasswordHash,
              ModifyDate = GETDATE()
          WHERE UserId = @UserId",
            new { PasswordHash = passwordHash, UserId = userId });
    }



    public User GetByRefreshToken(string refreshToken)
    {
        using var con = _context.CreateConnection();
        return con.QueryFirstOrDefault<User>(
            "SELECT * FROM Users WHERE RefreshToken = @RefreshToken",
            new { RefreshToken = refreshToken });
    }


}
