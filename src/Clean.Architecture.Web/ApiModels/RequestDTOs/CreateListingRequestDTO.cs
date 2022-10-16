﻿namespace Clean.Architecture.Web.ApiModels.RequestDTOs;

public class CreateListingRequestDTO
{
  public DateTime DateOfListing { get; set; }
  public int Price { get; set; }
  public Guid? BrokerId { get; set; }
  public string? URL { get; set; }
  public string Address { get; set; }
}
