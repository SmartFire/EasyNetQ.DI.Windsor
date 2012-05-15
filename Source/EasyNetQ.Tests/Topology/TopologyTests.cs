﻿// ReSharper disable InconsistentNaming

using EasyNetQ.Topology;
using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

namespace EasyNetQ.Tests.Topology
{
    [TestFixture]
    public class TopologyTests
    {
        private IModel model;
        private ITopologyVisitor visitor;

        private const string exchangeName = "speedster";
        private const string queueName = "roadster";
        private const string routingKey = "drop_head";

        [SetUp]
        public void SetUp()
        {
            model = MockRepository.GenerateStub<IModel>();
            visitor = new TopologyBuilder(model);
        }

        //  XD
        [Test]
        public void Should_create_a_direct_exchange()
        {
            var exchange = Exchange.CreateDirect(exchangeName);
            exchange.Visit(visitor);

            model.AssertWasCalled(x => x.ExchangeDeclare(exchangeName, "Direct", true));
        }

        //  XT
        [Test]
        public void Should_create_a_topic_exchange()
        {
            var exchange = Exchange.CreateTopic(exchangeName);
            exchange.Visit(visitor);

            model.AssertWasCalled(x => x.ExchangeDeclare(exchangeName, "Topic", true));
        }

        // QD
        [Test]
        public void Should_create_a_durable_queue()
        {
            var queue = Queue.CreateDurable(queueName);
            queue.Visit(visitor);

            model.AssertWasCalled(x => x.QueueDeclare(queueName, true, false, false, null));
        }

        // QT
        [Test]
        public void Should_create_a_transiet_queue()
        {
            var queue = Queue.CreateTransient(queueName);
            queue.Visit(visitor);

            model.AssertWasCalled(x => x.QueueDeclare(queueName, false, true, true, null));
        }

        //  XD -> QD
        [Test]
        public void Should_be_able_to_bind_a_queue_to_an_exchange()
        {
            var queue = Queue.CreateDurable(queueName);
            var exchange = Exchange.CreateDirect(exchangeName);

            queue.BindTo(exchange, routingKey);
            queue.Visit(visitor);

            model.AssertWasCalled(x => x.QueueDeclare(queueName, true, false, false, null));
            model.AssertWasCalled(x => x.ExchangeDeclare(exchangeName, "Direct", true));
            model.AssertWasCalled(x => x.QueueBind(queueName, exchangeName, routingKey));
        }

        //      ->
        //  XD  ->  QD
        //      ->
        [Test]
        public void Should_be_able_to_have_multiple_bindings_to_an_exchange()
        {
            var queue = Queue.CreateDurable(queueName);
            var exchange = Exchange.CreateDirect(exchangeName);

            queue.BindTo(exchange, "a", "b", "c");
            queue.Visit(visitor);

            model.AssertWasCalled(x => x.QueueDeclare(queueName, true, false, false, null));
            model.AssertWasCalled(x => x.ExchangeDeclare(exchangeName, "Direct", true));
            model.AssertWasCalled(x => x.QueueBind(queueName, exchangeName, "a"));
            model.AssertWasCalled(x => x.QueueBind(queueName, exchangeName, "b"));
            model.AssertWasCalled(x => x.QueueBind(queueName, exchangeName, "c"));
        }

        //  XD  ->  XD
        [Test]
        public void Should_be_able_to_bind_an_exchange_to_an_exchange()
        {
            var sourceExchange = Exchange.CreateDirect("source");
            var destinationExchange = Exchange.CreateDirect("destination");

            destinationExchange.BindTo(sourceExchange, routingKey);
            destinationExchange.Visit(visitor);

            model.AssertWasCalled(x => x.ExchangeDeclare("destination", "Direct", true));
            model.AssertWasCalled(x => x.ExchangeDeclare("source", "Direct", true));
            model.AssertWasCalled(x => x.ExchangeBind("destination", "source", routingKey));
        }

        // XD -> XD -> QD
        [Test]
        public void Should_be_able_to_bind_a_queue_to_an_exchange_and_then_to_an_exchange()
        {
            var sourceExchange = Exchange.CreateDirect("source");
            var destinationExchange = Exchange.CreateDirect("destination");
            var queue = Queue.CreateDurable(queueName);

            destinationExchange.BindTo(sourceExchange, routingKey);
            queue.BindTo(destinationExchange, routingKey);

            queue.Visit(visitor);

            model.AssertWasCalled(x => x.QueueDeclare(queueName, true, false, false, null));
            model.AssertWasCalled(x => x.ExchangeDeclare("destination", "Direct", true));
            model.AssertWasCalled(x => x.QueueBind(queueName, "destination", routingKey));
            model.AssertWasCalled(x => x.ExchangeDeclare("source", "Direct", true));
            model.AssertWasCalled(x => x.ExchangeBind("destination", "source", routingKey));
        }
    }
}

// ReSharper restore InconsistentNaming