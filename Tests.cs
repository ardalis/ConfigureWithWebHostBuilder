using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests
{
    public class Tests
    {
        private TestServer _server;
        private HttpClient _client;
        
        [Fact]
        public async void TestNoStartup() 
        {
            _server = new TestServer(
                new WebHostBuilder()
            .Configure(app => 
            {
                app.Run(async (context) =>
                {
                    await context.Response.WriteAsync("Hi");
                });
            }));
            _client = _server.CreateClient();

            var response = await _client.GetAsync("/");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Hi",result);
        }

        [Fact]
        public async void TestAddingServices() 
        {
            _server = new TestServer(
                new WebHostBuilder()
                .ConfigureServices(services => 
                {
                    services.AddSingleton<IGreeting,MorningGreeting>();
                })
                .UseStartup<StartupWithGreeting>()
            );
            _client = _server.CreateClient();

            var response = await _client.GetAsync("/");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Good morning, Steve!",result);
        }

        [Fact]
        public async void TestAddingLogging() 
        {
            _server = new TestServer(
                new WebHostBuilder()
                .ConfigureLogging(factory =>
                {
                    factory.AddConsole();
                })
                .UseStartup<StartupWithLogging>()
            );
            _client = _server.CreateClient();

            var response = await _client.GetAsync("/");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Logging",result);
        }

        [Fact]
        public async void TesMockingLogging() 
        {
            var mockLogger = new Mock<ILogger<StartupWithLogging>>();
            _server = new TestServer(
                new WebHostBuilder()
                .ConfigureLogging(factory =>
                {
                    factory.AddConsole();
                })
                .UseStartup<StartupWithLogging>()
            );
            _client = _server.CreateClient();

            var response = await _client.GetAsync("/");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Logging",result);
        }


    }

    public class StartupWithGreeting
    {
        public void Configure(IApplicationBuilder app, IGreeting greeting)
        {
            app.Run(async (context) =>
            {
                await context.Response.WriteAsync(greeting.Greet("Steve"));
            });
        } 
    }

        public class StartupWithLogging
    {
        public void Configure(IApplicationBuilder app,
           ILogger<StartupWithLogging> logger)
        {
            logger.LogWarning("Entering Configure");
            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Logging");
            });
        } 
    }


    public interface IGreeting
    {
        string Greet(string name);
    }

    public class MorningGreeting : IGreeting
    {
        public string Greet(string name)
        {
            return $"Good morning, {name}!";
        }
    }

}
