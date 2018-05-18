using System;
using System.Collections.Generic;
using System.Linq;
using MailKit;
using MailKit.Net.Smtp;
using MimeKit;

public class EmailService : IEmailService
{
    public EmailService()
	{
		
	}
    public void Send(EmailMessage emailMessage)
    {
        var message = new MimeMessage();
        message.To.AddRange(emailMessage.ToAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
        message.From.AddRange(emailMessage.FromAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));

        message.Subject = emailMessage.Subject;

        message.Body = new TextPart("html")
        {
            Text = emailMessage.Content
        };

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