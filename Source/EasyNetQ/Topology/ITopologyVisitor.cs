namespace EasyNetQ.Topology
{
    public interface ITopologyVisitor
    {
        void CreateExchange(string exchangeName, ExchangeType exchangeType);
        void CreateQueue(string queueName, bool durable, bool exclusive, bool autoDelete);
        string CreateQueue();
        void CreateBinding(IBindable bindable, IExchange exchange, string[] routingKeys);
    }
}