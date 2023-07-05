using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TwitchLib.EventSub.Websockets.Extensions;

namespace Vamper
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging();
                    services.AddTwitchLibEventSubWebsockets();
                    services.AddHostedService<VamperWebsocketMonitorHostedService>();
                });
    }
}
