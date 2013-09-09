﻿// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ.Topology;
using NUnit.Framework;

namespace EasyNetQ.Tests.Integration
{
    [TestFixture]
    [Explicit]
    public class AdvancedApiPingPongTest
    {
        private readonly IBus[] buses = new IBus[2];

        private const string connectionString = "host=localhost";
        private const string routingKey = "x";
        private const string messageText = "Hello World:0";
        private const long rallyLength = 1000;
        private long rallyCount;

        private readonly IQueue[] queues = new IQueue[2];
        private readonly IExchange[] exchanges = new IExchange[2];

        [SetUp]
        public void SetUp()
        {
            rallyCount = 0;
            for (int i = 0; i < 2; i++)
            {
                buses[i] = RabbitHutch.CreateBus(connectionString);
                var name = string.Format("advanced_ping_pong_{0}", i);

                exchanges[i] = buses[i].Advanced.ExchangeDeclare(name, "direct");
                queues[i] = buses[i].Advanced.QueueDeclare(name);
                buses[i].Advanced.QueuePurge(queues[i]);
                buses[i].Advanced.Bind(exchanges[i], queues[i], routingKey);
            }
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < 2; i++)
            {
                buses[i].Dispose();
            }
        }

        [Test, Explicit]
        public void Ping_pong_with_advances_consumers()
        {
            Consume(0, 1);
            Consume(1, 0);

            // kick off
            using (var channel = buses[1].Advanced.OpenPublishChannel())
            {
                var properties = new MessageProperties
                    {
                        CorrelationId = "ping pong test"
                    };
                var body = Encoding.UTF8.GetBytes(messageText);
                channel.Publish(exchanges[1], routingKey, properties, body);
            }

            while (Interlocked.Read(ref rallyCount) < rallyLength)
            {
                Thread.Sleep(100);
            }
        }

        public void Consume(int from, int to)
        {
            buses[from].Advanced.Consume(queues[from], (body, properties, info) => Task.Factory.StartNew(() =>
                {
                    using (var channel = buses[from].Advanced.OpenPublishChannel())
                    {
                        Console.Out.WriteLine("Consumer {0}: '{1}'", from, Encoding.UTF8.GetString(body));
                        Thread.Sleep(500);
                        var publishProperties = new MessageProperties
                            {
                                CorrelationId = properties.CorrelationId ?? "no id present"
                            };
                        var publishBody = GenerateNextMessage(body);
                        channel.Publish(exchanges[to], routingKey, publishProperties, publishBody);

                        Interlocked.Increment(ref rallyCount);
                    }
                }));
        }

        public byte[] GenerateNextMessage(byte[] previousMessageBody)
        {
            var bodyText = Encoding.UTF8.GetString(previousMessageBody);
            var bodyElements = bodyText.Split(':');
            var textPart = bodyElements[0];
            var count = int.Parse(bodyElements[1]);
            var nextBodyText = textPart + ":" + (++count);
            return Encoding.UTF8.GetBytes(nextBodyText);
        }
    }
}

// ReSharper restore InconsistentNaming