using System.Diagnostics;

namespace NET6.Microservice.Core.OpenTelemetry
{
    public class OpenTelemetryActivity
    {
         //Add Tags to the Activity for Message
        public static void AddActivityTagsMessage(Activity activity)
        {
            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination_kind", "queue");
        }

        // Inject context for message
        public static void InjectContextMessage(Dictionary<string, object> props, string key, string value)
        {
            try
            {
                props[key] = value;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Failed to inject trace context.");
                Console.WriteLine($"Failed to inject trace context.{ex}");
            }
        }

        // Extract the Activity from the message
        public static IEnumerable<string> ExtractTraceContextFromProperties(Dictionary<string, object> props, string key)
        {
            try
            {
                if (props.TryGetValue(key, out var value))
                {
                    var bytes = value as byte[];
                    return new [] { value.ToString() };
                }
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, $"Failed to extract trace context");
                Console.WriteLine($"Failed to extract trace context.{ex}");
            }

            return Enumerable.Empty<string>();
        }
    }
}