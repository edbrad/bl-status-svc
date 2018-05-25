using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using MailKit;
using MailKit.Net.Smtp;
using MimeKit;

/// <summary>
/// A Service to Send Email(s) via SMTP
/// </summary>
public class EmailService : IEmailService
{
    public static IConfiguration Configuration { get; set; } /* Application Configuration Object */

    /// <summary>
    /// Constructor
    /// </summary>
    public EmailService()
    {
        // Get App Configuration (from external config file)
        var appConfigBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");

        Configuration = appConfigBuilder.Build();
    }

    /// <summary>
    /// Construct and Send an Email Message via a SMTP Server
    /// </summary>
    /// <param name="emailMessage"></param>
    /// <param name="attachmentFilePath"></param>
    public void Send(EmailMessage emailMessage, String attachmentFilePath = "")
    {
        // Compose Message
        /* new MIMEKit message instance */
        var message = new MimeMessage();

        /* apply sender and recipient */
        message.To.AddRange(emailMessage.ToAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
        message.From.AddRange(emailMessage.FromAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));

        /* add subject */
        message.Subject = emailMessage.Subject;

        /* add body */
        var builder = new BodyBuilder();
        builder.TextBody = emailMessage.Content;
        
        /* append attachment to body (if specified) */
        if (!String.IsNullOrEmpty(attachmentFilePath))
        {
            builder.Attachments.Add(attachmentFilePath);
        }

        /* complete message body (build) */
        message.Body = builder.ToMessageBody();

        // Connect to Server and Send the Email
        using (var emailClient = new SmtpClient())
        {
            /* Connect to the Server */
            emailClient.Connect(Configuration["EmailConfiguration:SmtpServer"], Int32.Parse(Configuration["EmailConfiguration:SmtpPort"]));

            /* Remove any OAuth functionality as we won't be using it. */
            emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

            /* authenticate w/ Mail Server */
            emailClient.Authenticate(Configuration["EmailConfiguration:SMTPUsername"], Configuration["EmailConfiguration:SMTPPassword"]);

            /* SEND MESSAGE! */
            emailClient.Send(message);

            /* disconnect from Server */
            emailClient.Disconnect(true);
        }

    }

    public List<EmailMessage> ReceiveEmail(int maxCount = 10)
    {
        throw new NotImplementedException();
    }
}