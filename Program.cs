using APIGigaChat.Models;
using APIGigaChat.Models.Response;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace APIGigaChat
{
    public class Program
    {
        public static string ClientId = "019b2b50-ff41-77e2-b0f5-23e0ba29e4ef";
        public static string AutorizationKey = "MDE5YjJiNTAtZmY0MS03N2UyLWIwZjUtMjNlMGJhMjllNGVmOmZiYjEwNTdmLWM2ZmUtNDAwYS04NThjLTNlMTA2NjRmYTVkMA==";
        private static List<Request.Message> chatHistory = new List<Request.Message>();
        static async Task Main(string[] args)
        {
            string Token = await GetToken(ClientId, AutorizationKey);

            if(Token == null)
            {
                Console.WriteLine("не удалось получить токен");
                return;
            }
            Console.WriteLine("Диалог начат. Для выхода введите 'выход'.\n");
            while (true)
            {
                Console.Write("Вы: ");
                string userMessage = Console.ReadLine();

                if (userMessage.ToLower() == "выход")
                {
                    Console.WriteLine("Диалог завершен.");
                    break;
                }
                if (string.IsNullOrWhiteSpace(userMessage))
                    continue;

                // 1. Добавляем сообщение пользователя в историю
                chatHistory.Add(new Request.Message()
                {
                    role = "user",
                    content = userMessage
                });

                // 2. Получаем ответ от модели, передавая ВСЮ историю
                ResponseMessage Answer = await GetAnswer(Token, chatHistory);

                if (Answer == null || Answer.choices == null || Answer.choices.Count == 0)
                {
                    Console.WriteLine("Ошибка: не удалось получить ответ от GigaChat.");
                    // Убираем последнее сообщение пользователя из истории, так как на него не было ответа
                    chatHistory.RemoveAt(chatHistory.Count - 1);
                    continue;
                }

                string assistantReply = Answer.choices[0].message.content;
                Console.WriteLine($"GigaChat: {assistantReply}");

                // 3. Добавляем ответ ассистента в историю
                chatHistory.Add(new Request.Message()
                {
                    role = "assistant",
                    content = assistantReply
                });
            }
        }
        public static async Task<string> GetToken(string rqUID, string bearer)
        {
            string ReturnToken = null;
            string Url = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

            using (HttpClientHandler Handler = new HttpClientHandler())
            {
                Handler.ServerCertificateCustomValidationCallback = (message, sert, chain, SslPolicyErrors) => true;

                using (HttpClient Client = new HttpClient(Handler))
                {
                    HttpRequestMessage Request = new HttpRequestMessage(HttpMethod.Post, Url);

                    Request.Headers.Add("Accept", "application/json");
                    Request.Headers.Add("RqUID", rqUID);
                    Request.Headers.Add("Authorization", $"Bearer {bearer}");

                    var Data = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
                    };

                    Request.Content = new FormUrlEncodedContent(Data);

                    HttpResponseMessage Response = await Client.SendAsync(Request);

                    if (Response.IsSuccessStatusCode)
                    {
                        string ResponseContent = await Response.Content.ReadAsStringAsync();
                        ResponseToken Token = JsonConvert.DeserializeObject<ResponseToken>(ResponseContent);

                        ReturnToken = Token.access_token;
                    }
                }
            }
            return ReturnToken;
        }
        public static async Task<ResponseMessage> GetAnswer(string token, List<Request.Message> history)
        {
            ResponseMessage responseMessage = null;

            string Url = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

            using (HttpClientHandler Handler = new HttpClientHandler())
            {
                Handler.ServerCertificateCustomValidationCallback = (message, sert, chain, SslPolicyErrors) => true;

                using (HttpClient Client = new HttpClient(Handler))
                {
                    HttpRequestMessage HttpRequest = new HttpRequestMessage(HttpMethod.Post, Url);

                    HttpRequest.Headers.Add("Accept", "application/json");
                    HttpRequest.Headers.Add("Authorization", $"Bearer {token}");

                    Request DataRequest = new Request
                    {
                        model = "GigaChat",
                        stream = false,
                        repetition_penalty = 1,
                        messages = history
                    };

                    string JsonContent = JsonConvert.SerializeObject(DataRequest);

                    HttpRequest.Content = new StringContent(JsonContent, Encoding.UTF8, "application/json");

                    HttpResponseMessage Response = await Client.SendAsync(HttpRequest);

                    if (Response.IsSuccessStatusCode)
                    {
                        string ResponseContent = await Response.Content.ReadAsStringAsync();
                        responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(ResponseContent);
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка API: {Response.StatusCode}");
                    }
                }
            }
            return responseMessage;
        }
    }
}
