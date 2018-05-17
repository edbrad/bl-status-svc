using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentScheduler;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core;

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

                // Load Seed Data into Database
                _logger.LogDebug("TestJob: Creating & Loading Seed Data into MongoDB...");
                BsonDocument[] seedData = CreateSeedData();
                AsyncCrud(seedData).Wait();
                _logger.LogDebug("TestJob: Data Loaded!");

                // Send Email Notification
                _logger.LogDebug("TestJob: Creating & Sending Email...");

                // - create new message instance
                EmailMessage emailMessage = new EmailMessage();

                // - compose message
                List<EmailAddress> toAddresses = new List<EmailAddress>();
                List<EmailAddress> fromAddresses = new List<EmailAddress>();
                toAddresses.Add(new EmailAddress() 
                { 
                    Name = "Edward Bradley", 
                    Address = "edb@edbrad.com" 
                });

                fromAddresses.Add(new EmailAddress() 
                { 
                    Name = "Box Loading Status - Superintendent Service", 
                    Address = "bl-status-svc@gmail.com" 
                });

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
        await db.DropCollectionAsync("songs");
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