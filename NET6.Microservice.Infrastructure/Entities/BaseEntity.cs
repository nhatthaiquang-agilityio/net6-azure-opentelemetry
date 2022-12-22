namespace NET6.Microservice.Infrastructure.Entities;

public abstract class BaseEntity
{
    public virtual int Id { get; protected set; }
}