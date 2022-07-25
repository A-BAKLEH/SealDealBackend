using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ardalis.Specification;

namespace Clean.Architecture.Core.BrokerAggregate.Specifications;
public class BrokerByIdWithAgencySpec : Specification<Broker>, ISingleResultSpecification
{
  public BrokerByIdWithAgencySpec(Guid brokerId)
  {
    Query.Where(broker => broker.Id == brokerId)
      .Include(broker => broker.Agency);
  }
}
