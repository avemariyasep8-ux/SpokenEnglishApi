using System.Net;
using System.Net.Mail;

namespace SpokenEnglishAPI.Application.Services
{
    /// <summary>Sends exception alert emails to the admin address.</summary>
    public interface IEmailAlertService
    {
        Task SendExceptionAlertAsync(string subject, string body);
    }

    public class EmailAlertService : IEmailAlertService
    {
        private readonly IConfiguration _cfg;
        private readonly ILogger<EmailAlertService> _log;

        public EmailAlertService(IConfiguration cfg, ILogger<EmailAlertService> log)
        {
            _cfg = cfg;
            _log = log;
        }

        public async Task SendExceptionAlertAsync(string subject, string body)
        {
            try
            {
                var host     = _cfg["Email:SmtpHost"]     ?? "smtp.gmail.com";
                var port     = int.Parse(_cfg["Email:SmtpPort"] ?? "587");
                var user     = _cfg["Email:Username"]     ?? "";
                var pass     = _cfg["Email:Password"]     ?? "";
                var from     = _cfg["Email:From"]         ?? user;
                var alertTo  = _cfg["Email:AlertTo"]      ?? "avemariya.sep8@gmail.com";

                if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
                {
                    _log.LogWarning("Email credentials not configured — skipping alert email.");
                    return;
                }

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl   = true,
                    Credentials = new NetworkCredential(user, pass),
                };

                var msg = new MailMessage(from, alertTo)
                {
                    Subject    = $"[SpokenEnglish API] {subject}",
                    Body       = body,
                    IsBodyHtml = false,
                };

                await client.SendMailAsync(msg);
                _log.LogInformation("Exception alert email sent to {To}", alertTo);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to send exception alert email");
            }
        }
    }
}
