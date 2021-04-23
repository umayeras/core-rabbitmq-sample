using System;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using WebApp.Core.Constants;
using WebApp.Core.Entities;
using WebApp.Core.Mailing;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> logger;
        private readonly RabbitMqSettings rabbitMqSettings;
        private readonly EmailSettings emailSettings;

        public UserController(
            ILogger<UserController> logger,
            IOptions<RabbitMqSettings> rabbitMqSettings,
            IOptions<EmailSettings> emailSettings)
        {
            this.logger = logger;
            this.rabbitMqSettings = rabbitMqSettings.Value;
            this.emailSettings = emailSettings.Value;
        }

        [HttpPost]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.InternalServerError)]
        public IActionResult SendEmail([FromBody] User user)
        {
            var request = CreateEmailRequest(user);
            var factory = new ConnectionFactory
            {
                HostName = rabbitMqSettings.HostName,
                Port = rabbitMqSettings.Port,
                UserName = rabbitMqSettings.UserName,
                Password = rabbitMqSettings.Password,
                RequestedHeartbeat = TimeSpan.FromSeconds(60),
                Ssl = {ServerName = rabbitMqSettings.HostName, Enabled = rabbitMqSettings.UseSsl}
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(QueueNames.UserEmail, false, false, false, null);
                var serializedMessage = JsonConvert.SerializeObject(request);
                var body = Encoding.UTF8.GetBytes(serializedMessage);

                try
                {
                    channel.BasicPublish("", QueueNames.UserEmail, null, body);

                    logger.LogInformation($"{Messages.EnqueueSucceeded}");

                    return Ok(Messages.EnqueueSucceeded);
                }
                catch (Exception ex)
                {
                    var errorMessage = ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorMessage = ex.InnerException.Message;
                    }

                    logger.LogError(errorMessage);

                    return Problem(errorMessage);
                }
            }
        }

        private EmailRequest CreateEmailRequest(User user)
        {
            return new()
            {
                From = emailSettings.EmailAddress,
                To = user.Email,
                Subject = "Test E-mail",
                Body = "Test E-mail"
            };
        }
    }
}