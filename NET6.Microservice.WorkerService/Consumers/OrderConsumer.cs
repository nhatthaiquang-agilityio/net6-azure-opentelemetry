using System.Diagnostics;
using MassTransit;
using NET6.Microservice.Core.OpenTelemetry;
using NET6.Microservice.WorkerService.Services;
using OpenTelemetry.Context.Propagation;

namespace NET6.Microservice.WorkerService.Consumers
{
    public class OrderConsumer : IConsumer<Messages.Commands.OrderMessage>
    {
        private readonly ILogger<OrderConsumer> _logger;
        private readonly EmailService _emailService;
        private static readonly ActivitySource _activitySource = new ActivitySource("AzureMonitor.OpenTelemetry");
        private static readonly TextMapPropagator Propagator = new TraceContextPropagator();

        public OrderConsumer(ILogger<OrderConsumer> logger, EmailService emailService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        public Task Consume(ConsumeContext<Messages.Commands.OrderMessage> context)
        {
            var data = context.Message;
            var correlationId = data.CorrelationId;

            _logger.LogInformation("Consume Order Message {CorrelationId} {OrderNumber}", correlationId, data.OrderNumber);

            // set property for extracting Propagation context
            var pros = new Dictionary<string, object>();
            pros["traceparent"] = correlationId;

            // Extract the PropagationContext of order message
            var parentContext = Propagator.Extract(default, pros, OpenTelemetryActivity.ExtractTraceContextFromProperties);

            using var activity = _activitySource.StartActivity(
                "Order.Product Consumer", ActivityKind.Consumer, parentContext.ActivityContext);

            OpenTelemetryActivity.AddActivityTagsMessage(activity);

            try
            {
                // TODO: call service/task
                Task.Delay(2000);
                _emailService.SendEmail(correlationId, Guid.NewGuid(), "testing@domain.com", "Order: " + data.OrderNumber);
                activity?.SetStatus(ActivityStatusCode.Ok, "Consumed a message and processed successfully.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unable to send Email. {CorrelationId} ", correlationId);
                activity?.SetStatus(ActivityStatusCode.Error, "Error occured when sending email in OrderConsumer");
            }

            _logger.LogInformation("Consumed Order Message {CorrelationId} {OrderNumber}", correlationId, data.OrderNumber);
            return Task.CompletedTask;
        }
    }
}