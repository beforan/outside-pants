﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nest;
using process.FileProcessors;
using process.Services;
using process.Types;

namespace process
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Create service provider, then immediately resolve our app and run it
            await ConfigureServices(new ServiceCollection())
                .GetService<App>().Run();
        }

        private static IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", false)
                .Build();

            // Add services
            services.AddSingleton(configuration);
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddConsole();
            });

            services.AddTransient<IRedisQueueService, RedisQueueService>();
            services.AddTransient<IElasticService, ElasticService>();
            services.AddScoped(s => new ElasticClient(
                new ConnectionSettings(
                    new Uri(configuration["es:host"]))
                    .DefaultIndex(configuration["es:index"])));

            // Add the App
            services.AddTransient<App>();

            // Autofac
            var autofac = new ContainerBuilder();

            //All the Http Clients!
            /*
                Notes on unusual configuration:
                    1. Autofac can fulfill dependencies on concrete types, not just interfaces
                    2. Autofac can have multiple components registered for a given type
                        - here we have more than one instance of HttpClient
                    3. You can name (or key) a given component, to refer to that specific one later
                        - here we key each HttpClient to an enum value,
                            so we can be sure to use the right one when fulfilling dependencies
                    4. Autofac can run a function when it activates a component.
                        - here we use this to initialise some of our HttpClient instances
            */
            autofac.RegisterType<HttpClient>()
                .Keyed<HttpClient>(HttpClients.RedisMessageQueue)
                .OnActivating(x =>
                {
                    var client = x.Instance;
                    client.BaseAddress = new Uri(configuration["rsmq:host"]);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                });
            //autofac.RegisterType<HttpClient>()
            //    .Keyed<HttpClient>(HttpClients.ElasticSearch)
            //    .OnActivating(x =>
            //    {
            //        var client = x.Instance;
            //        client.BaseAddress = new Uri(
            //            configuration["SubmissionStatus:Uri"]);
            //        client.DefaultRequestHeaders.Accept.Clear();
            //        client.DefaultRequestHeaders.Accept.Add(
            //            new MediaTypeWithQualityHeaderValue("application/json"));
            //    });

            // We can use the same keying technique above for FileProcessors
            // though right now we only process CSVs
            services.AddTransient<IFileProcessor, CsvProcessor>();

            autofac.Populate(services); //load the basic services into autofac's container
            return new AutofacServiceProvider(autofac.Build());
        }
    }
}
