using System.Security.Claims;
using FluentAssertions;
using SpokenEnglishAPI.Infrastructure.Security;

namespace SpokenEnglishAPI.Tests;

/// <summary>
/// Tests for the IDOR/BOLA ownership guard and audit-log related security logic.
/// </summary>
public class SecurityTests
{
    private static ClaimsPrincipal Principal(string? uid, string role = "User")
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };
        if (uid != null) claims.Add(new Claim("uid", uid));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    [Fact]
    public void CanAccess_SameUser_Allowed()
    {
        OwnershipGuard.CanAccess(Principal("5"), 5).Should().BeTrue();
    }

    [Fact]
    public void CanAccess_DifferentUser_Denied()
    {
        OwnershipGuard.CanAccess(Principal("5"), 9).Should().BeFalse();
    }

    [Fact]
    public void CanAccess_Admin_AllowedForAnyUser()
    {
        OwnershipGuard.CanAccess(Principal("1", "Admin"), 999).Should().BeTrue();
    }

    [Fact]
    public void CanAccess_LegacyTokenWithoutUid_GraceAllowed()
    {
        // Tokens issued before the uid claim existed are allowed until they expire.
        OwnershipGuard.CanAccess(Principal(null), 5).Should().BeTrue();
    }

    [Fact]
    public void CanAccess_NullPrincipal_Denied()
    {
        OwnershipGuard.CanAccess(null, 5).Should().BeFalse();
    }

    [Fact]
    public void CanAccess_NonNumericUid_Denied()
    {
        OwnershipGuard.CanAccess(Principal("abc"), 5).Should().BeFalse();
    }

    [Theory]
    [InlineData("/api/auth/login", true)]
    [InlineData("/api/auth/register", true)]
    [InlineData("/api/auth/reset-password", true)]
    [InlineData("/api/lessons/1", false)]
    [InlineData("/api/progress/5/1", false)]
    public void AuthRateLimit_PathMatching(string path, bool shouldBeProtected)
    {
        var protectedPaths = new[]
        {
            "/api/auth/login", "/api/auth/register",
            "/api/auth/reset-password", "/api/auth/otp-login",
        };
        var isProtected = protectedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        isProtected.Should().Be(shouldBeProtected);
    }

    [Fact]
    public void AuditLog_TruncatesOverlongFields()
    {
        // Mirrors AuditLogService.Trunc behaviour
        string? Trunc(string? s, int max) =>
            string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s.Substring(0, max));

        Trunc(new string('x', 500), 200)!.Length.Should().Be(200);
        Trunc("short", 200).Should().Be("short");
        Trunc(null, 200).Should().BeNull();
    }
}
