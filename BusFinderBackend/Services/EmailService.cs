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

        public async Task SendPasswordResetEmailAsync(string recipientEmail, string oobCode, string recipientName)
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
                Subject = "Action Required: Reset Bus Finder SL Password",
                Body = $"<p style=\"margin-bottom: 15px;\">Dear {recipientName},</p>" +
                    $"<p style=\"margin-bottom: 15px;\">We received a request to reset your password for your Bus Finder SL account. Please use the following one-time code to complete your password reset:</p>" +
                    $"<div style=\"text-align:center; margin-bottom: 20px;\">" +
                    $"    <strong style=\"font-size: 24px; color: #0056b3; background-color: #f0f8ff; padding: 10px 20px; border-radius: 5px; letter-spacing: 2px; display: inline-block;\">{oobCode}</strong>" +
                    $"</div>" +
                    $"<p style=\"margin-bottom: 15px;\">This code is valid for a limited time. If you did not request a password reset, please disregard this email.</p>" +
                    $"<p style=\"margin-bottom: 5px;\">Thank you for using Bus Finder SL.</p>" +
                    $"<p style=\"margin-bottom: 0;\">Best regards,</p>" +
                    $"<p style=\"margin-bottom: 0;\">The Bus Finder SL Team</p>",
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

        public async Task SendCredentialsEmailAsync(string recipientEmail, string password, string userType, string recipientName)
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
                Subject = "Bus Finder SL: Account Credentials",
                Body = $"<p style=\"margin-bottom: 15px;\">Dear {recipientName},</p>" +
                    $"<p style=\"margin-bottom: 15px;\">Welcome to Bus Finder SL! Your {userType} account has been successfully created.</p>" +
                    $"<p style=\"margin-bottom: 15px;\">You can now log in using the following credentials:</p>" +
                    $"<ul style=\"margin-top: 0; margin-bottom: 15px; padding-left: 20px;\">" +
                    $"    <li style=\"margin-bottom: 5px;\"><strong>Email Address:</strong> {recipientEmail}</li>" +
                    $"    <li style=\"margin-bottom: 5px;\"><strong>Temporary Password:</strong> {password}</li>" +
                    $"</ul>" +
                    $"<p style=\"margin-bottom: 15px;\">For security purposes, we recommend that you change your password immediately after your first login.</p>" +
                    $"<p style=\"margin-bottom: 5px;\">We're excited to have you on board!</p>" +
                    $"<p style=\"margin-bottom: 0;\">Best regards,</p>" +
                    $"<p style=\"margin-bottom: 0;\">The Bus Finder SL Team</p>",
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

        public async Task SendAccountDeletedEmailAsync(string recipientEmail, string recipientName)
        {
            _logger.LogInformation("Preparing to send account deletion email to {RecipientEmail}", recipientEmail);
            if (string.IsNullOrEmpty(recipientEmail))
            {
                _logger.LogWarning("Recipient email is empty. Email not sent.");
                return; // Exit if the recipient email is empty
            }

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_email),
                Subject = "Bus Finder SL Account Deletion Confirmation",
                Body = $"<p style=\"margin-bottom: 15px;\">Dear {recipientName},</p>" +
                    $"<p style=\"margin-bottom: 15px;\">This email confirms that your Bus Finder SL account, associated with {recipientEmail}, has been successfully deleted as per your request or our policy.</p>" +
                    $"<p style=\"margin-bottom: 15px;\">If you believe this was done in error or did not initiate this action, please contact our support team immediately for assistance.</p>" +
                    $"<p style=\"margin-bottom: 5px;\">Thank you for being a part of the Bus Finder SL community.</p>" +
                    $"<p style=\"margin-bottom: 0;\">Sincerely,</p>" +
                    $"<p style=\"margin-bottom: 0;\">The Bus Finder SL Team</p>",
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
                    _logger.LogInformation("Account deletion email sent successfully to {RecipientEmail}", recipientEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send account deletion email to {RecipientEmail}", recipientEmail);
                }
            }
        }
    }
}
