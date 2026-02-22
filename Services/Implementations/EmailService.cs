using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Grevity.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Grevity.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            // For now, we'll log it if settings are missing to avoid crashing during dev without SMTP
            //var smtpServer = _configuration["EmailSettings:SmtpServer"];
            //var port = _configuration.GetValue<int>("EmailSettings:Port");
            //var senderEmail = _configuration["EmailSettings:SenderEmail"];
            //var senderPassword = _configuration["EmailSettings:SenderPassword"];

            var smtpServer = "smtp.gmail.com";
            var port = 587;
            var senderEmail = "";
            var senderPassword = "";

            if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(senderEmail))
            {
                // Fallback for development/demo: Log to console or debug
                System.Diagnostics.Debug.WriteLine($"[EmailService] To: {toEmail}, Subject: {subject}, Body: {message}");
                return;
            }

            var smtpClient = new SmtpClient(smtpServer)
            {
                Port = port,
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail),
                Subject = subject,
                Body = message,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
