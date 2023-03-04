using Microsoft.EntityFrameworkCore;
using Speckle.Core.Api.SubscriptionModels;
using SpeckleServer.Database;
using System.Threading.Tasks;

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

        public IEnumerable<JobTicket> RunCommandFromStream(string server, string streamId, string branch) => _context
                .Streams
                .Include(x => x.Jobs)
                .ThenInclude(x => x.Command)
                .ThenInclude(x => x.AutomationHistory)
                .Where(x => x.StreamId == streamId)
                .ToList()
                .SelectMany(x => x.Jobs)
                .Where(x => new SpeckleUrl(x.TriggeringUrl).MatchesBranchPattern(branch))
                .Select(x => RunCommandByName(x.Command.Name, new CommandRunSettings(
                    commitUrl: $"{server}/streams/{streamId}/branches/{branch}")))
                .ToList();

        private JobTicket RunAutomation(Automation automation, CommandRunSettings runSettings)
        {
            if (_scopeFactory.CreateScope().ServiceProvider.GetService(typeof(RhinoComputeService)) is not RhinoComputeService computer)
            {
                throw new Exception("Rhino Compute Service could not be started");
            }

            return computer.StartJob(runSettings.commitUrl, automation.GhString);
        }

        public JobTicket RunCommandByName(string command, CommandRunSettings runSettings) => _context
                .Commands
                .Where(x => x.Name == command)
                .Include(x => x.AutomationHistory)
                .Select(x => x.AutomationHistory.Last())
                .Select(x => RunAutomation(x, runSettings))
                .Single();
    }

}
