namespace Web.ControllerServices.StaticMethods;

public static class EmailHelpers
{
  //public static List<Option> GetDeltaQueryOptions(DateTimeOffset SyncStartDate)
  //{
  //  List<Option> options = new List<Option>();

  //  //var datee = SyncStartDate.ToString();
  //  var datee = SyncStartDate.ToString("o");
  //  //options.Add(new QueryOption("$select", "sender,isRead,conversationId,conversationIndex,createdDateTime"));
  //  options.Add(new QueryOption("$filter", $"receivedDateTime gt {datee}"));
  //  options.Add(new QueryOption("changeType", "created"));
  //  options.Add(new QueryOption("$orderby", "receivedDateTime desc"));
  //  return options;
  //}

  /// <summary>
  /// appends maxpageSize=20 and immutabeId to query options
  /// </summary>
  /// <param name="options"></param>
  //public static void AddDeltaHeaderOptions(this List<Option> options)
  //{
  //  options.Add(new HeaderOption("Prefer", "odata.maxpagesize=4"));
  //  options.Add(new HeaderOption("Prefer", "IdType=ImmutableId"));
  //}
  //public static List<Option> GetDeltaHeaderOptions()
  //{
  //  List<Option> options = new List<Option>();
  //  options.Add(new HeaderOption("Prefer", "odata.maxpagesize=3"));
  //  options.Add(new HeaderOption("Prefer", "IdType=ImmutableId"));
  //  return options;
  //}
}
