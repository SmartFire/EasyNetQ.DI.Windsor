namespace EasyNetQ.DI.Tests
{
    public class TestConventions : Conventions
    {
        public TestConventions(ITypeNameSerializer typeNameSerializer) : base(typeNameSerializer)
        {
            QueueNamingConvention = (messageType, subscriptionId) =>
            {
                var typeName = typeNameSerializer.Serialize(messageType);
                return string.Format("{0}_{1}", typeName, subscriptionId);
            };

        }
    }
}
