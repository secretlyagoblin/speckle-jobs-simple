using SpeckleSync;
using SpeckleSync.Files;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddSingleton<IFileWatcher, FileWatcher>();
        services.AddScoped<IFileConsumerService, FileConsumerService>();
    })
    .Build();

host.Run();
