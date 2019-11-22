﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Orleans;
using Orleans.Hosting;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Zoo.Services;

namespace Zoo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var hostBuilder = new HostBuilder();
            hostBuilder.UseConsoleLifetime();
            hostBuilder.ConfigureLogging(logging => logging.AddConsole());
            hostBuilder.ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls("http://*:9000");
                webBuilder.UseStartup<Startup>();
            });

            hostBuilder.UseOrleans(b =>
            {
                b.UseLocalhostClustering();                
                b.ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(Services.Zoo).Assembly)
                    .WithReferences());            
            });

            var host = hostBuilder.Build();
            await host.StartAsync();

            Console.ReadLine();
        }
    }
}
