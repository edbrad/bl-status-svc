using System;
using System.Collections.Generic;
using System.Linq;
using MailKit;
using MailKit.Net.Smtp;
using MimeKit;

/// <summary>
/// A Service to Send Email(s) via SMTP
/// </summary>
public class EmailService : IEmailService
{
    /// <summary>
    /// Constructor
    /// </summary>
    public EmailService()
	{
		
	}
    /// <summary>
    /// Construct and Send an Email Message via a SMTP Server
    /// </summary>
    /// <param name="emailMessage"></param>
    /// <param name="attachmentFilePath"></param>
    public void Send(EmailMessage emailMessage, String attachmentFilePath = "")
    {
        var message = new MimeMessage();
        message.To.AddRange(emailMessage.ToAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
        message.From.AddRange(emailMessage.FromAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));

        message.Subject = emailMessage.Subject;

        var builder = new BodyBuilder();
        builder.TextBody = emailMessage.Content;
        if(!String.IsNullOrEmpty(attachmentFilePath))
        {
            builder.Attachments.Add(attachmentFilePath);
        }
        

        message.Body = builder.ToMessageBody();
        
        using (var emailClient = new SmtpClient())
        {
            // Connect to the Server
            emailClient.Connect("smtp.gmail.com", 587);

            // Remove any OAuth functionality as we won't be using it. 
            emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

            emailClient.Authenticate("edbrad45@gmail.com", "eddie123a");

            emailClient.Send(message);

            emailClient.Disconnect(true);
        }

    }

    public List<EmailMessage> ReceiveEmail(int maxCount = 10)
    {
        throw new NotImplementedException();
    }
}