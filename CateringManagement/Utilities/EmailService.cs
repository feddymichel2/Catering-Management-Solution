using CateringManagement.ViewModels;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit.Text;
using MimeKit;
using MailKit.Net.Smtp;

namespace CateringManagement.Utilities
{
    /// <summary>
    /// Interface for my own email service
    /// </summary>
    public interface IMyEmailSender
    {
        Task SendOneAsync(string name, string email, string subject, string htmlMessage);
        Task SendToManyAsync(EmailMessage emailMessage);
    }

    /// <summary>
    /// This implements the IEmailService from
    /// Microsoft.AspNetCore.Identity.UI.Services for the Identity System
    /// </summary>
    public class EmailSender : IEmailSender
    {
        private readonly IEmailConfiguration _emailConfiguration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IEmailConfiguration emailConfiguration, ILogger<EmailSender> logger)
        {
            _emailConfiguration = emailConfiguration;
            _logger = logger;
        }

        /// <summary>
        /// Asynchronously builds and sends a message to a single recipient.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="subject"></param>
        /// <param name="htmlMessage"></param>
        /// <returns></returns>
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if(email.Contains("niagaracollege.ca"))
            {
                var message = new MimeMessage();
                message.To.Add(new MailboxAddress(email, email));
                message.From.Add(new MailboxAddress(_emailConfiguration.SmtpFromName, _emailConfiguration.SmtpUsername));

                message.Subject = subject;
                //We will say we are sending HTML. But there are options for plaintext etc. 
                message.Body = new TextPart(TextFormat.Html)
                {
                    Text = htmlMessage
                };
                try
                {
                    //Be careful that the SmtpClient class is the one from Mailkit not the framework!
                    using var emailClient = new SmtpClient();
                    //The last parameter here is to use SSL (Which you should!)
                    emailClient.Connect(_emailConfiguration.SmtpServer, _emailConfiguration.SmtpPort, false);

                    //Remove any OAuth functionality as we won't be using it. 
                    emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

                    emailClient.Authenticate(_emailConfiguration.SmtpUsername, _emailConfiguration.SmtpPassword);

                    await emailClient.SendAsync(message);

                    emailClient.Disconnect(true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.GetBaseException().Message);
                }
            }
        }
    }

    public class MyEmailSender : IMyEmailSender
    {
        private readonly IEmailConfiguration _emailConfiguration;

        public MyEmailSender(IEmailConfiguration emailConfiguration)
        {
            _emailConfiguration = emailConfiguration;
        }

        /// <summary>
        /// Asynchronously builds and sends a message to a single recipient
        /// </summary>        
        /// <param name="name">Optional - Uses the email if not supplied</param>
        /// <param name="email"></param>
        /// <param name="subject"></param>
        /// <param name="htmlMessage"></param>
        /// <returns></returns>
        public async Task SendOneAsync(string name, string email, string subject, string htmlMessage)
        {
            if (email.Contains("niagaracollege.ca"))
            {
                if (String.IsNullOrEmpty(name))
                {
                    name = email;
                }
                var message = new MimeMessage();
                message.To.Add(new MailboxAddress(name, email));
                message.From.Add(new MailboxAddress(_emailConfiguration.SmtpFromName, _emailConfiguration.SmtpUsername));

                message.Subject = subject;
                //We will say we are sending HTML. But there are options for plaintext etc. 
                message.Body = new TextPart(TextFormat.Html)
                {
                    Text = htmlMessage
                };

                //Be careful that the SmtpClient class is the one from Mailkit not the framework!
                using var emailClient = new SmtpClient();
                //The last parameter here is to use SSL (Which you should!)
                emailClient.Connect(_emailConfiguration.SmtpServer, _emailConfiguration.SmtpPort, false);

                //Remove any OAuth functionality as we won't be using it. 
                emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

                emailClient.Authenticate(_emailConfiguration.SmtpUsername, _emailConfiguration.SmtpPassword);

                await emailClient.SendAsync(message);

                emailClient.Disconnect(true);
            }   
        }

        /// <summary>
        /// Asynchronously sends a message to a List of email addresses
        /// </summary>
        /// <param name="emailMessage"></param>
        /// <returns></returns>
        public async Task SendToManyAsync(EmailMessage emailMessage)
        {
            var message = new MimeMessage();
            message.To.AddRange(emailMessage.ToAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
            message.From.Add(new MailboxAddress(_emailConfiguration.SmtpFromName, _emailConfiguration.SmtpUsername));

            message.Subject = emailMessage.Subject;
            //We will say we are sending HTML. But there are options for plaintext etc. 
            message.Body = new TextPart(TextFormat.Html)
            {
                Text = emailMessage.Content
            };

            //Be careful that the SmtpClient class is the one from Mailkit not the framework!
            using var emailClient = new SmtpClient();
            //The last parameter here is to use SSL (Which you should!)
            emailClient.Connect(_emailConfiguration.SmtpServer, _emailConfiguration.SmtpPort, false);

            //Remove any OAuth functionality as we won't be using it. 
            emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

            emailClient.Authenticate(_emailConfiguration.SmtpUsername, _emailConfiguration.SmtpPassword);

            await emailClient.SendAsync(message);

            emailClient.Disconnect(true);
        }
    }


}
