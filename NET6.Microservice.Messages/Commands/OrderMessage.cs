using System;
using NET6.Microservice.Messages.Bases;

namespace NET6.Microservice.Messages.Commands
{
    public class OrderMessage : BaseMessage
    {
        public Guid OrderId { get; set; }

        public double OrderAmount { get; set; }

        public string OrderNumber { get; set; }

        public DateTime OrderDate { get; set; }
    }
}