using System;
using NET6.Microservice.Order.API.Domain.AggregateModels.OrderAggregates;

namespace NET6.Microservice.Order.API.Domain.SeedWork
{
    public interface IRepository<T> where T : IAggregateRoot
    {
        IUnitOfWork UnitOfWork { get; }
    }
}

