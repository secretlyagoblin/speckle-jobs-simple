using SpeckleSync.Files;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SpeckleSync
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IFileWatcher _watcher;

        public Worker(ILogger<Worker> logger, IFileWatcher watcher)
        {             
            _logger = logger;
            _watcher = watcher;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _watcher.Start();
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.Log(LogLevel.Information, "Yep Still goin");

                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }
    }
}
