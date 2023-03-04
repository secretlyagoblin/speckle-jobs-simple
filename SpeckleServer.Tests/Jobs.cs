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
    public class Jobs
    {
        private WebApplicationFactory<Program> _application;
        private HttpClient _client;
        private string _commandName;
        private string _speckleStream;
        private string _command;

        [SetUp]
        public void Setup()
        {
            _application = new WebApplicationFactory<Program>();
            _client = _application.CreateClient();
            _commandName = "abcdef";
            _speckleStream = "111xxx111";
            _command = $"/commands/{_commandName}";

            _client.PutAsJsonAsync(_command, new Command("sadsad"));
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
            _application.Dispose();
        }


        [Test]
        public async Task ValidJobCreatedSucceeds()
        {
            var targetPath = "123123/branches/*/beef";
            var resultPath = "123123/branches/*/swede";

            var jobResponse = await _client.PutAsJsonAsync($"jobs/new", new NewJobScema(targetPath, resultPath, _commandName));

            Assert.That(jobResponse.IsSuccessStatusCode);
        }

        [Test]
        public async Task TwoIdenticalJobsFail()
        {
            //should really make each of these a new database  
            var targetPath = "123123/branches/*/brof";
            var resultPath = "123123/branches/*/sweeb";

            var jobResponse = await _client.PutAsJsonAsync($"jobs/new", new NewJobScema(targetPath, resultPath, _commandName));

            Assert.That(jobResponse.IsSuccessStatusCode);

            jobResponse = await _client.PutAsJsonAsync($"jobs/new", new NewJobScema(targetPath, resultPath, _commandName));

            Assert.That(!jobResponse.IsSuccessStatusCode);
        }

        [Test]
        public async Task JobThatMightCauseARecursiveLoopFail()
        {
            //should really 
            var targetPath = "123123/branches/*/norb";
            var resultPath = targetPath;

            var jobResponse = await _client.PutAsJsonAsync($"jobs/new", new NewJobScema(targetPath, resultPath, _commandName));

            Assert.That(!jobResponse.IsSuccessStatusCode);
        }


    }
}