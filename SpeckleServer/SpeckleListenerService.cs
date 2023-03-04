using Microsoft.EntityFrameworkCore;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using SpeckleServer;
using SpeckleServer.Database;
using SpeckleServer.RhinoJobber;
using System.Collections;
using System.Text.RegularExpressions;

public class SpeckleListenerService 
{

    private readonly IServiceScopeFactory _scopeFactory;

    private string _rhinoComputeUrl = "http://localhost:5000";
    private readonly Client _client;

    public SpeckleListenerService(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;

        var account = new Account();
        account.token = configuration.GetValue<string>("SpeckleListener:XYZKey");
        account.serverInfo = new ServerInfo
        {
            url = "https://speckle.xyz/"
        };

        _client = new Client(account);
        _client.OnCommitCreated += Client_OnCommitCreated;

        UpdateStreams();

        //https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-7.0&tabs=visual-studio
    }

    public void UpdateStreams() {

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();

        dbContext.Streams.Select(x => x.StreamId).ToList().ForEach(x => _client.SubscribeCommitCreated(x));
    }

    private void Client_OnCommitCreated(object sender, Speckle.Core.Api.SubscriptionModels.CommitInfo e)
    {
        using var scope = _scopeFactory.CreateScope();
        var rj = scope.ServiceProvider.GetRequiredService<RhinoJobService>();




    
    }

    public SpeckleUrl ValidateWildcardSpeckleStream(string url)
    {
        string preamble, serverUrl, stream, path, key, value;

        //look, sorry, I was trying to be clever
        var pattern = $"(?<{nameof(preamble)}>(?<{nameof(serverUrl)}>.+\\.+\\w+\\/)?(?:.*streams\\/)?)(?<{nameof(stream)}>[\\w\\*]+)\\/(?<{nameof(path)}>(?<{nameof(key)}>\\w+)\\/(?<{nameof(value)}>[^\\?]+))";
        var regex = new Regex(pattern);
        var match = regex.Match(url);

        if (!match.Success) return new SpeckleUrl(false, "", "", "", "", "", "");

        //still sorry, I was still trying to be clever
        preamble = match.Groups.GetValueOrDefault(nameof(preamble))?.Value ?? "";
        serverUrl = match.Groups.GetValueOrDefault(nameof(serverUrl))?.Value ?? "";
        stream = match.Groups.GetValueOrDefault(nameof(stream))?.Value ?? "";
        path = match.Groups.GetValueOrDefault(nameof(path))?.Value ?? "";
        key = match.Groups.GetValueOrDefault(nameof(key))?.Value ?? "";
        value = match.Groups.GetValueOrDefault(nameof(value))?.Value ?? "";

        return new SpeckleUrl(true, preamble,serverUrl, stream, path, key, value);
    }

    public record SpeckleUrl(bool isValid, string preamble, string serverUrl, string stream, string path, string key, string value);







}