using System;
using RabbitMQ.Client;

namespace EasyNetQ
{
    public class RabbitBus : IBus
    {
        private readonly SerializeType serializeType;
        private readonly ISerializer serializer;
        private readonly IConnection connection;
        private readonly IConsumerFactory consumerFactory;

        private const string rpcExchange = "easy_net_q_rpc";

        public RabbitBus(
            SerializeType serializeType, 
            ISerializer serializer,
            IConnection connection, 
            IConsumerFactory consumerFactory)
        {
            if(serializeType == null)
            {
                throw new ArgumentNullException("serializeType");
            }
            if(serializer == null)
            {
                throw new ArgumentNullException("serializer");
            }
            if(connection == null)
            {
                throw new ArgumentNullException("connection");
            }
            if (consumerFactory == null)
            {
                throw new ArgumentNullException("consumerFactory");
            }

            this.serializeType = serializeType;
            this.consumerFactory = consumerFactory;
            this.serializer = serializer;
            this.connection = connection;
        }

        public void Publish<T>(T message)
        {
            if(message == null)
            {
                throw new ArgumentNullException("message");
            }

            var typeName = serializeType(typeof (T));
            var messageBody = serializer.MessageToBytes(message);

            using (var channel = connection.CreateModel())
            {
                DeclarePublishExchange(channel, typeName);

                var defaultProperties = channel.CreateBasicProperties();
                channel.BasicPublish(
                    typeName,                   // exchange
                    typeName,                   // routingKey 
                    defaultProperties,          // basicProperties
                    messageBody);               // body
            }
        }

        private static void DeclarePublishExchange(IModel channel, string typeName)
        {
            channel.ExchangeDeclare(
                typeName,               // exchange
                ExchangeType.Direct,    // type
                true);                  // durable
        }

        public void Subscribe<T>(string subscriptionId, Action<T> onMessage)
        {
            if(onMessage == null)
            {
                throw new ArgumentNullException("onMessage");
            }

            var typeName = serializeType(typeof(T));
            var subscriptionQueue = string.Format("{0}_{1}", subscriptionId, typeName);

            var channel = connection.CreateModel();
            DeclarePublishExchange(channel, typeName);

            var queue = channel.QueueDeclare(
                subscriptionQueue,  // queue
                true,               // durable
                false,              // exclusive
                false,              // autoDelete
                null);              // arguments

            channel.QueueBind(queue, typeName, typeName);  

            // TODO: how does the channel (IModel) get disposed?  
            var consumer = consumerFactory.CreateConsumer(channel, 
                (consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body) =>
                {
                    var message = serializer.BytesToMessage<T>(body);
                    onMessage(message);
                    //channel.BasicAck(deliveryTag, false);
                });

            channel.BasicConsume(
                subscriptionQueue,      // queue
                true,                   // noAck 
                consumer.ConsumerTag,   // consumerTag
                consumer);              // consumer
        }

        public void Request<TRequest, TResponse>(TRequest request, Action<TResponse> onResponse)
        {
            Request<TRequest, TResponse>(onResponse)(request);
        }

        private Action<TRequest> Request<TRequest, TResponse>(Action<TResponse> onResponse)
        {
            if (onResponse == null)
            {
                throw new ArgumentNullException("onResponse");
            }

            var requestTypeName = serializeType(typeof(TRequest));

            var requestChannel = connection.CreateModel();
            var responseChannel = connection.CreateModel();

            // respond queue is transient, only exists for the lifetime of the call.
            var respondQueue = responseChannel.QueueDeclare();

            // tell the consumer to respond to the transient respondQueue
            var requestProperties = requestChannel.CreateBasicProperties();
            requestProperties.ReplyTo = respondQueue;

            DeclareRequestResponseStructure(requestChannel, requestTypeName);

            var consumer = consumerFactory.CreateConsumer(responseChannel,
                (consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body) =>
                {
                    var response = serializer.BytesToMessage<TResponse>(body);
                    onResponse(response);
                    //responseChannel.BasicAck(deliveryTag, false);
                });

            responseChannel.BasicConsume(
                respondQueue,           // queue
                true,                  // noAck 
                consumer.ConsumerTag,   // consumerTag
                consumer);              // consumer

            return request =>
            {
                if (request == null)
                {
                    throw new ArgumentNullException("request");
                }

                var requestBody = serializer.MessageToBytes(request);
                requestChannel.BasicPublish(
                    rpcExchange,            // exchange 
                    requestTypeName,        // routingKey 
                    requestProperties,      // basicProperties 
                    requestBody);           // body
            };
        }

        public void Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder)
        {
            if(responder == null)
            {
                throw new ArgumentNullException("responder");
            }

            var requestTypeName = serializeType(typeof(TRequest));
            var requestChannel = connection.CreateModel();

            DeclareRequestResponseStructure(requestChannel, requestTypeName);

            var consumer = consumerFactory.CreateConsumer(requestChannel,
                (consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body) =>
                {
                    var request = serializer.BytesToMessage<TRequest>(body);
                    var response = responder(request);
                    var responseProperties = requestChannel.CreateBasicProperties();
                    var responseBody = serializer.MessageToBytes(response);
                    requestChannel.BasicPublish(
                        "",                 // exchange 
                        properties.ReplyTo, // routingKey
                        responseProperties, // basicProperties 
                        responseBody);      // body
                });

            // TODO: dispose channel
            requestChannel.BasicConsume(
                requestTypeName,        // queue 
                true,                   // noAck 
                consumer.ConsumerTag,   // consumerTag
                consumer);              // consumer
        }

        private static void DeclareRequestResponseStructure(IModel channel, string requestTypeName)
        {
            channel.ExchangeDeclare(
                rpcExchange,            // exchange 
                ExchangeType.Direct,    // type 
                false,                  // autoDelete 
                true,                   // durable 
                null);                  // arguments

            channel.QueueDeclare(
                requestTypeName,    // queue 
                true,               // durable 
                false,              // exclusive 
                false,              // autoDelete 
                null);              // arguments

            channel.QueueBind(
                requestTypeName,    // queue
                rpcExchange,        // exchange 
                requestTypeName);   // routingKey
        }

        private bool disposed = false;
        public void Dispose()
        {
            if (disposed) return;
            
            connection.Close();
            connection.Dispose();
            disposed = true;
        }
    }
}