using System.Net;
using MimeKit;
using MailKit.Net.Smtp;

namespace SafeMind.Services
{
    public class EmailSender
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;

        public EmailSender(IConfiguration configuration)
        {
            _smtpServer = configuration.GetValue<string>("SmtpSettings:Host", "");
            _smtpPort = configuration.GetValue<int>("SmtpSettings:Port", 0);
            _smtpUsername = configuration.GetValue<string>("SmtpSettings:Username", "");
            _smtpPassword = configuration.GetValue<string>("SmtpSettings:Password", "");
        }
        public void SendEmail(string senderName, string senderEmail, string toName,
        string toEmail, string subject, string textContent)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            message.Body = new TextPart("plain")
            {
                Text = textContent
            };
            try
            {
                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    // Disable certificate revocation check (for development only)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    client.Connect(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                    client.Authenticate(_smtpUsername, _smtpPassword);
                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email send failed: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

    }


}