using System.Threading.Tasks;

namespace WebApp.Core.Mailing
{
    public interface IEmailService
    {
        Task<SendEmailResult> SendEmailAsync(EmailRequest request);
    }
}