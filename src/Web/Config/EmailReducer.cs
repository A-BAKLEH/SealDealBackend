namespace Web.Config;

public static class EmailReducer
{
    public static string Reduce(string text, string leadProvider)
    {
        if(leadProvider == "Lead@realtor.ca")
        {
            string pattern = @"Que pensez-vous de ce renvoi de client potentiel";
            var indexStart = text.IndexOf(pattern);
            if(indexStart != -1)
            {
                text = text.Substring(0, indexStart);
            }
        }
        return text;
    }
}
