using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeckleServer;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSingleton<SpeckleListenerService>();
builder.Services.AddScoped<RhinoJobService>();
builder.Services.AddSingleton<RhinoComputeQueue>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AutomationDbContext>(options => options.UseInMemoryDatabase("items"));

var app = builder.Build();

app.MapPost("/start", ([FromServices] SpeckleListenerService speckle, [FromServices] RhinoJobService rhino ) =>
{

});

app.MapPost("/stop", ([FromServices] SpeckleListenerService service, [FromServices] RhinoJobService rhino) =>
{
    
});

app.MapGet("/commands", ([FromServices] AutomationDbContext db) => {
    return db.NamedAutomations
    .Include(x => x.AutomationHistory)
    .Select(x => new
    {
        name = x.Name,
        versions = x.AutomationHistory.Count
    })
    .ToList();
});

app.MapGet("/commands/{command}", (string command, [FromServices] AutomationDbContext db) => {
    return db.NamedAutomations
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

app.MapPut("/commands/{command}", (string command, string ghString, [FromServices] AutomationDbContext db) =>{

    var automation = db.Automations.Add(new Automation()
    {
        GhString = ghString
    }).Entity;

    var namedStep = db.NamedAutomations.Find(command) is Command n
        ? db.NamedAutomations.Attach(n).Entity
        : db.NamedAutomations.Add(new Command() { Name = command }).Entity;

    namedStep.AutomationHistory.Add(automation);

    //var changes = db.ChangeTracker.ToDebugString();

    db.SaveChanges();

});

app.MapGet("/jobs", ([FromServices] AutomationDbContext db) => {
    return db.Jobs
    .Select(x => new
    {
        stream = x.StreamId,
        command = x.CommandId
    })
    .ToList();;
});

app.MapPut("/jobs/{stream}/{command}", (string stream, string command, [FromServices] AutomationDbContext db, [FromServices] SpeckleListenerService sl) =>{

    var commandRecord = db.NamedAutomations.Where(x => x.Name == command).Include(x => x.Jobs).SingleOrDefault();

    if (commandRecord == null) throw new Exception($"Command {command} does not exist");

    if (commandRecord.Jobs.Where(x=>x.StreamId == stream).SingleOrDefault() is not null)
    {
        //return Results.Ok;
        return;
    }

    var streamRecord = db.Streams.Find(stream) is Stream str
        ? db.Streams.Attach(str).Entity
        : db.Streams.Add(new Stream() { StreamId = stream }).Entity;

    var job = db.Jobs.Add(new Job()
    {
        Command = commandRecord,
        Stream = streamRecord
    });

    db.SaveChanges();

    sl.UpdateStreams();
});

app.MapGet("/streams", ([FromServices] AutomationDbContext db) => {
    return db.Streams
    .Include(x => x.Jobs)
    .Select(x => new
    {
        name = x.StreamId,
        jobs = x.Jobs.Select(x => x.CommandId)
    })
    .ToList();
});

app.MapGet("/streams/{stream}", (string stream, [FromServices] AutomationDbContext db) => {
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
