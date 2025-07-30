using Microsoft.Extensions.Logging;
using System;
using MsLogger = Microsoft.Extensions.Logging.ILogger; // Alias cho Microsoft.Extensions.Logging.ILogger

namespace C_TestForge.Core.Logging
{
    /// <summary>
    /// Generic logger implementation that wraps a non-generic ILogger.
    /// This class enables dependency injection of ILogger&lt;T&gt; in classes that need it.
    /// </summary>
    /// <typeparam name="T">The type requesting the logger</typeparam>
    public class Logger<T> : ILogger<T>
    {
        private readonly MsLogger _logger;

        /// <summary>
        /// Constructor for Logger&lt;T&gt;
        /// </summary>
        /// <param name="logger">The non-generic logger instance to wrap</param>
        public Logger(MsLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Begins a logical operation scope
        /// </summary>
        /// <typeparam name="TState">The type of the state to begin scope for</typeparam>
        /// <param name="state">The identifier for the scope</param>
        /// <returns>An IDisposable that ends the logical operation scope on dispose</returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            return _logger.BeginScope(state);
        }

        /// <summary>
        /// Checks if the given logLevel is enabled
        /// </summary>
        /// <param name="logLevel">Level to be checked</param>
        /// <returns>true if enabled</returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }

        /// <summary>
        /// Writes a log entry
        /// </summary>
        /// <typeparam name="TState">The type of the object to be written</typeparam>
        /// <param name="logLevel">Entry will be written on this level</param>
        /// <param name="eventId">Id of the event</param>
        /// <param name="state">The entry to be written. Can be also an object</param>
        /// <param name="exception">The exception related to this entry</param>
        /// <param name="formatter">Function to create a string message of the state and exception</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }

    /// <summary>
    /// Factory for creating Logger instances with custom configuration
    /// </summary>
    public static class LoggerFactory
    {
        /// <summary>
        /// Creates a new ILogger instance using Serilog
        /// </summary>
        /// <returns>A configured logger instance</returns>
        public static MsLogger CreateSerilogLogger()
        {
            // Configure Serilog here
            return new SerilogLoggerAdapter();
        }
    }

    /// <summary>
    /// Adapter for Serilog to implement ILogger interface
    /// </summary>
    internal class SerilogLoggerAdapter : MsLogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            // Create a scope for Serilog if needed
            return new NoOpDisposable();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            // Check if the log level is enabled in Serilog
            return true; // Default to true for this implementation
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            string message = formatter(state, exception);

            // Log to Serilog based on log level
            switch (logLevel)
            {
                case LogLevel.Trace:
                    Serilog.Log.Verbose(exception, message);
                    break;
                case LogLevel.Debug:
                    Serilog.Log.Debug(exception, message);
                    break;
                case LogLevel.Information:
                    Serilog.Log.Information(exception, message);
                    break;
                case LogLevel.Warning:
                    Serilog.Log.Warning(exception, message);
                    break;
                case LogLevel.Error:
                    Serilog.Log.Error(exception, message);
                    break;
                case LogLevel.Critical:
                    Serilog.Log.Fatal(exception, message);
                    break;
                default:
                    Serilog.Log.Information(exception, message);
                    break;
            }
        }

        /// <summary>
        /// No-operation disposable for use when no scope is needed
        /// </summary>
        private class NoOpDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}