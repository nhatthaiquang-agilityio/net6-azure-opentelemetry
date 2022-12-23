using System.Diagnostics;
using System.Net;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using NET6.Microservice.Core.OpenTelemetry;
using NET6.Microservice.Order.API.Models.Requests;
using NET6.Microservice.Order.API.Queries;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;


namespace NET6.Microservice.Order.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly ILogger<OrderController> _logger;
        private readonly IBus _bus;
        private static readonly ActivitySource _activitySource = new ActivitySource("AzureMonitor.OpenTelemetry");
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
        private readonly IOrderQueries _orderQueries;

        public OrderController(ILogger<OrderController> logger, IBus bus, IOrderQueries orderQueries)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _bus = bus ?? throw new ArgumentNullException(nameof(logger));
            _orderQueries = orderQueries ?? throw new ArgumentNullException(nameof(orderQueries));
        }

        [HttpGet]
        public async Task<ActionResult> GetOrdersAsync()
        {

            using var activity = _activitySource.StartActivity("Get Order", ActivityKind.Client);

            _logger.LogInformation("Get Order");
            OpenTelemetryActivity.AddActivityTagsMessage(activity);

            activity?.SetStatus(ActivityStatusCode.Ok, "Get Order successfully.");

            return Ok("Get Order");
        }

        [HttpPost]
        public async Task<IActionResult> OrderProduct(OrderRequest order)
        {
            using (var activity = Activity.Current)
            {
                _logger.LogInformation(
                    "Post Order API {CorrelationId} {OrderAmount}, {OrderNumber}",
                    activity?.Id, order.OrderAmount, order.OrderNumber);

                if (activity == null)
                {
                    throw new Exception("Activity source is null");
                }

                if (order != null)
                {
                    //set properties for Propagator
                    var props = new Dictionary<string, object>();
                    props["traceparent"] = activity?.Id;
                    props["operation_parentId"] = activity?.Id;

                    if (Propagator == null)
                    {
                        throw new Exception("Propagator is null");
                    }

                    // Inject the ActivityContext into the message headers to propagate trace context to the receiving service.
                    Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), props, null);

                    OpenTelemetryActivity.AddActivityTagsMessage(activity);

                    await _bus.Send(new Messages.Commands.OrderMessage()
                    {
                        OrderId = Guid.NewGuid(),
                        OrderAmount = order.OrderAmount,
                        OrderDate = DateTime.Now,
                        OrderNumber = order.OrderNumber,
                        CorrelationId = activity?.Id
                    });

                    _logger.LogInformation(
                        "Send to a message {CorrelationId} {OrderAmount}, {OrderNumber}",
                        activity?.Id, order.OrderAmount, order.OrderNumber);

                    activity?.SetStatus(ActivityStatusCode.Ok, "Send a message successfully.");

                    return Ok();
                }

                activity?.SetStatus(ActivityStatusCode.Error, "Error occurred when sending a message in Post Order API");
            }

            return BadRequest();
        }

        [HttpGet("{orderId:int}")]
        [ProducesResponseType(typeof(NET6.Microservice.Order.API.Models.Order), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult> GetOrderAsync(int orderId)
        {
            try
            {
                //Todo: It's good idea to take advantage of GetOrderByIdQuery and handle by GetCustomerByIdQueryHandler
                //var order customer = await _mediator.Send(new GetOrderByIdQuery(orderId));
                var order = await _orderQueries.GetOrderAsync(orderId);

                return Ok(order);
            }
            catch
            {
                return NotFound();
            }
        }


    }
}