using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeckleServer;
using SpeckleServer.Database;
using SpeckleServer.RhinoJobber;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpeckleServer
{

    public class Program
    {
        private static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.Services.AddHostedService<ListenerService>();
            builder.Services.AddSingleton<ISpeckleListener, SpeckleListener>();
            builder.Services.AddSingleton<IRhinoComputeListener, RhinoComputeListener>();
            builder.Services.AddScoped<IRhinoJobService, RhinoJobService>();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddDbContext<AutomationDbContext>(options => options.UseInMemoryDatabase("items"));

            var app = builder.Build();

            app.MapGet("/commands", ([FromServices] AutomationDbContext db) =>
            {
                return db.Commands
                .Include(x => x.AutomationHistory)
                .Select(x => new
                {
                    name = x.Name,
                    versions = x.AutomationHistory.Count
                })
                .ToList();
            });

            app.MapGet("/commands/{command}", (string command, [FromServices] AutomationDbContext db) =>
            {
                return db.Commands
                .Where(x => x.Name == command)
                .Include(x => x.AutomationHistory)
                .Select(x => new
                {
                    name = x.Name,
                    versions = x.AutomationHistory.Select(y =>
                    new
                    {
                        id = y.AutomationId,
                        date = y.DateTime
                    })
                })
                .ToList();
            });

            app.MapPut("/commands/{command}", (string command, Command commandPayload, [FromServices] AutomationDbContext db) =>
            {

                if (string.IsNullOrEmpty(command))
                {
                    return Results.BadRequest("Invalid command");
                }

                if (commandPayload?.GhString is null)
                {
                    return Results.BadRequest("Invalid command payload");
                }

                try
                {

                    var automation = db.Automations.Add(new Automation()
                    {
                        GhString = commandPayload.GhString
                    }).Entity;

                    var namedStep = db.Commands.Find(command) is SpeckleServer.Database.Command n
                        ? db.Commands.Attach(n).Entity
                        : db.Commands.Add(new SpeckleServer.Database.Command() { Name = command }).Entity;

                    namedStep.AutomationHistory.Add(automation);

                    //var changes = db.ChangeTracker.ToDebugString();

                    db.SaveChanges();

                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to load request: {ex.Message}");
                }

                return Results.Ok();

            });

            app.MapPost("/command/{command}/run", (string command, CommandRunSettings runSettings, [FromServices] IRhinoJobService rh) =>
            {
                return rh.RunCommandByName(command, runSettings);
            });

            app.MapGet("/jobs", ([FromServices] AutomationDbContext db) =>
            {
                return db.Jobs
                .Select(x => new
                {
                    stream = x.StreamId,
                    command = x.CommandId,
                    hint = x.DestinationUrlHint,
                })
                .ToList(); ;
            });

            app.MapPut("/jobs/new", (NewJobScema jobSchema, [FromServices] AutomationDbContext db, [FromServices] ISpeckleListener sl) =>
            {

                var command = jobSchema.command;

                var urls = new[] { jobSchema.targetPath, jobSchema.destinationPath }.Select(x => new SpeckleUrl(x));

                foreach (var result in urls)
                {
                    if (!result.IsValid) return Results.BadRequest("Invalid Speckle stream");

                    var stream = result.Stream;
                    var key = result.Key;

                    if (stream.Contains('*') || string.IsNullOrWhiteSpace(stream)) return Results.BadRequest("A stream cannot be empty or a wildcard value");

                    if (key is not "branches") return Results.BadRequest("We can only create requests for branches at the moment");
                }

                var target = urls.First();

                var commandRecord = db.Commands.Where(x => x.Name == command).Include(x => x.Jobs).SingleOrDefault();

                if (commandRecord == null) return Results.BadRequest($"Command {command} does not exist");

                if (commandRecord.Jobs.Where(x => x.StreamId == target.Stream).SingleOrDefault() is not null)
                {
                    return Results.Ok();
                }

                var streamRecord = db.Streams.Find(target.Stream) is SpeckleServer.Database.Stream str
                    ? db.Streams.Attach(str).Entity
                    : db.Streams.Add(new SpeckleServer.Database.Stream() { StreamId = target.Stream }).Entity;

                var job = db.Jobs.Add(new SpeckleServer.Database.Job()
                {
                    Command = commandRecord,
                    Stream = streamRecord,
                    TriggeringUrl = jobSchema.targetPath,
                    DestinationUrlHint = jobSchema.destinationPath
                });

                db.SaveChanges();

                sl.UpdateStreams();

                return Results.Ok();
            });

            app.MapGet("/streams", ([FromServices] AutomationDbContext db) =>
            {
                return db.Streams
                .Include(x => x.Jobs)
                .Select(x => new
                {
                    name = x.StreamId,
                    jobs = x.Jobs.Select(x => x.CommandId)
                })
                .ToList();
            });

            app.MapGet("/streams/{stream}", (string stream, [FromServices] AutomationDbContext db) =>
            {
                return db.Streams
                .Where(x => x.StreamId == stream)
                .Include(x => x.Jobs)
                .Select(x => new
                {
                    name = x.StreamId,
                    jobs = x.Jobs.Select(x => x.CommandId)
                })
                .SingleOrDefault();
            });

            app.MapGet("/history", (int count, int offset, [FromServices] AutomationDbContext db) =>
            {
                var query = db.Automations.Reverse().Skip(offset);

                if (count > 0) query = query.Take(count);

                return query.Include(x => x.Command).Select(x =>
                new
                {
                    id = x.AutomationId,
                    date = x.DateTime,
                    name = x.Command.Name
                });
            });

            app.MapGet("/results", ([FromServices] IRhinoComputeListener rc) =>
            {
                return rc.GetLatestJobsAndClearQueue().Select(x =>
                new
                {
                    job = x
                });
            });



            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    } 

    public record NewJobScema(string targetPath, string destinationPath, string command);
    public record Command(string GhString);
    public record CommandRunSettings(string commitUrl);

}
