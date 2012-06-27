﻿using System.Collections.Concurrent;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ
{
    /// <summary>
    /// Identical to the C# client's DefaultQueueingConsumer but it doesn't close the queue
    /// when its own model is closed.
    /// </summary>
    public class EasyNetQConsumer : DefaultBasicConsumer
    {
        private readonly ConcurrentQueue<BasicDeliverEventArgs> queue;

        public ConcurrentQueue<BasicDeliverEventArgs> Queue
        {
            get { return queue; }
        }

        public EasyNetQConsumer(IModel model, ConcurrentQueue<BasicDeliverEventArgs> queue)
            : base(model)
        {
            this.queue = queue;
        }

        /// <summary>
        /// Closes the consumer's model.
        /// </summary>
        public void CloseModel()
        {
            this.Model.Close();
        }

        /// <summary>
        /// Overrides DefaultBasicConsumer's
        ///             HandleBasicDeliver implementation, building a
        ///             BasicDeliverEventArgs instance and placing it in the
        ///             Queue.
        /// </summary>
        public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, byte[] body)
        {
            queue.Enqueue(new BasicDeliverEventArgs()
            {
                ConsumerTag = consumerTag,
                DeliveryTag = deliveryTag,
                Redelivered = redelivered,
                Exchange = exchange,
                RoutingKey = routingKey,
                BasicProperties = properties,
                Body = body
            });
        }

    }
}