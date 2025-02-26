using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<bool> SendEmailAsync(string recipientEmail, string subject, string body)
    {
        try
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            string host = smtpSettings["Host"];
            int port = int.Parse(smtpSettings["Port"]);
            string username = smtpSettings["Username"];
            string password = smtpSettings["Password"];
            bool enableSSL = bool.Parse(smtpSettings["EnableSSL"]);

            // Validate SMTP credentials
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("SMTP credentials are missing or invalid.");
                return false;
            }

            using (var smtpClient = new SmtpClient(host, port))
            {
                smtpClient.Credentials = new NetworkCredential(username, password);
                smtpClient.EnableSsl = enableSSL;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpClient.UseDefaultCredentials = false;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(username, "Voice OF Customer"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(recipientEmail);

                await smtpClient.SendMailAsync(mailMessage);
                Console.WriteLine($"✅ Email sent successfully to {recipientEmail}");
                return true;
            }
        }
        catch (SmtpException smtpEx)
        {
            Console.WriteLine($"❌ SMTP Error: {smtpEx.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ General Error: {ex.Message}");
            return false;
        }
    }
}
