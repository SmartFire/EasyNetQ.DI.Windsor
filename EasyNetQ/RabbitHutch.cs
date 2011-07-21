using System;
using System.Configuration;
using EasyNetQ.Loggers;
using RabbitMQ.Client;

namespace EasyNetQ
{
    /// <summary>
    /// Does poor man's dependency injection. Supplies default instances of services required by
    /// RabbitBus.
    /// </summary>
    public static class RabbitHutch
    {
        /// <summary>
        /// Creates a new instance of RabbitBus with default credentials.
        /// </summary>
        /// <param name="hostName">
        /// The RabbitMQ broker. To use the default Virtual Host, simply use the server name, e.g. 'localhost'.
        /// To identify the Virtual Host use the following scheme: 'hostname/virtualhost' e.g. 'localhost/myvhost'
        /// </param>
        /// <returns>
        /// A new RabbitBus instance.
        /// </returns>
        public static IBus CreateBus(string hostName)
        {
            return CreateBus(hostName, "guest", "guest");
        }

        /// <summary>
        /// Creates a new instance of RabbitBus
        /// </summary>
        /// <param name="hostName">
        /// The RabbitMQ broker. To use the default Virtual Host, simply use the server name, e.g. 'localhost'.
        /// To identify the Virtual Host use the following scheme: 'hostname/virtualhost' e.g. 'localhost/myvhost'
        /// </param>
        /// <param name="username">
        /// The username to use to connect to the RabbitMQ broker.
        /// </param>
        /// <param name="password">
        /// The password to use to connect to the RabbitMQ broker.
        /// </param>
        /// <returns>
        /// A new RabbitBus instance.
        /// </returns>
        public static IBus CreateBus(string hostName, string username, string password)
        {
            return CreateBus(hostName, username, password, new ConsoleLogger());
        }

        /// <summary>
        /// Creates a new instance of RabbitBus
        /// </summary>
        /// <param name="hostName">
        /// The RabbitMQ broker. To use the default Virtual Host, simply use the server name, e.g. 'localhost'.
        /// To identify the Virtual Host use the following scheme: 'hostname/virtualhost' e.g. 'localhost/myvhost'
        /// </param>
        /// <param name="username">
        /// The username to use to connect to the RabbitMQ broker.
        /// </param>
        /// <param name="password">
        /// The password to use to connect to the RabbitMQ broker.
        /// </param>
        /// <param name="logger">
        /// The logger to use.
        /// </param>
        /// <returns>
        /// A new RabbitBus instance.
        /// </returns>
        public static IBus CreateBus(string hostName, string username, string password, IEasyNetQLogger logger)
        {
            if(hostName == null)
            {
                throw new ArgumentNullException("hostName");
            }
            if(username == null)
            {
                throw new ArgumentNullException("username");
            }
            if(password == null)
            {
                throw new ArgumentNullException("password");
            }
            if(logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            var rabbitHost = GetRabbitHost(hostName);

            var connectionFactory = new ConnectionFactory
            {
                HostName = rabbitHost.HostName,
                VirtualHost = rabbitHost.VirtualHost,
                UserName = username,
                Password = password
            };

            var serializer = new JsonSerializer();

            var consumerErrorStrategy = new DefaultConsumerErrorStrategy(connectionFactory, serializer, logger);

            return new RabbitBus(
                TypeNameSerializer.Serialize,
                serializer,
                new QueueingConsumerFactory(logger, consumerErrorStrategy),
                connectionFactory,
                logger);
        }

        public static RabbitHost GetRabbitHost(string hostName)
        {
            var hostNameParts = hostName.Split('/');
            if (hostNameParts.Length > 2)
            {
                throw new EasyNetQException(@"hostname has too many parts, expecting '<server>/<vhost>' but was: '{0}'", 
                    hostName);
            }

            return new RabbitHost
            {
                HostName = hostNameParts[0],
                VirtualHost = hostNameParts.Length==1 ? "/" : hostNameParts[1]
            };
        }

        public struct RabbitHost
        {
            public string HostName;
            public string VirtualHost;
        }

        /// <summary>
        /// Creates a new instance of RabbitBus
        /// The RabbitMQ broker is defined in the connection string named 'rabbit'
        /// </summary>
        /// <returns></returns>
        public static IBus CreateBus()
        {
            var rabbitConnectionString = ConfigurationManager.ConnectionStrings["rabbit"];
            if (rabbitConnectionString == null)
            {
                throw new EasyNetQException(
                    "Could not find a connection string for RabbitMQ. " +
                    "Please add a connection string in the <ConnectionStrings> secion" +
                    "of the application's configuration file. For example: " +
                    "<add name=\"rabbit\" connectionString=\"localhost\" />");
            }

            return CreateBus(rabbitConnectionString.ConnectionString);
        }
    }
}