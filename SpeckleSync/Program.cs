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
                    services.AddHostedService<Worker>()
                        .AddSingleton<IFileWatcher, FileWatcher>()
                        .AddSingleton<Pusher>();
                })
                .Build();

            host.Run();
        }
    } 
}
