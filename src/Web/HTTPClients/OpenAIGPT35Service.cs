using Core.Config.Constants.LoggingConstants;
using Core.Domain.LeadAggregate;
using Microsoft.Graph.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Web.Config;
using Web.Constants;

namespace Web.HTTPClients
{
    public class OpenAIGPT35Service
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenAIGPT35Service> _logger;
        public OpenAIGPT35Service(HttpClient httpClient, ILogger<OpenAIGPT35Service> logger)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/chat/completions");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "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");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _logger = logger;
        }

        public async Task TranslateTemplateAsync(string TemplateText)
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
                //var GPTCompletionJSON = rawResponse.choices[0].message.content.Replace("\n", "");
                var GPTCompletionJSON = rawResponse.choices[0].message.content;
                var templateTranslated = JsonSerializer.Deserialize<TemplateTranslationContent>(GPTCompletionJSON);

                //res = new OpenAIResponse
                //{
                //    Success = true,
                //    ProcessedMessage = message
                //};
                ////email doesnt contain lead
                //if (LeadParsed.NotFound == 1)
                //{
                //    res.HasLead = false;
                //    res.EmailTokensUsed = rawResponse.usage.prompt_tokens - APIConstants.PromptTokensCount;
                //}
                //else
                //{
                //    res.HasLead = true;
                //    res.content = LeadParsed;
               // }
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
                //_logger.LogError("{tag} GPT 3.5 email parsing error for messageID {messageID}" +
                //    " and brokerEmail {brokerEmail} and error {Error}", TagConstants.openAi, message.Id, brokerEmail, e.Message + e.StackTrace);
            }
        }
        /// <summary>
        /// if leadProvdier is null then email is from unknown sender
        /// </summary>
        /// <param name="emailbody"></param>
        /// <param name="leadProvider"></param>
        /// <returns></returns>
        public async Task<OpenAIResponse?> ParseEmailAsync(Message message,string brokerEmail, bool FromLeadProvider = false)
        {
            OpenAIResponse res;
            try
            {
                
                var length = message.Body.Content.Length;
                var text = message.Body.Content;
                text = EmailReducer.Reduce(text,message.From.EmailAddress.Address);
                string prompt = APIConstants.ParseLeadPrompt4 + text;

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
                    ProcessedMessage = message
                };
                //email doesnt contain lead
                if (LeadParsed.NotFound == 1)
                {
                    res.HasLead = false;
                    res.EmailTokensUsed = rawResponse.usage.prompt_tokens - APIConstants.PromptTokensCount;
                }
                else
                {
                    res.HasLead = true;
                    res.content = LeadParsed;
                }
            }
            catch(HttpRequestException e)
            {
                res = new OpenAIResponse
                {
                    HasLead = false,
                    ErrorMessage = e.Message,
                    ErrorType = e.GetType(),
                    ProcessedMessage = message
                };
                _logger.LogError("{tag} GPT 3.5 email parsing error for messageID {messageID}" +
                    " and brokerEmail {brokerEmail} and error {Error}", TagConstants.openAi, message.Id, brokerEmail,
                    e.Message + " code: " + e.StatusCode + " " + e.StackTrace);
            }
            catch (Exception e)
            {
                res = new OpenAIResponse
                {
                    HasLead = false,
                    ErrorMessage = e.Message,
                    ErrorType = e.GetType(),
                    ProcessedMessage = message
                };
                _logger.LogError("{tag} GPT 3.5 email parsing error for messageID {messageID}" +
                    " and brokerEmail {brokerEmail} and error {Error}", TagConstants.openAi, message.Id,brokerEmail, e.Message + e.StackTrace);
            }
            return res;
        }
    }
}
