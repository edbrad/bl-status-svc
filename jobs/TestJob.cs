using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentScheduler;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using iText.IO.Font;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;

/// <summary>
/// TEST JOB: Scheduler Job to test functionality
/// </summary>
public class TestJob : IJob
{
    // Define Local Variables
    private readonly object _lock = new object();
    private readonly ILogger<TestJob> _logger;
    private bool _shuttingDown;
    EmailService _email = new EmailService();
    List<String> _jsondata = new List<String>();
    string _url = "";
    private string _filePath;
    string _apiTestResponse = "";

    /// <summary>
    /// Class Constructor
    /// </summary>
    /// <param name="logger"></param>
    public TestJob(ILogger<TestJob> logger)
    {
        // Initialize
        _logger = logger; /* get injected Logger singleton */
        _filePath = "";   /* clear PDF file path/name */
    }

    /// <summary>
    /// Execute Job Task(s)
    /// </summary>
    /// <returns></returns>
    public async void Execute()
    {
        try
        {
            // Make API Call
            // - verify that API endpoint is available
            _jsondata.Clear();
            _url = "https://jsonplaceholder.typicode.com/users";
            _logger.LogDebug("Testing Backend API Connection to: " + _url + "...");
            try
            {
                _apiTestResponse = await GetRequestAsyncTest(_url);
                _logger.LogDebug("API Response: " + _apiTestResponse);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogDebug("ERROR - Backend API Connection failure - HTTP Request Error:\n " + ex);
            }

            lock (_lock)
            {
                if (_shuttingDown)
                    return;

                // Load Seed Data into Database
                _logger.LogDebug("TestJob: Creating & Loading Seed Data into MongoDB...");
                BsonDocument[] seedData = CreateSeedData();
                AsyncMongoCrudTest(seedData).Wait();
                _logger.LogDebug("TestJob: Data Loaded!");

                // Run BASH Command (backup MongoDB Database)
                _logger.LogDebug("TestJob: Run Shell Command: Backup MongoDB data...");
                Console.WriteLine(Shell("mongodump --host localhost --port 27017 --out data/db-backup/testbackup"));
                _logger.LogDebug("TestJob: Shell Command: Complete!");

                // Generate PDF
                _logger.LogDebug("TestJob: Generating PDF...");
                GeneratePDFTest();
                _logger.LogDebug("TestJob: PDF Generated!");

                // Send Email Notification
                _logger.LogDebug("TestJob: Creating & Sending Email...");

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

                emailMessage.Subject = "TestJob Notification";
                emailMessage.Content = "<h1>TEST JOB<h1><p>This is a Test Job</p><br>" + _apiTestResponse;

                string attachmentPath = _filePath;

                /*
                * send composed message - via Email Service
                */
                _email.Send(emailMessage, attachmentPath);
                _logger.LogDebug("TestJob: Email Sent!");
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
    /// Test Code (MongoDB CRUD Testing) - Load/Read/Filter Dummy data w/ Local Database
    /// </summary>
    /// <param name="seedData"></param>
    /// <returns></returns>
    async static Task AsyncMongoCrudTest(BsonDocument[] seedData)
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
    /// Test Code (data)
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

    /// <summary>
    /// Submit a asynchronus http GET request to a REST API URL
    /// </summary>
    /// <param name="url">The URL of the http endpoint</param>
    /// <returns>The response content returned from the GET request</returns>
    async static Task<string> GetRequestAsyncTest(string url)
    {
        using (HttpClient client = new HttpClient())
        {
            using (HttpResponseMessage response = await client.GetAsync(url))
            {
                using (HttpContent content = response.Content)
                {
                    string webcontent = await content.ReadAsStringAsync();
                    return webcontent;
                }
            }
        }
    }

    /// <summary>
    /// Generate a Sample PDF File.
    /// </summary>
    void GeneratePDFTest()
    {
        // Write Sample PDF File
        /* set output file path */
        _filePath = Path.Combine("pdf", "test-job.pdf");
        _logger.LogDebug("TestJob: PDF File Path: " + _filePath);

        /* delete the existing file, if it already exists */
        if (File.Exists(_filePath))
        {
            _logger.LogDebug("TestJob: Deleting Existing File: " + _filePath);
            File.Delete(_filePath);
        }

        /* define a PDF file writter */
        var writer = new PdfWriter(_filePath);

        /* define a new PDF document and associate it w/ PDF writter */
        var pdf = new PdfDocument(writer);
        var document = new Document(pdf);

        /* create a PdfFont */
        var font = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);

        /* add a paragraph */
        document.Add(new Paragraph("iText is:").SetFont(font));
        
        /* create a list */
        List list = new List()
            .SetSymbolIndent(12)
            .SetListSymbol("\u2022") /* set the list layout */
            .SetFont(font);

        /* add List Item objects */
        list.Add(new ListItem("Never gonna give you up"))
            .Add(new ListItem("Never gonna let you down"))
            .Add(new ListItem("Never gonna run around and desert you"))
            .Add(new ListItem("Never gonna make you cry"))
            .Add(new ListItem("Never gonna say goodbye!"))
            .Add(new ListItem("Never gonna tell a lie and hurt you!"));

        /* add the list to the PDF document */
        document.Add(list);

        /* close/complete the PDF file */
        document.Close();
    }

    /// <summary>
    /// Copy a File.
    /// </summary>
    void FileCopyTest(string fromFilePath, string toFilePath)
    {
        /* copy file */
        _logger.LogDebug("Copying file: " + fromFilePath + " to: " + toFilePath);
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