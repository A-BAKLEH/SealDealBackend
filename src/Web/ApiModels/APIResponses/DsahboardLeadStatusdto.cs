namespace Web.ApiModels.APIResponses;

public class DsahboardLeadStatusdto
{
    public int NewLeadsTotal { get; set; }
    public int newLeadsToday { get; set; }
    public int newLeadsYesterday { get; set; }
    public int activeLeads { get; set; }
    public int clientLeads { get; set; }
    public int deadLeads { get; set; }

}
