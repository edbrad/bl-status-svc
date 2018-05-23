using System;
using System.IO;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using FluentScheduler;

namespace bl_status_svc
{
    class Program
    {
        /// <summary>
        /// BL-STATUS-SVC: Box Loading Status Superintendent Service
        /// Runs scheduled system maintenance/management/reporting Jobs in the background 
        /// </summary>
        /// <param name="args">Command Line Arguments</param>
        static void Main(string[] args)
        {
            // Initialize Logger
            var logger = NLog.LogManager.LoadConfiguration("nlog.config").GetCurrentClassLogger();
            logger.Debug("Logging started!");

            // Add Event Handlers
            AssemblyLoadContext.Default.Unloading += SigTermEventHandler;
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelHandler);
            JobManager.JobException += info => logger.Fatal("bl-status-svc: An error just happened with a scheduled job: " + info.Exception);

            // Inject Logger Into Scheduler Job Classes
            var servicesProvider = BuildDi(); /* initialize provider if injectable Services */
            //var testJob = servicesProvider.GetRequiredService<TestJob>();
            var backupJob = servicesProvider.GetRequiredService<BackupJob>();

            // Initialize Job Manager
            var jobRegistry = new Registry();

            /* **JOBS ARE SCHEDULED HERE** */
            //jobRegistry.Schedule(testJob).ToRunNow().AndEvery(60).Seconds();  /* Test Job */
            jobRegistry.Schedule(backupJob).ToRunNow().AndEvery(60).Seconds();  /* Backup Job */
            
            /* load all scheduled Jobs */
            JobManager.Initialize(jobRegistry);

            while (true)
            {
                // RUN UNTIL SHUT DOWN OR CANCELED
            }
        }

        /// <summary>
        /// Service Shutdown Event Handler
        /// </summary>
        /// <param name="obj"></param>
        private static void SigTermEventHandler(AssemblyLoadContext obj)
        {
            Console.WriteLine("bl-status-svc: Unloading...");
            
            // Stop the Job Scheduler
            JobManager.Stop();

            /*
            * NLog: flush and stop internal timers/threads before 
            * application-exit (Avoid segmentation fault on Linux)
            */
            NLog.LogManager.Shutdown();
        }

        /// <summary>
        /// Service Cancel Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void CancelHandler(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("bl-status-svc: Canceled. Exiting...");

            // Stop the Job Scheduler
            JobManager.Stop();

            /*
            * NLog: flush and stop internal timers/threads before 
            * application-exit (Avoid segmentation fault on Linux)
            */
            NLog.LogManager.Shutdown();
        }

        /// <summary>
        /// Dependancy Injection Service Provider (For: NILog). 
        /// Allows other Classes to reference a single instance of the Logger
        /// </summary>
        /// <returns></returns>
        private static IServiceProvider BuildDi()
        {
            var services = new ServiceCollection();

            // Add the custom class(es) that will reference Singleton(s) - Jobs
            //services.AddTransient<TestJob>();
            services.AddTransient<BackupJob>();

            // Build Injectable Logger Service (using NLog)
            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddLogging((builder) => builder.SetMinimumLevel(LogLevel.Trace));
            
            var serviceProvider = services.BuildServiceProvider();

            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            // Configure NLog
            loggerFactory.AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true });

            // Expose Injectable Service(es)
            return serviceProvider;
        }

    }
}
