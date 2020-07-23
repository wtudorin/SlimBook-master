using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace SlimBook.Utility
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailOptions emailOptions;

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var result = Execute(emailOptions.SendGridKey, subject, htmlMessage, email);
            return result;
        }

        public EmailSender(IOptions<EmailOptions> options)
        {
            emailOptions = options.Value;
        }

        private Task Execute(string sendGridKey, string subject, string message, string email)
        {
            //var apiKey = Environment.GetEnvironmentVariable("NAME_OF_THE_ENVIRONMENT_VARIABLE_FOR_YOUR_SENDGRID_KEY");
            var client = new SendGridClient(sendGridKey);
            var from = new EmailAddress("wtudorin@gmail.com", "Slim Books");
            //var subject = "Sending with SendGrid is Fun";
            var to = new EmailAddress(email, "Walter The Best");
            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", message);
            return client.SendEmailAsync(msg);
        }
    }
}