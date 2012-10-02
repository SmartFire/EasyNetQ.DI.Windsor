﻿// ReSharper disable InconsistentNaming

using System.Threading;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class ClusterTests
    {
        private const string clusterHost1 = "ubuntu";
        private const string clusterHost2 = "ubuntu";
        private const string clusterPort1 = "5672"; // rabbit@ubuntu
        private const string clusterPort2 = "5674"; // rabbit_2@ubuntu
        private string connectionString;

        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            const string hostFormat = "{0}:{1}";
            var host1 = string.Format(hostFormat, clusterHost1, clusterPort1);
            var host2 = string.Format(hostFormat, clusterHost2, clusterPort2);
            var hosts = string.Format("{0},{1}", host1, host2);
            connectionString = string.Format("host={0}", hosts);

            bus = RabbitHutch.CreateBus(connectionString);
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        [Test, Explicit("Requires a running rabbitMQ cluster on server 'ubuntu'")]
        public void Should_create_the_correct_connection_string()
        {
            connectionString.ShouldEqual("host=ubuntu:5672,ubuntu:5673");
        }

        [Test, Explicit("Requires a running rabbitMQ cluster on server 'ubuntu'")]
        public void Should_connect_to_the_first_available_node_in_cluster()
        {
            // just watch what happens
            Thread.Sleep(5 * 60 * 1000); // let's give it 5 minutes
        }
    }
}

// ReSharper restore InconsistentNaming