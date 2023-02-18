using Microsoft.EntityFrameworkCore;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using SpeckleServer;
using SpeckleServer.Database;
using SpeckleServer.RhinoJobber;
using System.Collections;
using System.Collections.Concurrent;

public class RhinoComputeService 
{

    private readonly IServiceScopeFactory _scopeFactory;

    private string _rhinoComputeUrl = "http://localhost:5000";

    private readonly ConcurrentQueue<JobDoer> jobDoers= new ConcurrentQueue<JobDoer>();

    public ConcurrentQueue<string> computeJobs = new ConcurrentQueue<string>();

    private bool isBeDoing = false;

    public RhinoComputeService(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _rhinoComputeUrl = configuration.GetValue<string>("Rhino:ComputeUrl") ?? "";
    }

    public void StartBeDoingIt()
    {
        jobDoers.Enqueue(new JobDoer(Random.Shared.Next().ToString()));

        if (isBeDoing) return;

        isBeDoing= true;

        Task.Run(() =>
        {
            while(isBeDoing&& jobDoers.Count > 0)
            {
                if(jobDoers.TryDequeue(out var job) && job is not null)
                {
                    Task.Delay(200).Wait();
                    computeJobs.Enqueue($"My goodness, seems a job named '{job.a}' done be donne");
                }
            }
            isBeDoing = false;

            computeJobs.Enqueue($"My goodness, seems a job Terminated Correct");
        });
    }


}

public record JobDoer(string a);