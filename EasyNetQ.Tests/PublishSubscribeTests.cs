// ReSharper disable InconsistentNaming
using System;
using System.Threading;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class PublishSubscribeTests
    {
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            bus = RabbitHutch.CreateRabbitBus("localhost");
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        // 1. Run this first, should see no messages consumed
        // 3. Run this again (after publishing below), should see published messages appear
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_subscribe()
        {
            bus.Subscribe<MyMessage>("test", msg => Console.WriteLine(msg.Text));

            // allow time for messages to be consumed
            Thread.Sleep(100);

            Console.WriteLine("Stopped consuming");
        }

        // 2. Run this a few times, should publish some messages
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_publish()
        {
            bus.Publish(new MyMessage { Text = "Hello! " + Guid.NewGuid().ToString().Substring(0, 5) });
        }

        // 4. Run this once to setup subscription, publish a few times using '2' above, run again to
        // see messages appear.
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_also_send_messages_to_second_subscriber()
        {
            var messageQueue2 = RabbitHutch.CreateRabbitBus("localhost");
            messageQueue2.Subscribe<MyMessage>("test2", msg => Console.WriteLine(msg.Text));

            // allow time for messages to be consumed
            Thread.Sleep(100);

            Console.WriteLine("Stopped consuming");
        }

        // 5. Run this once to setup subscriptions, publish a few times using '2' above, run again.
        // You should see two lots messages
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_two_subscriptions_from_the_same_app_should_also_both_get_all_messages()
        {
            bus.Subscribe<MyMessage>("test_a", msg => Console.WriteLine(msg.Text));
            bus.Subscribe<MyMessage>("test_b", msg => Console.WriteLine(msg.Text));

            // allow time for messages to be consumed
            Thread.Sleep(100);

            Console.WriteLine("Stopped consuming");
        }
    }

    [Serializable]
    public class MyMessage
    {
        public string Text { get; set; }
    }
}

// ReSharper restore InconsistentNaming