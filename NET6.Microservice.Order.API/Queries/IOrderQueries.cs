using NET6.Microservice.Order.API.Models;

namespace NET6.Microservice.Order.API.Queries;

public interface IOrderQueries
{
    Task<NET6.Microservice.Order.API.Models.Order> GetOrderAsync(int id);

    Task<IEnumerable<OrderSummary>> GetOrdersFromUserAsync(Guid userId);

    Task<IEnumerable<CardType>> GetCardTypesAsync();
}