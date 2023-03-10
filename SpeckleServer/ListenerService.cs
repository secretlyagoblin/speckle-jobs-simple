namespace SpeckleServer
{
    public class ListenerService : BackgroundService
    {
        private readonly SpeckleListener _speckleListener;
        private readonly RhinoComputeListener _rhinoListener;

        public ListenerService(SpeckleListener speckleListener, RhinoComputeListener rhinoListener)
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
