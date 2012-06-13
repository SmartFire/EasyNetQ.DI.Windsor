﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Topology
{
    public class Queue : IQueue
    {
        readonly bool durable;
        readonly bool exclusive;
        readonly bool autoDelete;

        private readonly IList<IBinding> bindings = new List<IBinding>();

        public static IQueue DeclareDurable(string queueName)
        {
            return new Queue(true, false, false, queueName);
        }

        public static IQueue DeclareTransient(string queueName)
        {
            return new Queue(false, true, true, queueName);
        }

        public static IQueue DeclareTransient()
        {
            return new Queue(false, true, true);
        }

        protected Queue(bool durable, bool exclusive, bool autoDelete, string name) 
            : this(durable, exclusive, autoDelete)
        {
            if(string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("name is null or empty");
            }
            Name = name;
        }

        protected Queue(bool durable, bool exclusive, bool autoDelete)
        {
            this.autoDelete = autoDelete;
            this.exclusive = exclusive;
            this.durable = durable;

            // making this assumption for now, that declaring a queue as autoDelete means that 
            // you only want to get one message and then Dispose.
            IsSingleUse = autoDelete;
        }

        public string Name { get; private set; }

        public bool IsSingleUse { get; private set; }

        public void BindTo(IExchange exchange, params string[] routingKeys)
        {
            if(exchange == null)
            {
                throw new ArgumentNullException("exchange");
            }
            if (exchange is DefaultExchange)
            {
                throw new EasyNetQException("All queues are bound automatically to the default exchange, do bind manually.");
            }
            if (routingKeys.Any(string.IsNullOrEmpty))
            {
                throw new ArgumentException("RoutingKey is null or empty");
            }
            if (routingKeys.Length == 0)
            {
                throw new ArgumentException("There must be at least one routingKey");
            }

            var binding = new Binding(this, exchange, routingKeys);
            bindings.Add(binding);
        }

        public void Visit(ITopologyVisitor visitor)
        {
            if(visitor == null)
            {
                throw new ArgumentNullException("visitor");
            }

            if (Name == null)
            {
                Name = visitor.CreateQueue();
            }
            else
            {
                visitor.CreateQueue(Name, durable, exclusive, autoDelete);
            }
            foreach (var binding in bindings)
            {
                binding.Visit(visitor);
            }
        }
    }
}