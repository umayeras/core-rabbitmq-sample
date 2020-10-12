using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using WebApp.Core.Constants;

namespace WebApp.Core.Mailing
{
    public class EmailService : IEmailService
    {
        private IConfiguration Configuration { get; }

        public EmailService(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public async Task<SendEmailResult> SendEmailAsync(EmailRequest request)
        {
            var emailSettings = Configuration.GetSection(SectionNames.EmailSettings).Get<EmailSettings>();

            var email = new MimeMessage
            {
                Sender = MailboxAddress.Parse(emailSettings?.EmailAddress),
                To = {MailboxAddress.Parse(request.To)},
                Subject = request.Subject,
            };

            var builder = new BodyBuilder {HtmlBody = request.Body};
            email.Body = builder.ToMessageBody();
            using var smtp = new SmtpClient {ServerCertificateValidationCallback = (s, c, h, e) => true};

            if (emailSettings == null)
            {
                return SendEmailResult.Error(Messages.GeneralError);
            }

            await smtp.ConnectAsync(emailSettings.Host, emailSettings.Port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(emailSettings.EmailAddress, emailSettings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            return SendEmailResult.Success();
        }
    }
}