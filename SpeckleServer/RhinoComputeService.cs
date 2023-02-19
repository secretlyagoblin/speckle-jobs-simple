using Microsoft.EntityFrameworkCore;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Newtonsoft.Json.Linq;
using SpeckleServer;
using SpeckleServer.Database;
using SpeckleServer.RhinoJobber;
using System.Collections;
using System.Collections.Concurrent;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics.Metrics;
using System.Text.Json;

public class RhinoComputeService 
{

    private readonly IServiceScopeFactory _scopeFactory;

    private string _rhinoComputeUrl = "http://localhost:5000";
    private readonly HttpClient _client;
    private readonly ConcurrentQueue<Job> jobDoers= new ConcurrentQueue<Job>();

    public ConcurrentQueue<string> computeJobs = new ConcurrentQueue<string>();

    private bool jobQueueIsCurrentlyIterating = false;

    private readonly string _token;

    public RhinoComputeService(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _rhinoComputeUrl = configuration.GetValue<string>("Rhino:ComputeUrl") ?? "";
        _token = configuration.GetValue<string>("SpeckleListener:XYZKey") ?? "";

        _client = new HttpClient()
        {
            BaseAddress = new Uri(_rhinoComputeUrl)
        };
    }

    public void StartJob(string stream, string algo)
    {
        jobDoers.Enqueue(new Job(stream,_token,algo));

        if (jobQueueIsCurrentlyIterating) return;

        jobQueueIsCurrentlyIterating = true;

        Task.Run(() =>
        {
            while(jobQueueIsCurrentlyIterating && !jobDoers.IsEmpty)
            {
                if(jobDoers.TryDequeue(out var job) && job is not null)
                {
                    RunJobOnCompute(job);
                }
            }
            jobQueueIsCurrentlyIterating = false;
        });
    }

    private void RunJobOnCompute(Job job)
    {
        const string? POINTER = null;

        var schema = new
        {
            absolutetolerance = 0.01,
            angletolerance = 1.0,
            modelunits = "Meters",
            algo = job.Algo,
            pointer = POINTER,
            cachesolve = false,
            recursionlevel = 0,
            values = new[] {
              new {
                ParamName = "InputStream",
                  InnerTree = new Dictionary < string, object[] > {
                    {
                      "0",
                      new [] {
                        new {
                          type = "System.String",
                          data = $"\"{job.Stream}\""
                        }
                      }
                    }
                  }
              },
              new {
                ParamName = "Token",
                  InnerTree = new Dictionary < string, object[] > {
                    {
                      "0",
                      new [] {
                        new {
                          type = "System.String",
                          data = $"\"{job.Token}\""
                        }
                      }
                    }
                  }
              }
            },
            warnings = Array.Empty<object>(),
            errors = Array.Empty<object>()
        };

        try
        {
            var script = _client.PostAsJsonAsync("/grasshopper", schema).Result.Content.ReadFromJsonAsync<JsonElement>().Result;
            computeJobs.Enqueue(script.GetRawText());

        }catch(Exception ex)
        {
            computeJobs.Enqueue(ex.Message);
        }
    }


}

public record Job(string Stream, string Token, string Algo);