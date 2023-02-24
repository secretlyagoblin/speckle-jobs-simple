using Microsoft.EntityFrameworkCore;
using Speckle.Core.Api.SubscriptionModels;
using SpeckleServer.Database;

namespace SpeckleServer.RhinoJobber
{
    internal class RhinoJobService
    {

        private readonly AutomationDbContext _context;
        private readonly IServiceScopeFactory _scopeFactory;


        public RhinoJobService(AutomationDbContext context, IConfiguration configuration, IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _scopeFactory = scopeFactory;
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

        public JobTicket RunCommandByName(string command, CommandRunSettings runSettings)
        {
            var task = _context.Commands
                .Where(x => x.Name == command)
                .Include(x => x.AutomationHistory)
                .Select(x => x.AutomationHistory.Last()).Single();

            var computer = _scopeFactory.CreateScope().ServiceProvider.GetService(typeof(RhinoComputeService)) as RhinoComputeService;

            if(computer is null)
            {
                throw new Exception("Rhino Compute Service could not be started");
            }


             
            return computer.StartJob(runSettings.commitUrl, task.GhString);
        }
    }

}
