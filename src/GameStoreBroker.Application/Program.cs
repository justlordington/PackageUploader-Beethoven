﻿using System;
using GameStoreBroker.ClientApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.Extensions.Logging.File;

namespace GameStoreBroker.Application
{
    internal static class Program
    {
        private const string LogTimestampFormat = "yyyy-MM-dd hh:mm:ss.fff ";

        private static async Task<int> Main(string[] args)
        {
            return await BuildCommandLine()
                .UseHost(hostBuilder => hostBuilder
                    .ConfigureLogging((_, logging) =>
                    {
                        logging.ClearProviders();
                        logging.SetMinimumLevel(LogLevel.Warning);
                        logging.AddFilter("GameStoreBroker", args.Contains("-v") || args.Contains("--Verbose") ? LogLevel.Trace : LogLevel.Information);
                        logging.AddSimpleConsole(options =>
                        {
                            options.IncludeScopes = true;
                            options.SingleLine = true;
                            options.TimestampFormat = LogTimestampFormat;
                        });
                        logging.AddFile(options =>
                        {
                            options.TextBuilder = new CustomFileLogEntryTextBuilder
                            {
                                TimestampFormat = LogTimestampFormat,
                            };
                            options.RootPath = Path.GetTempPath(); //AppContext.BaseDirectory;
                            options.Files = new [] { new LogFileOptions
                            {
                                Path = $"GameStoreBroker_{DateTime.Now:yyyyMMddhhmmss}.log",
                            }};
                        });
                    })
                    .ConfigureServices((_, services) =>
                    {
                        services.AddLogging();
                        services.AddGameStoreBrokerService();
                    }))
                .UseDefaults()
                .Build()
                .InvokeAsync(args)
                .ConfigureAwait(false);
        }

        private static CommandLineBuilder BuildCommandLine()
        {
            // Options
            var configFile = new Option<string>(new[] {"-c", "--ConfigFile"}, "The location of json config file");
            var clientSecret = new Option<string>(new[] {"-s", "--ClientSecret"}, "The client secret of the AAD app.");
            var verbose = new Option<bool>(new[] {"-v", "--Verbose"}, "Log verbose messages such as http calls.");

            // Root Command
            var rootCommand = new RootCommand
            {
                verbose,
                new Command("GetProduct", "Gets metadata of the product.")
                {
                    configFile, clientSecret, verbose,
                }.AddHandler(CommandHandler.Create<IHost, Options, CancellationToken>(GetProduct)),
                //new Command("UploadPcPackage", "Uploads a msix, appaxupload, package to a product.")
                //{
                //    configFile, clientSecret, verbose,
                //}.AddHandler(CommandHandler.Create<IHost, Options, CancellationToken>(UploadPcPackage)),
                //new Command("UploadXboxPackage", "Uploads a msixxvc, xvc and related assets to a product.")
                //{
                //    configFile, clientSecret, verbose,
                //}.AddHandler(CommandHandler.Create<IHost, Options, CancellationToken>(UploadXboxPackage)),
            };

            rootCommand.Description = "GameStoreBroker description.";
            return new CommandLineBuilder(rootCommand);
        }

        private static async Task<int> GetProduct(IHost host, Options options, CancellationToken ct) => 
            await new Commands.GetProduct(host, options).Run(ct).ConfigureAwait(false);

        private static async Task<int> UploadPcPackage(IHost host, Options options, CancellationToken ct) =>
            await new Commands.UploadPcPackage(host, options).Run(ct).ConfigureAwait(false);

        private static async Task<int> UploadXboxPackage(IHost host, Options options, CancellationToken ct) =>
            await new Commands.UploadXboxPackage(host, options).Run(ct).ConfigureAwait(false);
    }
}
