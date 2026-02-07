using System.Text.RegularExpressions;

namespace ContosoShop.Server.Services;

/// <summary>
/// Logger wrapper that sanitizes Personally Identifiable Information (PII) from log messages.
/// Implements email masking and financial amount removal per Constitution v2.0.0 requirements.
/// </summary>
public partial class PiiSanitizingLogger : ILogger
{
    private readonly ILogger _innerLogger;

    [GeneratedRegex(@"(\w)\w+@", RegexOptions.Compiled)]
    private static partial Regex EmailMaskingRegex();

    [GeneratedRegex(@"\$\d+(\.\d{2})?", RegexOptions.Compiled)]
    private static partial Regex AmountRemovalRegex();

    public PiiSanitizingLogger(ILogger innerLogger)
    {
        _innerLogger = innerLogger ?? throw new ArgumentNullException(nameof(innerLogger));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _innerLogger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _innerLogger.IsEnabled(logLevel);
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var originalMessage = formatter(state, exception);
        var sanitizedMessage = SanitizeMessage(originalMessage);

        _innerLogger.Log(logLevel, eventId, sanitizedMessage, exception, (msg, ex) => msg);
    }

    /// <summary>
    /// Sanitizes a log message by masking emails and removing financial amounts.
    /// </summary>
    /// <param name="message">Original log message</param>
    /// <returns>Sanitized log message with PII removed</returns>
    private static string SanitizeMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return message;
        }

        // Mask email addresses: user@example.com -> u***@example.com
        var sanitized = EmailMaskingRegex().Replace(message, "$1***@");

        // Remove dollar amounts: $59.99 -> [AMOUNT]
        sanitized = AmountRemovalRegex().Replace(sanitized, "[AMOUNT]");

        return sanitized;
    }
}

/// <summary>
/// Logger provider that creates PiiSanitizingLogger instances.
/// </summary>
public class PiiSanitizingLoggerProvider : ILoggerProvider
{
    private readonly ILoggerFactory _innerLoggerFactory;

    public PiiSanitizingLoggerProvider(ILoggerFactory innerLoggerFactory)
    {
        _innerLoggerFactory = innerLoggerFactory ?? throw new ArgumentNullException(nameof(innerLoggerFactory));
    }

    public ILogger CreateLogger(string categoryName)
    {
        var innerLogger = _innerLoggerFactory.CreateLogger(categoryName);
        return new PiiSanitizingLogger(innerLogger);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
