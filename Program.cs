using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core;

namespace bl_status_svc
{
    class Program
    {

        /// <summary>
        /// BL-STATUS-SVC: Box Loading Status Superintendent Service
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            var sleep = 3000;
            AssemblyLoadContext.Default.Unloading += SigTermEventHandler;
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelHandler);

            // Initialize Logger
            var logger = NLog.LogManager.LoadConfiguration("nlog.config").GetCurrentClassLogger();
            logger.Debug("Logging started!");

            // Run Work/Test Code
            BsonDocument[] seedData = CreateSeedData();
            AsyncCrud(seedData).Wait();

            // Run Logging Test
            if (args.Length > 0) { int.TryParse(args[0], out sleep); }
            while (true)
            {
                Console.WriteLine($"bl-status-svc: Working, pausing for {sleep}ms");
                logger.Info($"bl-status-svc: Working, pausing for {sleep}ms");
                Thread.Sleep(sleep);
            }
        }

        /// <summary>
        /// Service Shutdown Event Handler
        /// </summary>
        /// <param name="obj"></param>
        private static void SigTermEventHandler(AssemblyLoadContext obj)
        {
            Console.WriteLine("bl-status-svc: Unloading...");
            // NLog: flush and stop internal timers/threads before 
            // application-exit (Avoid segmentation fault on Linux)
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
            // Ensure to flush and stop internal timers/threads before 
            // application-exit (Avoid segmentation fault on Linux)
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

            // Add the custom class(es) that will reference Singleton(s)
            //services.AddTransient<Runner>();

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

        /// <summary>
        /// Work Code (MongoDB CRUD Testing)
        /// </summary>
        /// <param name="seedData"></param>
        /// <returns></returns>
        async static Task AsyncCrud(BsonDocument[] seedData)
        {
            // Create seed data
            BsonDocument[] songData = seedData;

            // Standard URI format: mongodb://[dbuser:dbpassword@]host:port/dbname
            String uri = "mongodb://127.0.0.1:27017/db";

            var client = new MongoClient(uri);
            var db = client.GetDatabase("db");

            /*
             * First we'll add a few songs. Nothing is required to create the
             * songs collection; it is created automatically when we insert.
             */

            var songs = db.GetCollection<BsonDocument>("songs");

            // Use InsertOneAsync for single BsonDocument insertion.
            await songs.InsertManyAsync(songData);

            /*
             * Then we need to give Boyz II Men credit for their contribution to
             * the hit "One Sweet Day".
             */

            var updateFilter = Builders<BsonDocument>.Filter.Eq("Title", "One Sweet Day");
            var update = Builders<BsonDocument>.Update.Set("Artist", "Mariah Carey ft. Boyz II Men");

            await songs.UpdateOneAsync(updateFilter, update);

            /*
             * Finally we run a query which returns all the hits that spent 10 
             * or more weeks at number 1.
             */

            var filter = Builders<BsonDocument>.Filter.Gte("WeeksAtOne", 10);
            var sort = Builders<BsonDocument>.Sort.Ascending("Decade");

            await songs.Find(filter).Sort(sort).ForEachAsync(song =>
              Console.WriteLine("In the {0}, {1} by {2} topped the charts for {3} straight weeks",
                song["Decade"], song["Title"], song["Artist"], song["WeeksAtOne"])
            );

            // Since this is an example, we'll clean up after ourselves.
            //await db.DropCollectionAsync("songs");
        }

        /// <summary>
        /// Work Code (data)
        /// </summary>
        /// <returns></returns>
        static BsonDocument[] CreateSeedData()
        {
            BsonDocument seventies = new BsonDocument {
                { "Decade" , "1970s" },
                { "Artist" , "Debby Boone" },
                { "Title" , "You Light Up My Life" },
                { "WeeksAtOne" , 10 }
            };

            BsonDocument eighties = new BsonDocument {
                { "Decade" , "1980s" },
                { "Artist" , "Olivia Newton-John" },
                { "Title" , "Physical" },
                { "WeeksAtOne" , 10 }
            };

            BsonDocument nineties = new BsonDocument {
                { "Decade" , "1990s" },
                { "Artist" , "Mariah Carey" },
                { "Title" , "One Sweet Day" },
                { "WeeksAtOne" , 16 }
            };

            BsonDocument[] SeedData = { seventies, eighties, nineties };
            return SeedData;
        }
    }
}
