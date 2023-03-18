using SpeckleSync;
using SpeckleSync.Files;
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
                    services
                        .AddHttpClient(nameof(SpeckleServer), x => { x.BaseAddress = new Uri(hostContext.Configuration.GetValue<string>("SpeckleHttpService") ?? ""); });

                    services
                        .AddHostedService<Worker>()                        
                        .AddSingleton<IFileWatcher, FileWatcher>()
                        .AddTransient<Pusher>();
                })
                .Build();

            host.Run();
        }
    } 
}
