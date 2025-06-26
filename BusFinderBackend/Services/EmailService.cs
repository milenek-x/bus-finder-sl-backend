using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace BusFinderBackend.Services
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _port;
        private readonly string _email;
        private readonly string _password;
        private readonly ILogger<EmailService> _logger; // Add logger

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _logger = logger; // Assign logger
            _email = emailSettings.Value.Email;
            _password = emailSettings.Value.Password;
            _smtpServer = emailSettings.Value.SmtpServer;
            _port = emailSettings.Value.Port;
            _logger.LogInformation("Email: {Email}, Password: {Password}", _email, _password);
        }

        public async Task SendPasswordResetEmailAsync(string recipientEmail, string oobCode)
        {
            _logger.LogWarning("Checking if recipient email or OOB code is empty");
            if (string.IsNullOrEmpty(recipientEmail) || string.IsNullOrEmpty(oobCode))
            {
                _logger.LogWarning("Recipient email or OOB code is empty. Email not sent.");
                return; // Exit if email or OOB code is empty
            }

            _logger.LogInformation("Creating mail message");
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_email),
                Subject = "Password Reset Request",
                Body = $"<p>Dear User,</p>" +
                       $"<p>We received a request to reset your password. Please use the following code to reset your password:</p>" +
                       $"<p><strong>{oobCode}</strong></p>" +
                       $"<p>If you did not request a password reset, please ignore this email.</p>" +
                       $"<p>Thank you,</p>" +
                       $"<p>Bus Finder SL Team</p>",
                IsBodyHtml = true,
            };
            mailMessage.To.Add(recipientEmail);

            _logger.LogInformation("Creating SMTP client");
            using (var smtpClient = new SmtpClient(_smtpServer, _port))
            {
                smtpClient.Credentials = new NetworkCredential(_email, _password);
                smtpClient.EnableSsl = true;
                smtpClient.UseDefaultCredentials = false;

                try
                {
                    _logger.LogInformation("Sending password reset email to {RecipientEmail}", recipientEmail);
                    await smtpClient.SendMailAsync(mailMessage);
                    _logger.LogInformation("Password reset email sent successfully to {RecipientEmail}", recipientEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password reset email to {RecipientEmail}", recipientEmail);
                }
            }
        }

        public async Task SendCredentialsEmailAsync(string recipientEmail, string password, string userType)
        {
            _logger.LogInformation("Preparing to send credentials email to {RecipientEmail}", recipientEmail);
            if (string.IsNullOrEmpty(recipientEmail) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("Recipient email or password is empty. Email not sent.");
                return; // Exit if any required information is empty
            }

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_email),
                Subject = "Your Account Credentials",
                Body = $"<p>Dear {userType},</p>" +
                       $"<p>Your account has been created successfully.</p>" +
                       $"<p>You can log in using your email address: <strong>{recipientEmail}</strong></p>" +
                       $"<p>Your Password: <strong>{password}</strong></p>" +
                       $"<p>Please keep your credentials safe.</p>" +
                       $"<p>Thank you,</p>" +
                       $"<p>Bus Finder SL Team</p>",
                IsBodyHtml = true,
            };
            mailMessage.To.Add(recipientEmail);

            using (var smtpClient = new SmtpClient(_smtpServer, _port))
            {
                smtpClient.Credentials = new NetworkCredential(_email, _password);
                smtpClient.EnableSsl = true;
                smtpClient.UseDefaultCredentials = false;

                try
                {
                    await smtpClient.SendMailAsync(mailMessage);
                    _logger.LogInformation("Credentials email sent successfully to {RecipientEmail}", recipientEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send credentials email to {RecipientEmail}", recipientEmail);
                }
            }
        }
    }
}
