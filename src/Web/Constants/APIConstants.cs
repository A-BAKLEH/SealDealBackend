using Infrastructure.Migrations;
using Stripe;

namespace Web.Constants;

public class APIConstants
{
    public const string ConvertToTimeZone = "ConvertToTimeZone";

    //sent email extended property id
    public const string APSentEmailExtendedPropId = "String {46920687-7fb5-49f0-963b-be637eec7ec0} Name SentTagId";

    //email categories
    public const string LeadCreated = "LeadCreated";
    public const string SentBySealDeal = "SentBySealDeal";
    public const string SeenOnSealDeal = "SeenOnSealDeal";

    //gpt prompts
    //public const string LeadProviderPrompt = "if email contains a lead, extract lead's first name,last name,language of correspondence,phone number,email address,property address of interest;and format output as firstName,lastName,Language,phoneNumber,emailAddress,PropertyAddress,StreetNumber; and Display 'null' for unavailable info (get 'StreetNumber' from 'PropertyAddress'). Else output 'no'.email: ";
    //public const string LeadProviderPrompt = "if email contains a lead, extract lead's first name,last name,language used in email,phone number,email address,property address of interest;and format output as JSON with properties: firstName,lastName,Language,phoneNumber,emailAddress,PropertyAddress,StreetNumber (get 'StreetNumber' from 'PropertyAddress'). Else output '{\"NotFound\":1}'.email: ";
    public const string ParseLeadPrompt = "if email contains an inquiry from a possible real-estate lead (not an ad or promotional email), extract lead's first name,last name,language used in email,phone number,email address,property address of interest;and format output as JSON with properties: firstName,lastName,Language,phoneNumber,emailAddress,PropertyAddress,StreetAddress,Apartment (get 'StreetAddress' and 'Apartment' from 'PropertyAddress'). Else output '{\"NotFound\":1}'.email: ";
    public const int PromptTokensCount = 100; //TODO update later
}

