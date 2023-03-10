﻿using Microsoft.EntityFrameworkCore;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using SpeckleServer;
using SpeckleServer.Database;
using SpeckleServer.RhinoJobber;
using System.Collections;
using System.Text.RegularExpressions;

public class SpeckleListener 
{

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly Client _client;

    public SpeckleListener(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;

        var account = new Account();
        account.token = configuration.GetValue<string>("SpeckleListener:XYZKey");
        account.serverInfo = new ServerInfo
        {
            url = "https://speckle.xyz"
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
        var dbContext = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();

        rj.RunCommandFromStream(_client.ServerUrl, e.streamId, e.branchName);
    }









}