using Ardalis.Specification;

namespace Clean.Architecture.Core.Domain.BrokerAggregate.Specifications;
public class BrokerByIdWithAgencySpec : Specification<Broker>, ISingleResultSpecification
{
  public BrokerByIdWithAgencySpec(Guid brokerId)
  {
    Query.Where(broker => broker.Id == brokerId)
      .Include(broker => broker.Agency);
  }
}
