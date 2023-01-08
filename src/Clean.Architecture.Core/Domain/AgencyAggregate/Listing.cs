﻿
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.Domain.LeadAggregate;
using Clean.Architecture.SharedKernel;
namespace Clean.Architecture.Core.Domain.AgencyAggregate;

public enum ListingStatus
{
  Listed, Sold
}
public class Listing : Entity<int>
{
  public int AgencyId { get; set; }
  public Agency Agency { get; set; }
  public Address Address { get; set; }
  /// <summary>
  /// client timeZ
  /// </summary>
  public DateTimeOffset DateOfListing { get; set; }
  public ListingStatus Status { get; set; } = ListingStatus.Listed;
  public int Price { get; set; }
  public int AssignedBrokersCount { get; set; } = 0;
  public List<BrokerListingAssignment>? BrokersAssigned { get; set; }
  public string? URL { get; set; }
  public List<Lead>? LeadsGenerated { get; set; }

}

