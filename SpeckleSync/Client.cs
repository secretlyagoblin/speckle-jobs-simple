using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SpeckleSync
{
    public class JobTicket : IJobTicket
    {
        public JobTicket(string id)
        {
            Id= id;
        }

        public string Id { get; }
    }

    public class FileJob : IJobDetails
    {
        public FileJob(string filePath)
        {
            FilePath = filePath;
        }

        public string FilePath { get; }

        public IJobTicket GetJobTicket()
        {
            var guid = Guid.NewGuid();
            var key = Convert.ToBase64String(guid.ToByteArray())
                .Substring(0, 22)
                .Replace("/", "_");

            return new JobTicket(key);
        }
    }



    public class Pusher : JobberSingleton<FileJob>
    {
        private readonly ILogger _logger;
        private readonly HttpClient _client;

        public Pusher(IConfiguration configuration, ILogger<Pusher> logger) : base()
        {
            this._logger = logger;
            var baseUrl = configuration.GetValue<string>("SpeckleHttpService") ?? "";

            _client = new HttpClient()
            {
                BaseAddress = new Uri(baseUrl)
            };
        }

        public class Result : IResult
        {
            public Result(object? obj, ResultType type, string message)
            {
                ResultValue = obj;
                ResultType = type;
                Message = message;
            }

            public object? ResultValue { get; }
            public ResultType ResultType { get; }
            public string? Message { get; }
        }
        

        public class NoJobRunResult : IResult
        {
            public ResultType ResultType => ResultType.Skipped;

            public object? ResultValue => null;

            public string? Message => "Job not performed";
        }



        protected override IResult RunJob(FileJob job)
        {
            var path = job.FilePath;

            _logger.LogInformation($"Before");

            if (!File.Exists(path)) return new NoJobRunResult();

            if (Path.GetExtension(path) != ".gh") return new NoJobRunResult();

            _logger.LogInformation($"Starting read of {path}");

            var str = Convert.ToBase64String(File.ReadAllBytes(path));

            _logger.LogInformation($"Serialised Base64 String ({str.Length} chars) as '{new string(str.Take(40).ToArray())}...' ");

            var command = $"commands/{Path.GetFileNameWithoutExtension(path)}";

            _logger.LogInformation($"Attempting push to '{_client.BaseAddress}{command}'");

            var message = "";

            try
            {

                var res = _client.PutAsJsonAsync(command, new
                {
                    ghString = Convert.ToBase64String(File.ReadAllBytes(path))
                }).WaitAsync(TimeSpan.FromSeconds(10)).Result;

                if (res.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Completed upload of {path}");
                    return new Result(null, ResultType.Success, $"Completed upload of {path}");
                }

                _logger.LogInformation($"Failed upload of {path} with error code {res.StatusCode}");
                return new Result(null, ResultType.Fail, $"Failed upload of {path} with error code {res.StatusCode}");

            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Failed upload of {path} with error code {ex.Message}");
                return new Result(null, ResultType.Fail, $"Failed upload of {path} with error code {ex.Message}");
            }
        }
    }
}