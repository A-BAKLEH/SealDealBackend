using Core.Config.Constants.LoggingConstants;
using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate;
using Infrastructure.Migrations;
using Microsoft.Graph.Models.Security;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Twilio.Rest.Trunking.V1;
using Web.Config;
using Web.Constants;
using Web.Processing.Nurturing;
using static Web.Processing.EmailAutomation.EmailProcessor;
using MsftMessage = Microsoft.Graph.Models.Message;

namespace Web.HTTPClients;

public class OpenAIGPT35Service
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAIGPT35Service> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;
    public OpenAIGPT35Service(HttpClient httpClient, IConfiguration config,IWebHostEnvironment webHostEnvironment , ILogger<OpenAIGPT35Service> logger)
    {
        var key = config.GetSection("OpenAI")["APIKey"];
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/chat/completions");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _logger = logger;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<string> TranslateSubjectAsync(string subject, string targetLanguage)
    {
        try
        {
            string prompt = string.Format(APIConstants.TranslateSubjectPrompt, targetLanguage) + subject;

            StringContent jsonContent = new(
            JsonSerializer.Serialize(new
            {
                model = "gpt-3.5-turbo",
                messages = new List<GPTRequest>
                {
            new GPTRequest{role = "user", content = prompt},
                },
                temperature = 0,
            }),
            Encoding.UTF8,
            "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync("", content: jsonContent);

            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var rawResponse = JsonSerializer.Deserialize<GPT35RawResponse>(jsonResponse);
            var GPTCompletion = rawResponse.choices[0].message.content.Replace("\n", "").Trim();
            return GPTCompletion;
        }
        catch (HttpRequestException e)
        {
            //res = new OpenAIResponse
            //{
            //    HasLead = false,
            //    ErrorMessage = e.Message,
            //    ErrorType = e.GetType(),
            //    ProcessedMessage = message
            //};
            //_logger.LogError("{tag} GPT 3.5 email parsing error for messageID {messageID}" +
            //    " and brokerEmail {brokerEmail} and error {Error}", TagConstants.openAi, message.Id, brokerEmail,
            //    e.Message + " code: " + e.StatusCode + " " + e.StackTrace);
        }
        return null;
    }
    public async Task<TemplateTranslationContent> TranslateTemplateAsync(string TemplateText)
    {
        try
        {
            string prompt = APIConstants.TranslateTemplatePrompt + TemplateText;

            StringContent jsonContent = new(
            JsonSerializer.Serialize(new
            {
                model = "gpt-3.5-turbo",
                messages = new List<GPTRequest>
                {
            new GPTRequest{role = "user", content = prompt},
                },
                temperature = 0,
            }),
            Encoding.UTF8,
            "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync("", content: jsonContent);

            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var rawResponse = JsonSerializer.Deserialize<GPT35RawResponse>(jsonResponse);
            var GPTCompletionJSON = rawResponse.choices[0].message.content.Replace("\n", "");
            var templateTranslated = JsonSerializer.Deserialize<TemplateTranslationContent>(GPTCompletionJSON);
            return templateTranslated;
        }
        catch (HttpRequestException e)
        {
            //res = new OpenAIResponse
            //{
            //    HasLead = false,
            //    ErrorMessage = e.Message,
            //    ErrorType = e.GetType(),
            //    ProcessedMessage = message
            //};
            //_logger.LogError("{tag} GPT 3.5 email parsing error for messageID {messageID}" +
            //    " and brokerEmail {brokerEmail} and error {Error}", TagConstants.openAi, message.Id, brokerEmail,
            //    e.Message + " code: " + e.StatusCode + " " + e.StackTrace);
        }
        catch (Exception e)
        {
            //res = new OpenAIResponse
            //{
            //    HasLead = false,
            //    ErrorMessage = e.Message,
            //    ErrorType = e.GetType(),
            //    ProcessedMessage = message
            //};
            _logger.LogError("{tag} GPT 3.5 template translation error {error}", TagConstants.openAiTranslation, e.Message + " \n" + e.StackTrace);
        }
        return null;
    }
    /// <summary>
    /// if leadProvdier is null then email is from unknown sender
    /// </summary>
    /// <param name="emailbody"></param>
    /// <param name="leadProvider"></param>
    /// <returns></returns>
    public async Task<OpenAIResponse?> ParseEmailAsync(MsftMessage? msftMessage, GmailMessageDecoded? gmailMessage, string brokerEmail, string brokerFirstName, string brokerLastName, bool FromLeadProvider = false)
    {
        OpenAIResponse res;
        int length = 0;
        string sender = "";
        string messId = gmailMessage?.message.Id ?? msftMessage.Id;
        try
        {
            string text = "";
            if (gmailMessage == null)
            {
                text = msftMessage.Body.Content;
            }
            else
            {
                text = gmailMessage.textBody;
            }
            sender = gmailMessage?.From ?? msftMessage?.From.EmailAddress.Address;
            text = EmailReducer.Reduce(text, sender, _webHostEnvironment.IsDevelopment());
            length = text.Length;

            var input = APIConstants.MyNameIs + brokerFirstName + " " + brokerLastName + APIConstants.IamBrokerWithEmail
                + brokerEmail + APIConstants.VeryStrictGPTPrompt + text;

            string prompt = input;

            StringContent jsonContent = new(
            JsonSerializer.Serialize(new
            {
                model = "gpt-3.5-turbo",
                messages = new List<GPTRequest>
                {
            new GPTRequest{role = "user", content = prompt},
                },
                temperature = 0,
            }),
            Encoding.UTF8,
            "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync("", content: jsonContent);
            //TODO handle API error 
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var rawResponse = JsonSerializer.Deserialize<GPT35RawResponse>(jsonResponse);
            var GPTCompletionJSON = rawResponse.choices[0].message.content.Replace("\n", "");
            var LeadParsed = JsonSerializer.Deserialize<LeadParsingContent>(GPTCompletionJSON);

            res = new OpenAIResponse
            {
                Success = true,
                ProcessedMessageMSFT = msftMessage,
                ProcessedMessageGMAIL = gmailMessage,
                EmailTokensUsed = rawResponse.usage.prompt_tokens - APIConstants.StrictPromptTokens
            };
            if (LeadParsed.NotFound == 1)
            {
                res.HasLead = false;
            }
            else
            {
                res.HasLead = true;
                res.content = LeadParsed;
            }
            if (GlobalControl.LogAllEmailsLengthsOpenAi)
                _logger.LogInformation("{tag} text length messageID {messageID}" +
                                           " and brokerEmail {brokerEmail} from sender {senderEmail}, hasLead is {hasLead}, length {messLength}",
                                           TagConstants.openAi, messId, brokerEmail, sender, res.HasLead, length);
        }
        catch (HttpRequestException e)
        {
            res = new OpenAIResponse
            {
                HasLead = false,
                ErrorMessage = e.Message,
                ErrorType = e.GetType(),
                ProcessedMessageGMAIL = gmailMessage,
                ProcessedMessageMSFT = msftMessage
            };
            _logger.LogError("{tag} GPT 3.5 httpexception, email parsing error for messageID {messageID}" +
                " and brokerEmail {brokerEmail} from sender {senderEmail} and length {messLength} and error {Error}", TagConstants.openAi, messId, brokerEmail,
                sender, length, " code: " + e.StatusCode);
        }
        catch (Exception e)
        {
            res = new OpenAIResponse
            {
                HasLead = false,
                ErrorMessage = e.Message,
                ErrorType = e.GetType(),
                ProcessedMessageGMAIL = gmailMessage,
                ProcessedMessageMSFT = msftMessage
            };
            _logger.LogError("{tag} GPT 3.5 exception, email parsing error for messageID {messageID}" +
                " and brokerEmail {brokerEmail} from sender {senderEmail} and length {messLength} and error {Error}", TagConstants.openAi, messId, brokerEmail,
                sender, length,
                e.Message + e.StackTrace);
        }
        if (GlobalControl.LogOpenAIEmailParsingObjects)
        {
            if (res.content == null) _logger.LogWarning("{tag} returning content object null,haslead {hasLead}, success {success}", TagConstants.openAi, res.HasLead, res.Success);
            else _logger.LogWarning("{tag} returning content object: {@openAiContent},haslead {hasLead}, success {success}", TagConstants.openAi, res.content, res.HasLead, res.Success);
        }

        return res;
    }

    public async Task<OpenAIResponse> ProccessAINurturing(NurturingProcessingType eventType, Broker brokerInfo, Lead leadInfo, List<InputEmail> emails = null, string emailsText = null)
    {
        try
        {
            string prompt = String.Empty;

            switch (eventType)
            {
                case NurturingProcessingType.FollowUp:
                    prompt = APIConstants.NurturingFollowUpPrompt;
                    prompt = prompt.Replace("{agentName}", $"{brokerInfo.FirstName} {brokerInfo.LastName}");
                    prompt = prompt.Replace("{agencyName}", $"{brokerInfo.Agency.AgencyName}");
                    prompt = prompt.Replace("{leadName}", $"{leadInfo.LeadFirstName} {leadInfo.LeadLastName}");
                    prompt = prompt.Replace("{leadBudget}", $"{leadInfo.Budget}");
                    break;

                case NurturingProcessingType.AskingQuestions:
                    prompt = APIConstants.NurturingAskingQuestionPrompt;
                    prompt = prompt.Replace("{agentName}", $"{brokerInfo.FirstName} {brokerInfo.LastName}");
                    prompt = prompt.Replace("{agencyName}", $"{brokerInfo.Agency.AgencyName}");
                    prompt = prompt.Replace("{leadName}", $"{leadInfo.LeadFirstName} {leadInfo.LeadLastName}");
                    prompt = prompt.Replace("{leadBudget}", $"{leadInfo.Budget}");
                    break;

                case NurturingProcessingType.SendingInitialMessage:
                    prompt = APIConstants.NurturingInitialMessagePrompt;
                    prompt = prompt.Replace("{agentName}", $"{brokerInfo.FirstName} {brokerInfo.LastName}");
                    prompt = prompt.Replace("{agencyName}", $"{brokerInfo.Agency.AgencyName}");
                    prompt = prompt.Replace("{leadName}", $"{leadInfo.LeadFirstName} {leadInfo.LeadLastName}");
                    prompt = prompt.Replace("{leadBudget}", $"{leadInfo.Budget}");
                    break;

                case NurturingProcessingType.AnalysingLead:
                    prompt = APIConstants.NurturingLeadAnalysisPrompt;
                    break;
            }

            var gptRequestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new List<GPTRequest>(),
                temperature = 0.2,
            };

            gptRequestBody.messages.Add(
                GeneratePromptMessage(prompt));

            if (eventType != NurturingProcessingType.SendingInitialMessage)
            {
                var conversationHistory = GenerateEmailConversationHistory(emails, emailsText);
                gptRequestBody.messages.AddRange(conversationHistory);
            }
            
            var jsonBody = JsonSerializer.Serialize(gptRequestBody);
            var requestContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(String.Empty, requestContent);

            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var rawResponse = JsonSerializer.Deserialize<GPT35RawResponse>(jsonResponse);
            var reply = rawResponse.choices.First().message.content;

            var openAIResponse = new OpenAIResponse();
            openAIResponse.Success = true;

            if (eventType != NurturingProcessingType.AnalysingLead)
            {
                openAIResponse.TextReply = reply;
            }
            else
            {
                var leadAnalysisResult = JsonSerializer.Deserialize<NurturingResult>(reply);
                openAIResponse.LeadAnalysis = leadAnalysisResult;
            }

            return openAIResponse;
        }
        catch (Exception e)
        {
            var openAIResponse = new OpenAIResponse()
            {
                Success = false,
                ErrorMessage = e.Message,
                ErrorType = e.GetType()
            };

            _logger.LogError("{tag} GPT 3.5 follow up message error {error}", TagConstants.openAiNurturingFollowUp, e.Message + " \n" + e.StackTrace);

            return openAIResponse;
        }
    }

    public GPTRequest GeneratePromptMessage(string prompt)
    {
        return new GPTRequest
        {
            role = "system",
            content = prompt
        };
    }

    public List<GPTRequest> GenerateEmailConversationHistory(List<InputEmail> emails, string emailsText)
    {
        List<GPTRequest> conversationHistory = new List<GPTRequest>();

        if (emails == null && emailsText == null)
        {
            return conversationHistory;
        }

        if (!String.IsNullOrEmpty(emailsText))
        {
            conversationHistory.Add(new GPTRequest()
            {
                role = "user",
                content = emailsText
            });

            return conversationHistory;
        }

        foreach (var email in emails)
        {
            GPTRequest message = new GPTRequest();

            switch (email.Type)
            {
                case NurturningEmailType.AIMessage:
                    message.role = "assistant";
                    break;
                case NurturningEmailType.LeadMessage:
                    message.role = "user";
                    break;
            }

            message.content = email.Content;
            conversationHistory.Add(message);
        }

        return conversationHistory;
    }
}
