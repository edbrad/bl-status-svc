using System;
using System.Collections.Generic;
using FluentScheduler;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

public class TestJob : IJob
{
    private readonly object _lock = new object();
    private readonly ILogger<TestJob> _logger;
    private bool _shuttingDown;

    EmailService _email = new EmailService();

    public TestJob(ILogger<TestJob> logger)
    {
        // Initialize
        _logger = logger;
        
        

    }

    public void Execute()
    {
        try
        {
            lock (_lock)
            {
                if (_shuttingDown)
                    return;

                // Do work, son!
                Console.WriteLine("TestJob: Doing Work!");
                _logger.LogDebug("TestJob: Doing Work!");

                // Send Email Notification
                _logger.LogDebug("TestJob: Creating & Sending Email...");

                // - create new message instance
                EmailMessage emailMessage = new EmailMessage();

                // - compose message
                List<EmailAddress> toAddresses = new List<EmailAddress>();
                List<EmailAddress> fromAddresses = new List<EmailAddress>();
                toAddresses.Add(new EmailAddress(){Name = "Edward Bradley", Address = "edb@edbrad.com"});
                fromAddresses.Add(new EmailAddress(){Name = "Box Loading Superintendent Service", Address = "edbrad45@gmail.com"});
                
                emailMessage.ToAddresses = toAddresses;
                emailMessage.FromAddresses = fromAddresses;

                emailMessage.Subject = "TestJob Notification";
                emailMessage.Content = "<h1>TEST JOB<h1><p>This is a Test Job<p>";

                // - send composed message - via Email Service
                _email.Send(emailMessage);
                _logger.LogDebug("TestJob: Email Sent!");
            }
        }
        finally
        {

        }
    }

    public void Stop(bool immediate)
    {
        // Locking here will wait for the lock in Execute to be 
        // released until this code can continue.
        lock (_lock)
        {
            _shuttingDown = true;
        }

    }
}