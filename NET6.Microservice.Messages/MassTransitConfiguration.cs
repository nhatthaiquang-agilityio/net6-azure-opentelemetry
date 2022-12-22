namespace NET6.Microservice.Messages
{
    public class MassTransitConfiguration
    {
        public string NotificationQueue { get; set;}
        public string OrderQueue { get; set;}
        public bool IsUsingAmazonSQS { get; set;}
        public bool IsUsingRabbitMQ { get; set;}
        public string MessageBusRabbitMQ { get; set;}
        public string MessageBusSQS { get; set;}
        public string AwsAccessKey { get; set;}
        public string AwsSecretKey { get; set;}
        public string AwsRegion { get; set;}
    }
}