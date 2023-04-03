﻿namespace Web.HTTPClients
{
    public class OpenAIResponse
    {
        /// <summary>
        /// Response received without errors
        /// </summary>
        public bool Success { get; set; } = false;
        public bool HasLead { get; set; }
        public string? ErrorMessage { get; set; }
        public ResponseContent content { get; set; }
        public int? PromptTokensUsed { get; set; }
    }

    public class GPTRequest
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class GPT35RawResponse
    {
        public Usage usage { get; set; }
        public List<Choice> choices { get; set; }
        public class Usage
        {
            public int prompt_tokens { get; set; }
            public int completion_tokens { get; set; }
            public int total_tokens { get; set; }
        }
        public class Choice
        {
            public GPTMessage message { get; set; }
            public string finish_reason { get; set; }
        }
        public class GPTMessage
        {
            public string role { get; set; }
            public string content { get; set; }
        }
    }

    public class ResponseContent
    {
        public byte NotFound { get; set; } = 0;
        public string? firstName { get; set; }
        public string? lastName { get; set; }
        public string? emailAddress { get; set; }
        public string? phoneNumber { get; set; }
        public string? PropertyAddress { get; set; }
        public string? StreetNumber { get; set; }
        public string? Language { get; set; }
    }
}
