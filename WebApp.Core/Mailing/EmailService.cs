using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using WebApp.Core.Constants;

namespace WebApp.Core.Mailing
{
        public class EmailService : IEmailService
    {
        private IConfiguration Configuration { get; }
        private readonly ILogger<EmailService> logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            Configuration = configuration;
            this.logger = logger;
        }

        public async Task<SendEmailResult> SendEmailAsync(EmailRequest request)
        {
            var settings = Configuration.GetSection(SectionNames.EmailSettings).Get<EmailSettings>();
            var email = new MimeMessage
            {
                Sender = MailboxAddress.Parse(settings?.EmailAddress),
                To = { MailboxAddress.Parse(request.To) },
                Subject = request.Subject,
            };

            var builder = new BodyBuilder { HtmlBody = request.Body };
            email.Body = builder.ToMessageBody();
            using var smtp = new SmtpClient { ServerCertificateValidationCallback = (s, c, h, e) => true };

            if (settings == null)
            {
                logger.LogError($"{nameof(SendEmailAsync)} - {Messages.GeneralError}");
                return SendEmailResult.Error(Messages.GeneralError);
            }

            try
            {
                await smtp.ConnectAsync(settings.Host, settings.Port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(settings.EmailAddress, settings.Password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
                
                logger.LogInformation($"{nameof(SendEmailAsync)} - {Messages.EmailSent}");
                return SendEmailResult.Success();
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage = ex.InnerException.Message;
                }
                
                logger.LogError($"{nameof(SendEmailAsync)} - {errorMessage}");
                return SendEmailResult.Error(ex.Message);
            }
        }
    }
}