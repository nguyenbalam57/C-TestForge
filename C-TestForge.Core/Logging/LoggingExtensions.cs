using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using MsLogger = Microsoft.Extensions.Logging.ILogger; // Alias cho Microsoft.Extensions.Logging.ILogger
using SerilogLogger = Serilog.ILogger; // Alias cho Serilog.ILogger

namespace C_TestForge.Core.Logging
{
    /// <summary>
    /// Extension methods for configuring logging in the application
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Configures Serilog for the application and registers loggers with the container
        /// </summary>
        /// <param name="containerRegistry">The container registry to register loggers with</param>
        public static void AddLogging(this IContainerRegistry containerRegistry)
        {
            // Setup Serilog
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "C-TestForge", "Logs", "log-.txt");

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));

            // Configure Serilog
            Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .Enrich.WithEnvironmentUserName()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 10 * 1024 * 1024,
                    retainedFileCountLimit: 10,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{ThreadId}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            // Register generic ILogger factory
            containerRegistry.RegisterInstance<MsLogger>(new SerilogLoggerAdapter());

            // Đăng ký Logger<T> làm triển khai của ILogger<T>
            // Lưu ý: Prism.Unity không có phương thức RegisterGeneric nên ta cần đăng ký các kiểu cụ thể
        }
    }
}