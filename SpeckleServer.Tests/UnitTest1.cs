using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Configuration;

namespace SpeckleServer.Tests
{
    public class Tests
    {
        const string GHFILE1 = "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";
        
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public async Task TestRootEndpoint()
        {
            await using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();

            var response = await client.PutAsJsonAsync("/commands/myCommand", new Command("sadgasd"));

            Assert.That(response?.StatusCode, Is.Not.Null);
            Assert.That(response?.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        }

        [Test]
        public async Task TestRootEndpointNull()
        {
            await using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();

            var response = await client.PutAsJsonAsync("/commands/myCommand", new Command(null));

            Assert.That(response?.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task CommandAddedToStream()
        {
            await using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();

            var commandName = "doBattleWithZeFoole";
            var speckleStream = "111xxx111";

            var command = $"/commands/{commandName}";

            var commandResponse = await client.PutAsJsonAsync(command, new Command("xxx111"));
            var jobResponse = await client.PutAsync($"jobs/{speckleStream}/{commandName}",null);
            var getResponse = await client.GetAsync("jobs").Result.Content.ReadFromJsonAsync<JsonElement>();

            Assert.That(getResponse[0].GetProperty("command").GetString(), Is.EqualTo(commandName));
            Assert.That(getResponse[0].GetProperty("stream").GetString(), Is.EqualTo(speckleStream));
        }

        [Test]
        public async Task CommandRun()
        {
            await using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();
            

            var commandName = "testCommand";
            //var speckleStream = "123123123123";
            var grasshopperSerialisedStream = GHFILE1;

            var command = $"/commands/{commandName}";

            await client.PutAsJsonAsync(command, new Command(grasshopperSerialisedStream));
            //await client.PutAsync($"jobs/{speckleStream}/{commandName}", null);

            var commandRunSettings = new CommandRunSettings("https://speckle.xyz/streams/511eeb2612/globals");

            var resA = await client.PostAsJsonAsync($"/command/{commandName}/run", commandRunSettings);
            var resB = await client.PostAsJsonAsync($"/command/{commandName}/run", commandRunSettings);
            var resC = await client.PostAsJsonAsync($"/command/{commandName}/run", commandRunSettings);
            var resD = await client.PostAsJsonAsync($"/command/{commandName}/run", commandRunSettings);
            var resE = await client.PostAsJsonAsync($"/command/{commandName}/run", commandRunSettings);

            var getResponse = await client.GetAsync("results").Result.Content.ReadFromJsonAsync<JsonElement>();

            Assert.That(getResponse.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(getResponse.GetArrayLength, Is.EqualTo(0));

            Task.Delay(TimeSpan.FromSeconds(90)).Wait();

            getResponse = await client.GetAsync("results").Result.Content.ReadFromJsonAsync<JsonElement>();

            Assert.That(getResponse.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(getResponse.GetArrayLength, Is.EqualTo(5));



        }
    }
}