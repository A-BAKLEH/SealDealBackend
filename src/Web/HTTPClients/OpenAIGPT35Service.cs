using HtmlAgilityPack;
using Microsoft.Graph;
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
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(message.Body.Content);
                string EmailText = doc.DocumentNode.InnerText;
                var length = EmailText.Length;

                string prompt = APIConstants.ParseLeadPrompt + EmailText;

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
                var rawResponse = await response.Content.ReadFromJsonAsync<GPT35RawResponse>();
                
                //lol change
                if (rawResponse.choices[0].message.content == "s")
                {
                    res = new OpenAIResponse();
                    res.Success = true;
                    res.HasLead = false;
                    res.PromptTokensUsed = rawResponse.usage.prompt_tokens;
                }
                else
                {
                   // res = rawResponse.choices[0].message.content;
                   res = new OpenAIResponse();
                }
            }
            catch (Exception e)
            {
                res = new OpenAIResponse();
                res.ErrorMessage = e.Message;
                _logger.LogError("{Category} GPT 3.5 email parsing error for messageID {messageID} and error {Error}","OpenAI",message.Id,e.Message);
            }

            return res;
        }
    }  
}
