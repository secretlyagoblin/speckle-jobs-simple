using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeckleServer;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;

namespace SpeckleServer.Tests
{
    public class Speckle
    {


        private readonly List<string> _streams = new List<string>();

        [SetUp]
        public void Setup()
        {

        }

        [TearDown]
        public void TearDown()
        {
            using var client = CreateClient();

            _streams.ForEach(x => client.StreamDelete(x));
            _streams.Clear();
        }

        private Client CreateClient()
        {
            var token = new ConfigurationBuilder().AddUserSecrets(typeof(Program).Assembly).Build().GetValue<string>("SpeckleListener:XYZKey") ?? "";

            var account = new Account();
            account.token = token;
            account.serverInfo = new ServerInfo
            {
                url = "https://speckle.xyz"
            };

            var client = new Client(account);

            return client;
        }



        [Test]
        public async Task EventTriggers()
        {
            var speckleClient = CreateClient();

            var stream = await speckleClient.StreamCreate(new StreamCreateInput()
            {
                name = $"TEST{Random.Shared.Next()}",
                description = "Created as part of a testing process, this should automatically get wiped",
                isPublic = false
            });

            _streams.Add(stream); //ensures it gets wiped after test

            await using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();

            var commandName = "abcdef";
            var command = $"/commands/{commandName}";

            var result = await client.PutAsJsonAsync(command, new Command(Tests.GHFILE1));

            var targetPath = $"{speckleClient.ServerUrl}/streams/{stream}/branches/main";
            var destinationPath = $"{speckleClient.ServerUrl}/streams/{stream}/branches/notMain";


            var jobResponse = await client.PutAsJsonAsync($"jobs/new", new NewJobScema(targetPath, destinationPath, commandName));

            Task.Delay(TimeSpan.FromSeconds(5)).Wait();

            speckleClient.tra

            var commitCreated = await speckleClient.CommitCreate(new CommitCreateInput()
            {
                streamId = stream,
                branchName = "main",
                message = "Test commit",
                objectId
            });

            Task.Delay(TimeSpan.FromSeconds(5)).Wait();

            var getResponse = await client.GetAsync("results").Result.Content.ReadFromJsonAsync<JsonElement>();

            var strResponse = getResponse.GetRawText();

            Assert.That(getResponse.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(getResponse.GetArrayLength, Is.EqualTo(1));

        }
    }
}
