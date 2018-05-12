using System;
using System.Threading;
using System.Runtime.Loader;
using Logging;

namespace bl_status_svc
{
    class Program
    {
        
        static void Main(string[] args)
        {
            var sleep = 3000;
            AssemblyLoadContext.Default.Unloading += SigTermEventHandler;
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelHandler);

            if (args.Length > 0) { int.TryParse(args[0], out sleep); }
            while (true)
            {
                Logger.Log($"bl-status-svc: Working, pausing for {sleep}ms", LogLevel.Info);
                Console.WriteLine($"bl-status-svc: Working, pausing for {sleep}ms");
                Thread.Sleep(sleep);
            }
        }

        private static void SigTermEventHandler(AssemblyLoadContext obj)
        {
            Logger.Log("bl-status-svc: Unloading...", LogLevel.Info);
            Console.WriteLine("bl-status-svc: Unloading...");
        }

        private static void CancelHandler(object sender, ConsoleCancelEventArgs e)
        {
            Logger.Log("bl-status-svc: Canceled. Exiting...", LogLevel.Warning);
            Console.WriteLine("bl-status-svc: Canceled. Exiting...");
        }
    }
}
