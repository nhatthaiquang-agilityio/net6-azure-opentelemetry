using System;
using NET6.Microservice.Order.API.Domain.SeedWork;

namespace NET6.Microservice.Order.API.Domain.AggregateModels.OrderAggregates
{
    public interface IOrderRepository : IRepository<Order>
    {
        Order Add(Order order);

        void Update(Order order);

        Task<Order> GetAsync(int orderId);
    }
}

