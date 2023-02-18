using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SpeckleServer.Tests
{
    public class Tests
    {
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

            var content = new StringContent("ghString=burt");

            var response = await client.PutAsJsonAsync<Command>("/commands/myCommand", new Command(null));

            Assert.That(response?.StatusCode, Is.Not.Null);
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
            

            var commandName = "doBattleWithZeFoole";
            var speckleStream = "111xxx111";
            var grasshopperSerialisedStream = "qwesfgh";

            var command = $"/commands/{commandName}";

            await client.PutAsJsonAsync(command, new Command(grasshopperSerialisedStream));
            await client.PutAsync($"jobs/{speckleStream}/{commandName}", null);

            await client.PostAsync($"/command/{commandName}/run", null);
            await client.PostAsync($"/command/{commandName}/run", null);
            await client.PostAsync($"/command/{commandName}/run", null);
            await client.PostAsync($"/command/{commandName}/run", null);
            await client.PostAsync($"/command/{commandName}/run", null);

            var getResponse = await client.GetAsync("results").Result.Content.ReadFromJsonAsync<JsonElement>();

            Assert.That(getResponse.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(getResponse.GetArrayLength, Is.EqualTo(0));

            Task.Delay(2000).Wait();


            getResponse = await client.GetAsync("results").Result.Content.ReadFromJsonAsync<JsonElement>();

            Assert.That(getResponse.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(getResponse.GetArrayLength, Is.EqualTo(6));



        }

        [Test]
        public void Try()
        {
          // var mock = new Mock>
          //
          // var SpeckleListenerService = new SpeckleListenerService()
        }
    }
}