using Microsoft.EntityFrameworkCore;

namespace SpeckleServer
{
    public class RhinoJobService : IHostedService
    {
        private readonly AutomationDbContext context;
        private readonly RhinoComputeQueue queue;

        public RhinoJobService(AutomationDbContext context, RhinoComputeQueue queue)
        {
            this.context = context;
            this.queue = queue;
        }

        public void Calculate(Speckle.Core.Api.SubscriptionModels.CommitInfo e)
        {

            var jobs = context.Streams
                .Where(x => x.StreamId == e.streamId)
                .Include(x => x.Jobs)
                .ThenInclude(x => x.Command)
                .ThenInclude(x => x.AutomationHistory)
                .SelectMany(x => x.Jobs)
                .Select(x => x.Command.AutomationHistory.Last());

            foreach (var job in jobs)
            {
                queue.AddAutomation(job);
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
            //throw new NotImplementedException();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
            //throw new NotImplementedException();
        }
    }
}
