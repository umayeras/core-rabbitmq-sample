using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApp.Core.Constants;
using WebApp.Core.Mailing;

namespace WebApp.Consumer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).ConfigureServices((hostContext, services) =>
            {
                var configuration = hostContext.Configuration;
                services.Configure<RabbitMqSettings>(configuration.GetSection(SectionNames.RabbitMqSettings));
                services.AddSingleton<IEmailService, EmailService>();
                services.AddHostedService<Worker>();
            });
    }
}