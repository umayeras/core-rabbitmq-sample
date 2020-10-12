using System;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WebApp.Core.Constants;
using WebApp.Core.Entities;
using WebApp.Core.Mailing;

namespace WebApp.Consumer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : ControllerBase
    {
        private IConfiguration Configuration { get; }
        private readonly IEmailService emailService;

        public HomeController(IConfiguration configuration, IEmailService emailService)
        {
            Configuration = configuration;
            this.emailService = emailService;
        }

        public IActionResult Index()
        {
            var rabbitSettings = Configuration.GetSection(SectionNames.RabbitMqSettings).Get<RabbitMqSettings>();
            var factory = new ConnectionFactory {HostName = rabbitSettings.Uri};
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(QueueNames.UserEmail, false, false, false, null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += Consumer_Received;
                channel.BasicConsume(QueueNames.UserEmail, true, consumer);
            }

            return Ok(Messages.GeneralSuccess);
        }

        private async void Consumer_Received(object sender, BasicDeliverEventArgs ea)
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var user = JsonConvert.DeserializeObject<User>(message);

                await SendEmail(user.Email);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.InnerException?.Message);
            }
        }

        private async Task SendEmail(string email)
        {
            var emailSettings = Configuration.GetSection(SectionNames.EmailSettings).Get<EmailSettings>();

            var emailRequest = new EmailRequest
            {
                From = emailSettings.EmailAddress,
                To = email,
                Subject = "Test E-mail",
                Body = "Test E-mail"
            };

            await emailService.SendEmailAsync(emailRequest);
        }
    }
}