using Microsoft.EntityFrameworkCore;
using Speckle.Core.Api.SubscriptionModels;
using SpeckleServer.Database;

namespace SpeckleServer.RhinoJobber
{
    internal class RhinoJobService
    {

        private readonly AutomationDbContext _context;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly string _rhinoComputeUrl;
        private Task _baseTask = Task.CompletedTask;

        public RhinoJobService(AutomationDbContext context, IConfiguration configuration, IServiceScopeFactory scopeFactory)
        {
            this._context = context;
            this._scopeFactory = scopeFactory;
            _rhinoComputeUrl = configuration.GetValue<string>("Rhino:ComputeUrl") ?? "";
        }

        public void RunCommandFromCommit(CommitInfo commit)
        {
            var tasks = _context.Streams
                .Where(x => x.StreamId == commit.streamId)
                .Include(x => x.Jobs)
                .ThenInclude(x => x.Command)
                .ThenInclude(x => x.AutomationHistory)
                .SelectMany(x => x.Jobs)
                .Select(x => x.Command.AutomationHistory.Last());
            //.Async(TryRunGrasshopperScript,cancellationToken) ?? Task.CompletedTask;

        }

        public void RunCommandFromStream(string streamId)
        {
            var tasks = _context.Streams                
                .Include(x => x.Jobs)
                .ThenInclude(x => x.Command)
                .ThenInclude(x => x.AutomationHistory)
                .Where(x => x.StreamId == streamId)
                .ToList()
                .SelectMany(x => x.Jobs)
                .Select(x => x.Command.AutomationHistory.Last());
            //.Async(TryRunGrasshopperScript,cancellationToken) ?? Task.CompletedTask;

        }

        public void RunCommandByName(string command)
        {
            var tasks = _context.Commands
                .Where(x => x.Name == command)
                .Include(x => x.AutomationHistory)
                .Select(x => x.AutomationHistory.Last());

            var computer = _scopeFactory.CreateScope().ServiceProvider.GetService(typeof(RhinoComputeService)) as RhinoComputeService;

            computer?.StartBeDoingIt();

        }
    }

}
