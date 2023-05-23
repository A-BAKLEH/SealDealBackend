using Microsoft.Graph.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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

        /// <summary>
        /// if leadProvdier is null then email is from unknown sender
        /// </summary>
        /// <param name="emailbody"></param>
        /// <param name="leadProvider"></param>
        /// <returns></returns>
        public async Task<OpenAIResponse?> ParseEmailAsync(Message message, bool FromLeadProvider = false)
        {
            OpenAIResponse res;
            try
            {
                //TODO strip emailBOdy from unnecessary parts
                //HtmlDocument doc = new HtmlDocument();
                //doc.LoadHtml(message.Body.Content);
                //string EmailText = doc.DocumentNode.InnerText;
                var length = message.Body.Content.Length;

                string prompt = APIConstants.ParseLeadPrompt + message.Body.Content;

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
            catch (Exception e)
            {
                res = new OpenAIResponse
                {
                    HasLead = false,
                    ErrorMessage = e.Message,
                    ErrorType = e.GetType(),
                    ProcessedMessage = message
                };
                _logger.LogError("{Category} GPT 3.5 email parsing error for messageID {messageID} and error {Error}", "OpenAI", message.Id, e.Message);
            }
            return res;
        }
    }
}
