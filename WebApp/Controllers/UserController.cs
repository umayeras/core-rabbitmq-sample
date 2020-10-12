using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using WebApp.Core.Constants;
using WebApp.Core.Entities;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private IConfiguration Configuration { get; }
        
        public UserController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        [HttpPost, Route("send-email")]
        public IActionResult Index(User user)
        {
            var rabbitSettings = Configuration.GetSection(SectionNames.RabbitMqSettings).Get<RabbitMqSettings>();
            var factory = new ConnectionFactory {HostName = rabbitSettings.Uri};
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(QueueNames.UserEmail, false, false, false, null);
                var message = JsonConvert.SerializeObject(user);
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish("", QueueNames.UserEmail, null, body);
            }

            return Ok(Messages.GeneralSuccess);
        }
    }
}