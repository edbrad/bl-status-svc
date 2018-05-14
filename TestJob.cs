using System;
using FluentScheduler;

public class TestJob : IJob
{
    private readonly object _lock = new object();

    private bool _shuttingDown;

    public TestJob()
    {
        // Initialize

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