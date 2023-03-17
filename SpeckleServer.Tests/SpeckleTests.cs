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
using Speckle.Core.Transports;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Speckle.Core.Models;

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

            _streams.ForEach(x => client.StreamDelete(x).Wait());
            _streams.Clear();
        }

        private Client CreateClient()
        {
            var token = new ConfigurationBuilder().AddUserSecrets(typeof(SpeckleServer.Program).Assembly).Build().GetValue<string>("SpeckleListener:XYZKey") ?? "";

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

            await using var application = new WebApplicationFactory<SpeckleServer.Program>();
            using var client = application.CreateClient();

            var commandName = "abcdef";
            var command = $"/commands/{commandName}";

            var result = await client.PutAsJsonAsync(command, new Command(Tests.GHFILE1));

            var targetPath = $"{speckleClient.ServerUrl}/streams/{stream}/branches/main";
            var destinationPath = $"{speckleClient.ServerUrl}/streams/{stream}/branches/notMain";


            var jobResponse = await client.PutAsJsonAsync($"/jobs/new", new NewJobScema(targetPath, destinationPath, commandName));

            Assert.That(jobResponse.IsSuccessStatusCode);

            Task.Delay(TimeSpan.FromSeconds(5)).Wait();

            var transport = new ServerTransport(speckleClient.Account, stream);

            var dummyData = new Base();
            dummyData["dummyInt"] = 42;
            dummyData["dummyChild"] = new Base();
            ((Base)dummyData["dummyChild"])["dummyName"] = "NamedChild";            

            var objectId = await Operations.Send(dummyData, new List<ITransport> { transport }, disposeTransports: true);

            var commitCreated = await speckleClient.CommitCreate(new CommitCreateInput()
            {
                streamId = stream,
                branchName = "main",
                message = "Test commit",
                objectId = objectId
            });

            Task.Delay(TimeSpan.FromSeconds(15)).Wait();

            var getResponse = await client.GetAsync("results").Result.Content.ReadFromJsonAsync<JsonElement>();

            var strResponse = getResponse.GetRawText();

            Assert.That(getResponse.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(getResponse.GetArrayLength, Is.EqualTo(1));

        }
    }
}
