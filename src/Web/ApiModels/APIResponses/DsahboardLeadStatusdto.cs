namespace Web.ApiModels.APIResponses;

public class DsahboardLeadStatusdto
{
    public int NewLeadsToday { get; set; }
    public int NewLeadsYesterday { get; set; }
    public int HotLeads { get; set; }
    public int activeLeads { get; set; }
    public int slowLeads { get; set; }
    public int coldLeads { get; set; }
    public int closedLeads { get; set; }
    public int deadLeads { get; set; }

}
