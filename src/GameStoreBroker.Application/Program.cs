﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Extensions;
using GameStoreBroker.Application.Operations;
using GameStoreBroker.Application.Services;
using GameStoreBroker.ClientApi;
using GameStoreBroker.FileLogger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application
{
    internal static class Program
    {
        private const string LogTimestampFormat = "yyyy-MM-dd hh:mm:ss.fff ";

        // Options
        private static readonly Option<bool> VerboseOption = new (new[] { "-v", "--Verbose" }, "Log verbose messages such as http calls.");
        private static readonly Option<FileInfo> LogFileOption = new(new[] { "-l", "--LogFile" }, "The location of the log file.");
        private static readonly Option<string> ClientSecretOption = new (new[] { "-s", "--ClientSecret" }, "The client secret of the AAD app.");
        private static readonly Option<FileInfo> ConfigFileOption = new (new[] { "-c", "--ConfigFile" }, "The location of the json config file.")
        {
            IsRequired = true,
        };

        private static async Task<int> Main(string[] args)
        {
            return await BuildCommandLine()
                .UseHost(hostBuilder => hostBuilder
                    .ConfigureLogging(ConfigureLogging)
                    .ConfigureServices(ConfigureServices)
                    .ConfigureAppConfiguration((context, builder) => ConfigureAppConfiguration(context, builder, args))
                )
                .UseDefaults()
                .Build()
                .InvokeAsync(args)
                .ConfigureAwait(false);
        }

        private static void ConfigureLogging(HostBuilderContext context, ILoggingBuilder logging)
        {
            var invocationContext = context.GetInvocationContext();
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Warning);
            logging.AddFilter("GameStoreBroker", invocationContext.GetOptionValue(VerboseOption) ? LogLevel.Trace : LogLevel.Information);
            logging.AddSimpleFile(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = LogTimestampFormat;
            }, file =>
            {
                var logFile = invocationContext.GetOptionValue(LogFileOption);
                file.Path = logFile?.FullName ?? Path.Combine(Path.GetTempPath(), $"GameStoreBroker_{DateTime.Now:yyyyMMddhhmmss}.log");
                file.Append = true;
            });
            logging.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = LogTimestampFormat;
            });
        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddLogging();
            services.AddScoped<IProductService, ProductService>();
            services.AddGameStoreBrokerService(context.Configuration);
        }

        private static void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder builder, string[] args)
        {
            var invocationContext = context.GetInvocationContext();
            var configFilePath = invocationContext.GetOptionValue(ConfigFileOption);
            if (configFilePath is not null)
            {
                builder.AddJsonFile(configFilePath.FullName, false, false);
            }

            var switchMappings = ClientSecretOption.Aliases.ToDictionary(s => s, _ => "aadAuthInfo:clientSecret");
            builder.AddCommandLine(args, switchMappings);
        }

        private static CommandLineBuilder BuildCommandLine()
        {
            var rootCommand = new RootCommand
            {
                new Command("GetProduct", "Gets metadata of the product.")
                {
                    ConfigFileOption,
                    ClientSecretOption,
                }.AddHandler(CommandHandler.Create<IHost, Options, CancellationToken>(GetProductAsync)),
                new Command("UploadUwpPackage", "Gets metadata of the product.")
                {
                    ConfigFileOption,
                    ClientSecretOption,
                }.AddHandler(CommandHandler.Create<IHost, Options, CancellationToken>(UploadUwpPackageAsync)),
            };
            rootCommand.AddGlobalOption(VerboseOption);
            rootCommand.AddGlobalOption(LogFileOption);
            rootCommand.Description = "Application that enables game developers to upload Xbox and PC game packages to Partner Center.";
            return new CommandLineBuilder(rootCommand);
        }

        private static async Task<int> GetProductAsync(IHost host, Options options, CancellationToken ct) => 
            await new GetProductOperation(host, options).RunAsync(ct).ConfigureAwait(false);

        private static async Task<int> UploadUwpPackageAsync(IHost host, Options options, CancellationToken ct) =>
            await new UploadUwpPackageOperation(host, options).RunAsync(ct).ConfigureAwait(false);
    }
}
