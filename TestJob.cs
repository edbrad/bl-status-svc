using System;
using FluentScheduler;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

public class TestJob : IJob
{
    private readonly object _lock = new object();
    private readonly ILogger<TestJob> _logger;
    private bool _shuttingDown;

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
                Console.WriteLine("Test Job Doing Work!");
                _logger.LogDebug("Logger: Test Job Doing Work!");
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