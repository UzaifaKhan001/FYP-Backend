using FYP.Models;
using System.Net.Mail;
using System.Net;

public class EmailService
{
    private readonly SmtpSettings _smtpSettings;

    public EmailService(IConfiguration configuration)
    {
        _smtpSettings = configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
    }

    public async Task<bool> SendEmailAsync(string recipient, string subject, string body)
    {
        try
        {
            var fromAddress = new MailAddress(_smtpSettings.Username, "Uzaifa Khan");
            var toAddress = new MailAddress(recipient);

            using (var smtp = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port))
            {
                smtp.EnableSsl = _smtpSettings.EnableSSL;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(fromAddress.Address, _smtpSettings.Password);

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    await smtp.SendMailAsync(message);
                    Console.WriteLine("✅ Email sent successfully!");
                    return true; // Indicate success
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            return false; // Indicate failure
        }
    }
}
