using SpeckleSync;
using System.Net.Http;

namespace SpeckleSync
{
    public class Program
    {
        private static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHttpClient();
                    services.AddSingleton<IHostedService, DirectoryListenerService>();
                });

            host.Build().Run();
        }
    } 
}
