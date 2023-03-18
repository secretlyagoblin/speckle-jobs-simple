using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using SpeckleServer.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeckleSync
{
    public class DirectoryListenerService : IHostedService
    {
        private readonly ILogger<DirectoryListenerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _watchPath;
        private readonly FileSystemWatcher _watcher;

        public DirectoryListenerService(ILogger<DirectoryListenerService> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient(nameof(SpeckleServer));

            _watchPath = _configuration.GetValue<string>("TargetDirectory") ?? throw new Exception();
            _watchPath = Path.GetFullPath(_watchPath);

            if (!Directory.Exists(_watchPath))
                Directory.CreateDirectory(_watchPath);

            _httpClient.GetAsync("/")
                //.WaitAsync(TimeSpan.FromSeconds(15))
                .ContinueWith(x =>
                {
                    if (x.Result.StatusCode == System.Net.HttpStatusCode.OK) return;
                    throw new Exception(x.Result.StatusCode.ToString());
                });

            _watcher = new FileSystemWatcher(_watchPath);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting MyService...");
            _watcher.Created += PushTheUpdate;

            _watcher.EnableRaisingEvents = true;




            _watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
            | NotifyFilters.Size;

            _watcher.Changed += _fileSystemWatcher_Changed;
            _watcher.Created += _fileSystemWatcher_Created;
            _watcher.Deleted += _fileSystemWatcher_Deleted;
            _watcher.Renamed += _fileSystemWatcher_Renamed;
            _watcher.Error += _fileSystemWatcher_Error;

            _watcher.EnableRaisingEvents = true;
            _watcher.IncludeSubdirectories = true;

            _logger.LogInformation($"File Watching has started for directory {_watcher}");

            return Task.CompletedTask;
        }

        private async void _fileSystemWatcher_Error(object sender, ErrorEventArgs e)
        {
            _logger.LogInformation($"File error event {e.GetException().Message}");
        }

        private async void _fileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            _logger.LogInformation($"File rename event for file {e.FullPath}");
            PushTheUpdate(sender, e);
        }

        private async void _fileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation($"File deleted event for file {e.FullPath}");
            PushTheUpdate(sender, e);
        }

        private async void _fileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation($"File changed event for file {e.FullPath}");
            PushTheUpdate(sender, e);
        }

        private async void _fileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation($"File created event for file {e.FullPath}");
            PushTheUpdate(sender, e);
        }

        private async void PushTheUpdate(object s, FileSystemEventArgs e)
        {
            var fileInfo = new FileInfo(e.FullPath);

            var path = fileInfo.FullName;

            _logger.LogInformation($"Before");

            if (!File.Exists(path))
            {
                _logger.LogWarning($"File doesn't exist: {path}");
                return;
            }


            if (Path.GetExtension(path) != ".gh")
            {
                _logger.LogWarning($"Skipped file: {path}");
                return;
            }

            _logger.LogInformation($"Starting read of {path}");

            var str = Convert.ToBase64String(File.ReadAllBytes(path));

            _logger.LogInformation($"Serialised Base64 String ({str.Length} chars) as '{new string(str.Take(40).ToArray())}...' ");

            var command = $"commands/{Path.GetFileNameWithoutExtension(path)}";

            _logger.LogInformation($"Attempting push to '{_httpClient.BaseAddress}{command}'");

            try
            {
                var message = new
                {
                    ghString = Convert.ToBase64String(File.ReadAllBytes(path))
                };

                var res = await _httpClient.PutAsJsonAsync(command, message).WaitAsync(TimeSpan.FromSeconds(10));

                if (res.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Completed upload of {path}");
                    return;
                }

                _logger.LogInformation($"Failed upload of {path} with error code {res.StatusCode}");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Failed upload of {path} with error code {ex.Message}");
                return;
            }
        }


    

    public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping MyService...");

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _watcher.Dispose();
        }
    }


}
