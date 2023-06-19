namespace Web.Constants;

public class APIConstants
{
    public const string ConvertToTimeZone = "ConvertToTimeZone";

    //sent email extended property id
    public const string APSentEmailExtendedPropId = "String {46920687-7fb5-49f0-963b-be637eec7ec0} Name SentByAPId";
    /// <summary>
    /// when value = 1 then needs reprocessing, 0 means success
    /// </summary>
    public const string ReprocessMessExtendedPropId = "String {40d29561-1bb1-4ca6-b6a5-6dc57b888ac9} Name ReprocessMessId";
    //email categories
    public const string NewLeadCreated = "SealDeal:LeadCreated";
    public const string SentBySealDeal = "SealDeal:SentByAutoFlow";
    public const string VerifyEmailAddress = "SealDeal:VerifyEmailAddress";
    public const string SeenOnSealDeal = "SeenOnSealDeal";

    //'Not Provided'
    //public const string ParseLeadPrompt3 = "If the email that follows is from a possible" +
    //    " real-estate lead and not an ad or promotional email, extract the lead's first name," +
    //    "last name,phone number,email address,property address he/she inquired about and isolate this property's street address and apartment number." +
    //    "Also determine the language the email is written in." +
    //    "Output and format these values as strictly JSON with keys: firstName,lastName,phoneNumber,emailAddress," +
    //    "PropertyAddress,StreetAddress,Apartment,Language. Only output values that you are sure about." +
    //    " Else if the email is not from a lead,output exactly '{\"NotFound\":1}'.email: ";

    public const string ParseLeadPrompt4 = "If the email that follows is from a possible" +
        " real-estate lead and not an ad or promotional email, extract the lead's first name," +
        "last name,phone number,email address,property address he/she inquired about and isolate this property's street address and apartment number." +
        "Also determine the language the email is written in." +
        "Output and format these values as strictly JSON with keys: firstName,lastName,phoneNumber,emailAddress," +
        "PropertyAddress,StreetAddress,Apartment,Language. Use 'null' for values that do not exist in the email text." +
        " Else if the email is not from a lead,output exactly '{\"NotFound\":1}'.email: ";

    public const string TranslateTemplatePrompt = "If the following email template is in French, translate it to" +
        "English and if it is in English, translate it to French. Do not change any words enclosed between '$' characters and conserve" +
        " the paragraph spacing of the original template in your translation.Only output the translated version and its language," +
        "and your output must be formatted strictly as one JSON object with" +
        "properties 'translatedtext' and 'translationlanguage', where 'translatedtext' is your translation and 'translationlanguage' is the language of the translated template." +
        "Here is the email template: ";

    public const string TranslateSubjectPrompt = "Translate the following email subject to" +
        " {0}. Do not change any words enclosed between '$' characters.Strictly output the translation. Here is the email subject: ";

    //property address of interest
    //(get 'StreetAddress' and 'Apartment' from 'PropertyAddress')
    public const int PromptTokensCount = 100; //TODO update later

    public const string SealDealTenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";
}

