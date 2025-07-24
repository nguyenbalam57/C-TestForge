using System;
using System.IO;
using C_TestForge.Core.Services;
using C_TestForge.Parser;
using C_TestForge.TestCase.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace C_TestForge.Core
{
    public class AppBootstrapper
    {
        private static ServiceProvider _serviceProvider;

        public static void ConfigureServices()
        {
            // Setup Serilog
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "C-TestForge", "Logs", "log-.txt");

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 10 * 1024 * 1024,
                    retainedFileCountLimit: 10,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            // Create service collection
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(config =>
            {
                config.ClearProviders();
                config.AddSerilog(dispose: true);
            });

            // Register services
            services.AddSingleton<IParser, ClangSharpParserService>();
            services.AddSingleton<IProjectService, ProjectService>();
            services.AddSingleton<ITestCaseService, TestCaseService>();

            // Build service provider
            _serviceProvider = services.BuildServiceProvider();
        }

        public static T GetService<T>() where T : class
        {
            if (_serviceProvider == null)
            {
                ConfigureServices();
            }

            return _serviceProvider.GetRequiredService<T>();
        }

        public static void Shutdown()
        {
            // Dispose the service provider
            _serviceProvider?.Dispose();

            // Close and flush the log
            Log.CloseAndFlush();
        }
    }
}
