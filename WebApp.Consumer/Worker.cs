using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using WebApp.Core.Constants;
using WebApp.Core.Mailing;
using JsonException = System.Text.Json.JsonException;

namespace WebApp.Consumer
{
    public class Worker : BackgroundService
    {
        #region ctor

        private readonly ILogger<Worker> logger;
        private readonly IEmailService emailService;
        private readonly RabbitMqSettings rabbitMqSettings;
        private ConnectionFactory connectionFactory;
        private IConnection connection;
        private IModel channel;
        private readonly string queueName = QueueNames.UserEmail;

        public Worker(ILogger<Worker> logger, IOptions<RabbitMqSettings> rabbitMqSettings, IEmailService emailService)
        {
            this.logger = logger;
            this.rabbitMqSettings = rabbitMqSettings.Value;
            this.emailService = emailService;
        }

        #endregion

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            connectionFactory = new ConnectionFactory
            {
                HostName = rabbitMqSettings.HostName,
                Port = rabbitMqSettings.Port,
                UserName = rabbitMqSettings.UserName,
                Password = rabbitMqSettings.Password,
                DispatchConsumersAsync = true,
                RequestedHeartbeat = TimeSpan.FromSeconds(60),
                Ssl = {ServerName = rabbitMqSettings.HostName, Enabled = rabbitMqSettings.UseSsl}
            };

            connection = connectionFactory.CreateConnection();
            if (!connection.IsOpen)
            {
                logger.LogError(Messages.RabbitMqConnectionError);
            }

            channel = connection.CreateModel();
            channel.QueueDeclare(queueName, false, false, false, null);
            channel.BasicQos(0, 1, false);

            logger.LogInformation($"{queueName} {Messages.QueueListening}");

            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.Received += async (bc, ea) =>
                {
                    var message = GetMessage(ea);

                    try
                    {
                        var emailRequest = JsonConvert.DeserializeObject<EmailRequest>(message);
                        if (emailRequest != null)
                        {
                            await emailService.SendEmailAsync(emailRequest);
                        }
                    }
                    catch (JsonException ex)
                    {
                        logger.LogError($"{Messages.JsonParseError} - '{message}' - {ex}");
                        channel.BasicNack(ea.DeliveryTag, false, false);
                    }
                    catch (AlreadyClosedException ex)
                    {
                        logger.LogWarning($"{Messages.RabbitMqClosed} - {ex}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"{nameof(ExecuteAsync)}, {ex.Message}, {ex}");
                    }
                };

                channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

                await Task.Delay(1000, stoppingToken);
            }
        }

        private static string GetMessage(BasicDeliverEventArgs ea)
        {
            var body = ea.Body.ToArray();
            return Encoding.UTF8.GetString(body);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            connection.Close();
            logger.LogWarning(Messages.RabbitMqClosed);
        }
    }
}