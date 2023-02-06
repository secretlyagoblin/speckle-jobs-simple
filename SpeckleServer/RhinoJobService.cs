using Microsoft.EntityFrameworkCore;
using Speckle.Core.Api.SubscriptionModels;

namespace SpeckleServer
{
    public class RhinoJobService : BackgroundService
    {
        private readonly CommitInfo _commitInfo;
        private readonly AutomationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly string _rhinoComputeUrl;

        public RhinoJobService(CommitInfo commitInfo, AutomationDbContext context, IConfiguration configuration, IWebHostEnvironment environment)
        {
            this._commitInfo = commitInfo;
            this._context = context;
            this._environment = environment;
            _rhinoComputeUrl = configuration.GetValue<string>("Rhino:ComputeUrl") ?? "";
        }

        //public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        //public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        private static async Task<GrasshopperScriptResult> TryRunGrasshopperScript(Automation automation)
        {
            var delay = Random.Shared.Next(2000, 7000);

            await Task.Delay(delay);

            return new GrasshopperScriptResult($"Task completed after {delay} millisecond delay.");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tasks = _context.Streams
                .Where(x => x.StreamId == _commitInfo.streamId)
                .Include(x => x.Jobs)
                .ThenInclude(x => x.Command)
                .ThenInclude(x => x.AutomationHistory)
                .SelectMany(x => x.Jobs)
                .Select(x => x.Command.AutomationHistory.Last());
            //.Async(TryRunGrasshopperScript,cancellationToken) ?? Task.CompletedTask;

            return Parallel.ForEachAsync(tasks, async (x, cancellationToken) => {
                var result = await TryRunGrasshopperScript(x);

                Console.WriteLine(result);

                //do something
            });
        }
    }

    public record GrasshopperScriptResult(string Message);
}
