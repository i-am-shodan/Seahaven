using Azure.AI.OpenAI;
using Newtonsoft.Json;
using Azure.Identity;
using Seahaven.Interfaces;

namespace Seahaven.Generation
{
    internal class AzureOpenAIPromptGenerator : IPromptBasedGenerator
    {
        private string DeploymentName = "gpt-3.5-turbo-16k";
        private string Key = string.Empty;
        private string URI = "";

        public void SetDeployment(string deployment)
        {
            DeploymentName = deployment;
        }

        public void SetKey(string key)
        {
            Key = key;
        }

        public void SetURI(string uri)
        {
            URI = uri;
        }

        private OpenAIClient GetClient()
        {
            if (string.IsNullOrWhiteSpace(Key))
            {
                return new OpenAIClient(new Uri(URI), new DefaultAzureCredential());
            }
            else if (!string.IsNullOrEmpty(URI) && !string.IsNullOrEmpty(Key))
            {
                return new OpenAIClient(new Uri(URI), new Azure.AzureKeyCredential(Key));
            }
            else
            {
                return new OpenAIClient(Key);
            }
        }

        public async Task<T> DeserializeResponseFromJson<T>(string prompt, int attempt = 0)
        {
            if (attempt > 3)
            {
                throw new Exception("Couldn't convince the AI to give us a valid response");
            }

            try
            {
                var client = GetClient();

                ChatCompletionsOptions cco = new ChatCompletionsOptions()
                {
                    Messages =
                    {
                        new ChatMessage(ChatRole.System, "You are tool that generates real world representative data for consumption by others tools."),
                        new ChatMessage(ChatRole.System, "Your output must only be a valid JSON document. Markdown, summary or preamble text is strictly not forbidden."),
                        new ChatMessage(ChatRole.System, "You must not describe your output in freetext, just output a single JSON document."),
                        new ChatMessage(ChatRole.System, "Your output must be suitable for a computer to parse"),
                        new ChatMessage(ChatRole.User, prompt.Trim()),
                    },
                    Temperature = 1.2f
                };

                var completionsResponse = await client.GetChatCompletionsAsync(DeploymentName, cco);

                string completion = completionsResponse.Value.Choices[0].Message.Content;

                completion = completion.Replace("```json", "").Replace("```", "").Trim();

                if (completion[0] != '{')
                {
                    completion = completion.Remove(0, completion.IndexOf("{"));
                }
                if (completion[completion.Length - 1] != '}')
                {
                    completion = completion.Remove(completion.LastIndexOf("}") + 1);
                }

                return JsonConvert.DeserializeObject<T>(completion);
            }
            catch
            {
                // try again
                return await DeserializeResponseFromJson<T>(prompt, attempt + 1);
            }
        }

        public async Task<string> GetResponseAsString(string prompt)
        {
            var client = GetClient();

            ChatCompletionsOptions cco = new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatMessage(ChatRole.System, "You are an AI assistant that generates real world representative data for testing"),
                    new ChatMessage(ChatRole.User, prompt),
                },
                Temperature = 1.2f
            };

            var completionsResponse = await client.GetChatCompletionsAsync(DeploymentName, cco);

            string completion = completionsResponse.Value.Choices[0].Message.Content;

            return completion;
        }
    }
}
