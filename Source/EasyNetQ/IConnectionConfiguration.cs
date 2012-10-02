﻿using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ
{
    public interface IConnectionConfiguration
    {
        ushort Port { get; }
        string VirtualHost { get; }
        string UserName { get; }
        string Password { get; }
        ushort RequestedHeartbeat { get; }

        IEnumerable<IHostConfiguration> Hosts { get; }
    }

    public interface IHostConfiguration
    {
        string Host { get; }
        ushort Port { get; }
    }

    public class ConnectionConfiguration : IConnectionConfiguration
    {
        public ushort Port { get; set; }
        public string VirtualHost { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public ushort RequestedHeartbeat { get; set; }

        public IEnumerable<IHostConfiguration> Hosts { get; set; }

        public ConnectionConfiguration()
        {
            // set default values
            Port = 5672;
            VirtualHost = "/";
            UserName = "guest";
            Password = "guest";
            RequestedHeartbeat = 0;
            Hosts = new List<IHostConfiguration>();
        }

        public void Validate()
        {
            if (!Hosts.Any())
            {
                throw new EasyNetQException("Invalid connection string. 'host' value must be supplied. e.g: \"host=myserver\"");
            }
            foreach (var hostConfiguration in Hosts)
            {
                if (hostConfiguration.Port == 0)
                {
                    ((HostConfiguration)hostConfiguration).Port = Port;
                }
            }
        }
    }

    public class HostConfiguration : IHostConfiguration
    {
        public string Host { get; set; }
        public ushort Port { get; set; }
    }
}