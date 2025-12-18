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
        public static string ClientId = "";
        public static string AutorizationKey = "";
        static async Task Main(string[] args)
        {
            string Token = await GetToken(ClientId, AutorizationKey);

            if(Token == null)
            {
                Console.WriteLine("не удалось получить токен");
                return;
            }
            while (true)
            {
                Console.WriteLine("Сообщение");
                string Message = Console.ReadLine();

                ResponseMessage Answer = await GetAnswer(Token, Message);
                Console.WriteLine("Ответ: " + Answer.choices[0].message.content);
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
                    Request.Headers.Add("Authorization", $"Bearer{bearer}");

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
        public static async Task<ResponseMessage> GetAnswer(string token, string user_message)
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
                    HttpRequest.Headers.Add("Authorization", $"Bearer{token}");

                    Request DataRequest = new Request
                    {
                        model = "GigaChat",
                        stream = false,
                        repetition_penalty = 1,
                        messages = new List<Request.Message>()
                        {
                            new Request.Message()
                            {
                            role = "user",
                            content = user_message
                            }
                        }
                    };

                    string JsonContent = JsonConvert.SerializeObject(DataRequest);

                    HttpRequest.Content = new StringContent(JsonContent, Encoding.UTF8, "application/json");

                    HttpResponseMessage Response = await Client.SendAsync(HttpRequest);

                    if (Response.IsSuccessStatusCode)
                    {
                        string ResponseContent = await Response.Content.ReadAsStringAsync();
                        responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(ResponseContent);
                    }
                }
            }
            return responseMessage;
        }
    }
}
