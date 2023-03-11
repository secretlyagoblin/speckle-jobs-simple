namespace SpeckleServer
{
    public class ListenerService : BackgroundService
    {
        private readonly ISpeckleListener _speckleListener;
        private readonly IRhinoComputeListener _rhinoListener;

        public ListenerService(ISpeckleListener speckleListener, IRhinoComputeListener rhinoListener)
        {
            this._speckleListener = speckleListener;
            this._rhinoListener = rhinoListener;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }
    }
}
