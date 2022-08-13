﻿using Newtonsoft.Json;

namespace Clean.Architecture.Core.Config;
public class DomainNotificationBase<T> : IDomainEventNotification<T> where T : IDomainEvent
{
  [JsonIgnore]
  public T DomainEvent { get; }

  public Guid Id { get; }

  public DomainNotificationBase(T domainEvent)
  {
    this.Id = Guid.NewGuid();
    this.DomainEvent = domainEvent;
  }
}
