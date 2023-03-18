using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeckleSync.Files
{
    public class FileWatcher : IFileWatcher
    {
        private string _fileFilter = "*.*";
        FileSystemWatcher _fileSystemWatcher;
        private string _targetDirectory;
        ILogger<FileWatcher> _logger;
        IServiceProvider _serviceProvider;

        public FileWatcher(ILogger<FileWatcher> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _targetDirectory = configuration.GetValue<string>("TargetDirectory") ?? "";            
            _logger = logger;

            var path = Path.GetFullPath(_targetDirectory);

            if(!Directory.Exists(_targetDirectory))
                Directory.CreateDirectory(_targetDirectory);
            _fileSystemWatcher = new FileSystemWatcher(_targetDirectory, _fileFilter);
            _serviceProvider = serviceProvider;
        }

        public void Start()
        {
            _fileSystemWatcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            _fileSystemWatcher.Changed += _fileSystemWatcher_Changed;
            _fileSystemWatcher.Created += _fileSystemWatcher_Created;
            _fileSystemWatcher.Deleted += _fileSystemWatcher_Deleted;
            _fileSystemWatcher.Renamed += _fileSystemWatcher_Renamed;
            _fileSystemWatcher.Error += _fileSystemWatcher_Error;

            _fileSystemWatcher.EnableRaisingEvents = true;
            _fileSystemWatcher.IncludeSubdirectories = true;

            _logger.LogInformation($"File Watching has started for directory {_targetDirectory}");
        }

        private void _fileSystemWatcher_Error(object sender, ErrorEventArgs e)
        {
            _logger.LogInformation($"File error event {e.GetException().Message}");
        }

        private void _fileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            _logger.LogInformation($"File rename event for file {e.FullPath}");
        }

        private void _fileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation($"File deleted event for file {e.FullPath}");
        }

        private void _fileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
        }

        private void _fileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            using var scope = _serviceProvider.CreateScope();
            var consumerService = scope.ServiceProvider.GetRequiredService<Pusher>();
            consumerService.RunJob(new FileJob(e.FullPath)).ContinueWith(x => _logger.Log(LogLevel.Information,$"Job fired with result: {x.IsCompletedSuccessfully}"));
        }
    }
}
