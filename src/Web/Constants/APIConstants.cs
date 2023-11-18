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
        "PropertyAddress,StreetAddress,Apartment,Language. Use 'null' for values that do not exist in the email text,except the Language." +
        " Else if the email is not from a lead,output exactly '{\"NotFound\":1}'.email: ";

    public const string MyNameIs = "My name is ";
    public const string IamBrokerWithEmail = ". I am a real-estate broker and my email address is ";

    public const string VeryStrictGPTPrompt = ". I received the email message mentioned below. I am only interested" +
        " in this email message if it contains information about a new lead who wishes to hire me as a real-estate broker, or a lead" +
        " who is inquiring about a property I listed for sale or rent. If this is the case, extract this lead's first name, " +
        "last name,phone number,email address,property address he/she inquired about and isolate this property's street address and apartment number. " +
        "Also determine the language the email is written in. Output and format these values as strictly JSON with keys: firstName," +
        "lastName,phoneNumber,emailAddress,PropertyAddress,StreetAddress,Apartment,Language. Use 'null' for values that do not exist in the email text," +
        " except the Language. Also use 'null' for values that you are not >95% confident about. If the email message is not about a new lead and therefore" +
        " not of interest to me, output exactly '{\"NotFound\":1}'.For example, if the email message is about a new lead, but this lead's email address" +
        " is not mentioned, then the value for key 'emailAddress' in your response shoud be 'null'. If the email message is about a communication,news letter,event or" +
        " advertisement that relates to real-estate, then it does not interest me and you should again output '{\"NotFound\":1}'." +
        " Here is the email message I received: ";

    public const string TranslateTemplatePrompt = "If the following email template is in French, translate it to" +
        "English and if it is in English, translate it to French. Do not change any words enclosed between '$' characters and conserve" +
        " the paragraph spacing of the original template in your translation.Only output the translated version and its language," +
        "and your output must be formatted strictly as one JSON object with" +
        "properties 'translatedtext' and 'translationlanguage', where 'translatedtext' is your translation and 'translationlanguage' is the language of the translated template." +
        "Here is the email template: ";

    public const string TranslateSubjectPrompt = "Translate the following email subject to" +
        " {0}. Do not change any words enclosed between '$' characters.Strictly output the translation. Here is the email subject: ";

    public const string NurturingFollowUpPrompt = "You're an AI real estate assistant who has been interacting with a potential lead via email. Generate a follow-up email based on the previous emails sent in the conversation flow. " +
        "Agent name - {agentName}, Agency Name - {agencyName}, Lead Full Name - {leadName}, Lead budget - {leadBudget}. Don't generate an email subject, only generate body and format it properly.";

    public const string NurturingInitialMessagePrompt = "You are a real estate agent write an initial email to a new lead based on the following format using the data provided: \"Hello, my name is [firstname] from [agency/team name]." +
        " I'm writing because you made a\r\nrequest for a property and I was wondering how long have you been looking for a property?\" and the following information we have about the lead making sure the email is short and concise" +
        "Agent name - {agentName}, Agency Name - {agencyName}, Lead Full Name - {leadName}, Lead budget - {leadBudget}. Don't generate an email subject, only generate body and format it properly.";

    public const string NurturingLeadAnalysisPrompt = "You're an AI real estate assistant who just finished interacting with a potential lead via email. Based on the conversation, determine if the lead's status is 0 (Hot), 1 (Cold), 2 (Slow), 3 (Active), or 4 (Dead). " +
        "Provide reasoning for the assigned status based on the flow and content of the conversation. Reply in the following json format - { \"FinalStatus\": <status number> }";

    public const string NurturingAskingQuestionPrompt = @"You're an AI real estate assistant who has been interacting with a potential lead via email, Based on the content and tone of the previous interaction, " +
        "select only a couple relevant questions from the list below, that have not yet been asked. Never repeat questions. Ask only a couple questions per email. " +
        "Then, craft an email response incorporating these questions to ensure the email flows naturally from the previous conversation and engages the lead further, " +
        "while maintaining a friendly and professional tone. Don't generate an email subject, only generate body and format it properly." +
        "Use the following broker name - {agentName}, and agency name - {agencyName} Lead Full Name - {leadName}, Lead budget - {leadBudget}." +
        "\n List of Questions:" +
        "\n1. 'How long have you been looking for a property?'" +
        "\n2. 'Could you let me know your timeline? When do you need to move into your new house?'" +
        "\n3. '(Name), is selling a current property necessary for you to buy the next one?'" +
        "\n4. 'Have you come across any houses that have caught your interest?'" +
        "\n5. 'Are you currently working with any other brokers?'" +
        "\n6. 'What price range are you considering for your new house?'" +
        "\n7. 'Could you share the amount of down payment you have planned for?'" +
        "\n8. 'Are you aware that in the current market, it can take one or two months to purchase a property and move in?'" +
        "\n9. 'Do you have a specific date by which you need to be in your new property, or are you ready to start the process now?'" +
        "\n10. 'To move forward towards finding your ideal property, shall we set up an appointment? Now, generate an email that includes the selected questions and builds on the previous conversation to effectively nurture the lead.";

    public const int PromptTokensCount = 142;
    public const int StrictPromptTokens = 320;

    public const string SealDealTenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";
}

