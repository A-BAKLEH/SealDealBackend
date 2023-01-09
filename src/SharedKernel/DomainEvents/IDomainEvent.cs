using MediatR;

public interface IDomainEvent : INotification
{
  DateTime OccurredOn { get; }
}
