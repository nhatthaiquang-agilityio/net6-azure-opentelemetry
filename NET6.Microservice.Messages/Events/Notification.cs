using System;
using NET6.Microservice.Messages.Bases;

namespace NET6.Microservice.Messages.Events
{
    public class Notification : BaseMessage
    {
        string NotificationType { get; set; }

        string NotificationContent { get; set; }

        string NotificationAddress { get; set; }

        DateTime NotificationDate { get; set; }
    }
}