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
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using NUnit.Framework;
using System.Net.Http;
using System.Runtime;

namespace SpeckleSync.Tests
{
    public class Speckle
    {


        private readonly List<string> _testDirectories = new List<string>();
        private readonly List<string> _streams = new List<string>();

        private CancellationTokenSource _hostCancellationToken;

        [SetUp]
        public void Setup()
        {
            _hostCancellationToken = new();
        }

        [TearDown]
        public void TearDown()
        {
            using var client = CreateSpeckleClient();

            _streams.ForEach(x => client.StreamDelete(x).Wait());
            _streams.Clear();
            _hostCancellationToken.Cancel();

            _testDirectories.ForEach(x => Directory.Delete(x, true));
        }

        private Client CreateSpeckleClient()
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

        private static IConfigurationRoot GetConfigurationRoot(string settings = "appsettings.SpeckleTest.json") => new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(settings, true, true)
            .Build();        

        private IHostBuilder ConfigureSpeckleSync(HttpClient client)
        {
            var builder = new HostBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Debug);
            }).ConfigureServices((context, services) =>
            {
                services.AddHttpClient(nameof(SpeckleServer), x => x.BaseAddress = client.BaseAddress);
                services.AddSingleton<IConfiguration>(GetConfigurationRoot());
                services.AddSingleton<IHostedService, DirectoryListenerService>();
            });

            return builder;
        }

        [Test]
        public async Task EventTriggers()
        {
            var appsettingsPath = "appsettings.SpeckleTest.json";
            var appsettings = GetConfigurationRoot(appsettingsPath) ?? throw new Exception("Could not get settings");

            //setup SpeckleXYZ client
            Client speckleXYZClient = CreateSpeckleClient();

            var baseUrl = appsettings.GetValue<string>("SpeckleHttpService") ?? throw new Exception();

            //setup our Speckle server client (needs a better name)
            await using var daisy = new WebApplicationFactory<SpeckleServer.Program>()
                    .WithWebHostBuilder(builder =>
                    {
                        builder.UseUrls(baseUrl);

                        //not sure if this is needed???
                        builder.ConfigureServices(x =>
                        {
                            x.AddCors(o =>
                            {
                                o.AddPolicy("TestingPolicy", p =>
                                {
                                    p.AllowAnyOrigin();
                                    p.AllowAnyMethod();
                                    p.AllowAnyHeader();
                                });
                            });
                        });

                        builder.ConfigureLogging(logging =>
                        {
                            logging.ClearProviders();
                            logging.AddConsole();
                            logging.SetMinimumLevel(LogLevel.Debug);
                        });
                    });

            using HttpClient daisyClient = daisy.CreateClient(new WebApplicationFactoryClientOptions()
            {
                BaseAddress = new Uri(baseUrl)
            });

            await daisyClient.GetAsync("/")
                .ContinueWith(x =>
                {
                     if (x.Result.StatusCode == System.Net.HttpStatusCode.OK) return;
                     throw new Exception(x.Result.StatusCode.ToString());
                });

            //setup app
            IHost host = ConfigureSpeckleSync(daisyClient).Build();
            Task runningHost = host.StartAsync(_hostCancellationToken.Token);




            string testStreamId;

            // Step 1, setup the test stream
            {
                testStreamId = await speckleXYZClient.StreamCreate(new StreamCreateInput()
                {
                    name = $"TEST{Random.Shared.Next()}",
                    description = "Created as part of a testing process, this should automatically get wiped",
                    isPublic = false
                });

                _streams.Add(testStreamId); //ensures it gets wiped after test
            }

            string commandName = "Blart";

            // Step 2, add a Gh file to Sync folder
            {
                var fullPath = Path.GetFullPath(appsettings.GetValue<string>("TargetDirectory") ?? throw new Exception());
                _testDirectories.Add(fullPath);

                File.WriteAllText($"{fullPath}/{commandName}.gh", "This is a debug file and will fail");

                await Task.Delay(TimeSpan.FromSeconds(10));
            }

            //Step 3, configure Daisy Job
            {
                var targetPath = $"{speckleXYZClient.ServerUrl}/streams/{testStreamId}/branches/main";
                var destinationPath = $"{speckleXYZClient.ServerUrl}/streams/{testStreamId}/branches/notMain";


                var jobResponse = await daisyClient.PutAsJsonAsync($"/jobs/new", new NewJobScema(targetPath, destinationPath, "Blart"));

                Assert.That(jobResponse.IsSuccessStatusCode);
            }

            // Step 4, send the test data to Speckle
            {
                var transport = new ServerTransport(speckleXYZClient.Account, testStreamId);

                var dummyData = new Base();
                dummyData["dummyInt"] = 42;
                dummyData["dummyChild"] = new Base();
                ((Base)dummyData["dummyChild"])["dummyName"] = "NamedChild";

                var objectId = await Operations.Send(dummyData, new List<ITransport> { transport }, disposeTransports: true);

                var commitCreated = await speckleXYZClient.CommitCreate(new CommitCreateInput()
                {
                    streamId = testStreamId,
                    branchName = "main",
                    message = "Test commit",
                    objectId = objectId
                });
            }

            // Step 5, confirm that the job has run
            {
                var getResponse = await daisyClient.GetAsync("results").Result.Content.ReadFromJsonAsync<JsonElement>();

                var strResponse = getResponse.GetRawText();

                Assert.That(getResponse.ValueKind, Is.EqualTo(JsonValueKind.Array));
                Assert.That(getResponse.GetArrayLength, Is.EqualTo(1));
            }
        }
    }
}
