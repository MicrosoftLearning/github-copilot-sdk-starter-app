namespace ContosoShop.Server.Services;

/// <summary>
/// Development implementation of email service that logs to console.
/// </summary>
public class EmailServiceDev : IEmailService
{
    private readonly ILogger<EmailServiceDev> _logger;

    public EmailServiceDev(ILogger<EmailServiceDev> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Simulates sending an email by logging to console.
    /// </summary>
    public Task SendEmailAsync(string to, string subject, string body)
    {
        _logger.LogInformation(
            "EMAIL: To={To}, Subject={Subject}, Body={Body}",
            to, subject, body);

        return Task.CompletedTask;
    }
}
