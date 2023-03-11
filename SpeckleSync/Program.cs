using SpeckleSync;
using SpeckleSync.Files;
using System.Net.Http;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddHostedService<Worker>()
    .AddSingleton<IFileWatcher, FileWatcher>()
    .AddSingleton<Pusher>();

var host = builder.Build();

host.Run();
