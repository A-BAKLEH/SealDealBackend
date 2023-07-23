namespace Web.ApiModels.RequestDTOs.Google;

public class GoogleDTOs
{

}
public class CodeSendingDTO
{
    public string code { get; set; }
}
public class GoogleResDTO
{
    public string access_token { get; set; }
    public int expires_in { get; set; }
    public string refresh_token { get; set; }
    public string scope { get; set; }
    public string token_type { get; set; }
}

public class GoogleProfileDTO
{
    public string emailAddress { get; set; }
}