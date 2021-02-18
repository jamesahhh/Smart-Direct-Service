using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using log4net;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Text;

namespace Smart_Direct_Service
{
    class SmartDirectService
    {
        public static readonly ILog _log = LogManager.GetLogger(typeof(SmartDirectService));
        private FileSystemWatcher CsvWatcher;
        private string watchDirectory = ConfigurationManager.AppSettings["watchDirectory"];
        private string key = ConfigurationManager.AppSettings["APIsecret"];
        private string sender = ConfigurationManager.AppSettings["sender"];

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
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            };
            using (var reader = new StreamReader(e.FullPath))
            using (var csv = new CsvReader(reader, config))
            {
                var record = new Fields();
                var records = csv.EnumerateRecords(record);
                foreach (var r in records)
                {
                    CallExternalAPI(r.Phone, r.Message);
                }
            }
        }

        private void CallExternalAPI(string phone, string message)
        {
            var param = new { sender = sender, text = message, key = key, receiver = phone };

            var client = new RestClient("https://direct.smart.bz/api/P2001");

            var request = new RestRequest()
                .AddJsonBody(param);

            var reponse = client.Post(request);

            _log.Debug(reponse.Content);
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
