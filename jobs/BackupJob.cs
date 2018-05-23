using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using FluentScheduler;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

/// <summary>
/// BACKUP JOB: Scheduler Job to backup the 
/// bl-status system database (MongoDB)
/// </summary>
public class BackupJob : IJob
{
    // Define Local Variables
    private readonly object _lock = new object(); /* Task/Job Locker */
    private readonly ILogger<BackupJob> _logger; /* system logger (singleton injection) */
    private bool _shuttingDown; /* scheduler showtdown flag */
    private string _bkupOutput; /* captures mongodump results */
    EmailService _email = new EmailService(); /* system Emailer instance */

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger"></param>
    public BackupJob(ILogger<BackupJob> logger)
    {
        // Initialize
        _logger = logger; /* get injected Logger singleton */
        _bkupOutput = "";
    }

    /// <summary>
    /// Execute Job Task(s)
    /// </summary>
    /// <returns></returns>
    public void Execute()
    {
        try
        {
            lock (_lock)
            {
                if (_shuttingDown)
                    return;

                // Run Shell Command (backup MongoDB Database)
                _logger.LogDebug("BackupJob: Run Shell Command: Backup MongoDB data...");
                _bkupOutput = Shell("sh data/db-backup/mongobackup.sh &> data/db-backup/bl-status-DBbackup.txt");
                _logger.LogDebug("BackupJob: Shell Command: Complete!");

                // Send Email Notification
                _logger.LogDebug("BackupJob: Creating & Sending Email...");

                /*
                * create new message instance
                */
                EmailMessage emailMessage = new EmailMessage();

                /*
                * compose message
                */
                List<EmailAddress> toAddresses = new List<EmailAddress>();
                toAddresses.Add(new EmailAddress()
                {
                    Name = "Edward Bradley",
                    Address = "edb@emsmail.com"
                });

                List<EmailAddress> fromAddresses = new List<EmailAddress>();
                fromAddresses.Add(new EmailAddress()
                {
                    Name = "Box Loading Status - Superintendent Service",
                    Address = "bl-status-svc@emsmail.com"
                });

                emailMessage.ToAddresses = toAddresses;
                emailMessage.FromAddresses = fromAddresses;

                emailMessage.Subject = "Backup Job Notification";

                /*
                * read the captured output and add it to Email body
                */
                string logPath = @"data/db-backup/bl-status-DBbackup.txt";
                _bkupOutput = "";
                if (File.Exists(logPath))
                {
                    string[] readText = File.ReadAllLines(logPath);
                    foreach (string s in readText)
                    {
                        _bkupOutput = _bkupOutput + s + "\n";
                    }
                }
                else
                {
                    _bkupOutput = "--ERROR-- Missing Log File";
                    _logger.LogError("bl-status-svc: Missing mongodump Log File (data/db-backup/bl-status-DBbackup.txt)");
                }
                emailMessage.Content = "bl-status-svc - BACKUP JOB RESULTS:\n\n" + _bkupOutput;

                /*
                * set attachment path
                */
                string attachmentPath = "";

                /*
                * send composed message - via Email Service
                */
                _email.Send(emailMessage, attachmentPath);
                _logger.LogDebug("BackupJob: Email Sent!");
            }
        }
        finally
        {

        }
    }

    /// <summary>
    /// Post Stop/Shutdown Code
    /// </summary>
    /// <param name="immediate"></param>
    public void Stop(bool immediate)
    {
        /* 
        * Locking here will wait for the lock in Execute to be 
        * released until this code can continue.
        */
        lock (_lock)
        {
            _shuttingDown = true;
        }
    }

    /// <summary>
    /// Execute Shell Command (Process)
    /// </summary>
    /// <param name="cmd">Shell Command to Execute a local Shell Comand</param>
    /// <returns>A string containing the STDOUT result</returns>
    static string Shell(string cmd)
    {
        var escapedArgs = cmd.Replace("\"", "\\\"");

        var process = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{escapedArgs}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        process.Start();
        string result = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return result;
    }
}