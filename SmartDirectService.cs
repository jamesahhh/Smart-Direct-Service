using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using log4net;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace Smart_Direct_Service
{
    class SmartDirectService
    {
        public static readonly ILog _log = LogManager.GetLogger(typeof(SmartDirectService));
        private FileSystemWatcher CsvWatcher;
        private string watchDirectory = ConfigurationManager.AppSettings["watchDirectory"];
        private string key = ConfigurationManager.AppSettings["APIsecret"];
        private string sender = ConfigurationManager.AppSettings["sender"];
        private string apiurl = ConfigurationManager.AppSettings["Apiurl"];
        private string outDirectory = ConfigurationManager.AppSettings["processedDirectory"];

        public void Start()
        {
            _log.Info("Starting up SDS");
            RunWatcher();
        }

        public void Stop()
        {
            CsvWatcher.Dispose();
            _log.Info("Stopping SDS");
        }

        private void RunWatcher()
        {
            if (!Directory.Exists(watchDirectory))
            {
                Directory.CreateDirectory(watchDirectory);
            }
            CsvWatcher = new FileSystemWatcher
            {
                Path = watchDirectory,
                //must have LastWrite and FileName to detect created events
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                Filter = "*.csv",
            };

            CsvWatcher.Created += new FileSystemEventHandler(processCSV);
            CsvWatcher.EnableRaisingEvents = true;
            _log.Debug("Watcher Initiated");
        }

        private void processCSV(object sender, FileSystemEventArgs e)
        {
            do
            {
                Thread.Sleep(100);
            } while (!IsFileClosed(e.FullPath, true));
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            };
            using (StreamReader reader = new StreamReader(e.FullPath))
            {
                using (CsvReader csv = new CsvReader(reader, config))
                {
                    var record = new Fields();
                    var records = csv.EnumerateRecords(record);
                    foreach (var r in records)
                    {
                        //_log.Info(r.Phone + r.Message);
                        CallExternalAPI(r.Phone, r.Message);
                    }
                }
            }
            File.Move(e.FullPath, outDirectory + e.Name);
            //_log.Info("disposed");

        }

        private void CallExternalAPI(string phone, string message)
        {
            var param = new { sender = sender, text = message, key = key, receiver = phone };

            var client = new RestClient(apiurl);

            var request = new RestRequest()
                .AddJsonBody(param);

            var reponse = client.Post(request);

            _log.Debug(reponse.Content);
        }

        public static bool IsFileClosed(string filepath, bool wait)
        {
            var fileClosed = false;
            var retries = 20;
            const int delay = 500; // Max time spent here = retries*delay milliseconds

            if (!File.Exists(filepath))
                return false;

            do
            {
                try
                {
                    // Attempts to open then close the file in RW mode, denying other users to place any locks.
                    var fs = File.Open(filepath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    fs.Close();
                    fileClosed = true; // success
                }
                catch (IOException) { }

                if (!wait) break;

                retries--;

                if (!fileClosed)
                    Thread.Sleep(delay);
            }
            while (!fileClosed && retries > 0);

            return fileClosed;
        }
    }



    public class Fields
    {
        [Index(1)]
        public string Message { get; set; }

        [Index(0)]
        public string Phone { get; set; }
    }
}
