using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
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
        public async void TestAddingLoggingViaMethod() 
        {
            _server = new TestServer(
                new WebHostBuilder()
                .ConfigureLogging(factory =>
                {
                    factory.AddConsole();
                })
                .UseStartup<StartupWithLoggingPassedIntoMethod>()
            );
            _client = _server.CreateClient();

            var response = await _client.GetAsync("/");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Logging via method injection",result);
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
        private readonly ILogger<StartupWithLogging> _logger;
        public StartupWithLogging(ILogger<StartupWithLogging> logger)
        {
        _logger = logger;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            _logger.LogInformation("Starting ConfigureServices");
            // do stuff here
            _logger.LogInformation("Exiting ConfigureServices");        
        }
        public void Configure(IApplicationBuilder app)
        {
            _logger.LogWarning("Entering Configure");
            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Logging");
            });
        } 
    }

    public class StartupWithLoggingPassedIntoMethod
    {
            public void Configure(IApplicationBuilder app, ILogger<StartupWithLoggingPassedIntoMethod> logger)
        {
            logger.LogWarning("Entering Configure");
            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Logging via method injection");
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
