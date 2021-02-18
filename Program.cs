using log4net;
using log4net.Config;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Reflection;
using Topshelf;

namespace Smart_Direct_Service
{
    class Program
    {
        public static readonly ILog _log = LogManager.GetLogger(typeof(SmartDirectService));
        static void Main(string[] args)
        {
            HostFactory.Run(hostConfig =>
            {
                var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
                XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

                hostConfig.UseLog4Net("log4net.config");

                hostConfig.Service<SmartDirectService>(serviceConfig =>
                {
                    serviceConfig.ConstructUsing(() => new SmartDirectService());
                    serviceConfig.WhenStarted(s => s.Start());
                    serviceConfig.WhenStopped(s => s.Stop());
                });
                hostConfig.RunAsLocalSystem();
                hostConfig.SetDescription("Service to watch directory for incoming file input and perfrom interactions with Web API");
                hostConfig.SetDisplayName("Smart Direct Messager");
                hostConfig.SetServiceName("SDSMessager");
            });
        }
    }
}
