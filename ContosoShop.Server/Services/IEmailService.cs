namespace ContosoShop.Server.Services;

/// <summary>
/// Interface for email sending operations.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email asynchronously.
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body content</param>
    Task SendEmailAsync(string to, string subject, string body);
}
