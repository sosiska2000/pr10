using APIGigaChat.Models; // Использование моделей запросов из пространства имен APIGigaChat.Models
using APIGigaChat.Models.Response; // Использование моделей ответов из пространства имен APIGigaChat.Models.Response
using Newtonsoft.Json; // Использование библиотеки Newtonsoft.Json для сериализации/десериализации JSON
using System; // Использование базовых классов .NET (Console, Exception и т.д.)
using System.Collections.Generic; // Использование коллекций (List, Dictionary и т.д.)
using System.Net.Http; // Использование HttpClient для HTTP-запросов
using System.Runtime.Remoting.Messaging; // Пространство имен для удаленного взаимодействия (возможно не используется)
using System.Text; // Использование классов для работы с кодировкой (Encoding)
using System.Threading.Tasks; // Использование асинхронного программирования (Task, async/await)

namespace APIGigaChat // Объявление пространства имен для всего проекта
{
    public class Program // Основной класс программы
    {
        // Идентификатор клиента для авторизации в API GigaChat
        public static string ClientId = "019b2b50-ff41-77e2-b0f5-23e0ba29e4ef";

        // Ключ авторизации в формате base64 для получения токена доступа
        public static string AutorizationKey = "MDE5YjJiNTAtZmY0MS03N2UyLWIwZjUtMjNlMGJhMjllNGVmOmZiYjEwNTdmLWM2ZmUtNDAwYS04NThjLTNlMTA2NjRmYTVkMA==";

        // Список для хранения истории сообщений диалога (сообщения пользователя и ассистента)
        private static List<Request.Message> chatHistory = new List<Request.Message>();

        // Основной асинхронный метод - точка входа в программу
        static async Task Main(string[] args)
        {
            // Получение токена доступа для работы с API GigaChat
            string Token = await GetToken(ClientId, AutorizationKey);

            // Проверка успешности получения токена
            if (Token == null)
            {
                // Вывод сообщения об ошибке при неудачном получении токена
                Console.WriteLine("не удалось получить токен");
                return; // Завершение работы программы
            }

            // Вывод приветственного сообщения для начала диалога
            Console.WriteLine("Диалог начат. Для выхода введите 'выход'.\n");

            // Бесконечный цикл для поддержания диалога с пользователем
            while (true)
            {
                // Приглашение пользователя к вводу сообщения
                Console.Write("Вы: ");
                // Чтение введенного пользователем сообщения из консоли
                string userMessage = Console.ReadLine();

                // Проверка на команду выхода из диалога
                if (userMessage.ToLower() == "выход")
                {
                    // Вывод сообщения о завершении диалога
                    Console.WriteLine("Диалог завершен.");
                    break; // Выход из цикла while
                }
                // Проверка на пустое сообщение или состоящее только из пробелов
                if (string.IsNullOrWhiteSpace(userMessage))
                    continue; // Пропуск пустого сообщения и возврат к началу цикла

                // 1. Добавление сообщения пользователя в историю диалога
                chatHistory.Add(new Request.Message()
                {
                    role = "user", // Роль отправителя - пользователь
                    content = userMessage // Текст сообщения пользователя
                });

                // 2. Получение ответа от модели GigaChat с передачей всей истории диалога
                ResponseMessage Answer = await GetAnswer(Token, chatHistory);

                // Проверка на корректность полученного ответа
                if (Answer == null || Answer.choices == null || Answer.choices.Count == 0)
                {
                    // Вывод сообщения об ошибке при получении ответа
                    Console.WriteLine("Ошибка: не удалось получить ответ от GigaChat.");
                    // Удаление последнего сообщения пользователя из истории, так как на него не было получено ответа
                    chatHistory.RemoveAt(chatHistory.Count - 1);
                    continue; // Возврат к началу цикла для нового ввода
                }

                // Извлечение текста ответа ассистента из полученной структуры
                string assistantReply = Answer.choices[0].message.content;
                // Вывод ответа ассистента в консоль
                Console.WriteLine($"GigaChat: {assistantReply}");

                // 3. Добавление ответа ассистента в историю диалога для поддержания контекста
                chatHistory.Add(new Request.Message()
                {
                    role = "assistant", // Роль отправителя - ассистент
                    content = assistantReply // Текст ответа ассистента
                });
            }
        }

        // Метод для получения токена доступа к API GigaChat
        public static async Task<string> GetToken(string rqUID, string bearer)
        {
            string ReturnToken = null; // Переменная для хранения полученного токена
            string Url = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth"; // URL для получения токена

            // Создание обработчика HTTP-запросов с настройками
            using (HttpClientHandler Handler = new HttpClientHandler())
            {
                // Отключение проверки SSL-сертификата (не рекомендуется для продакшена)
                Handler.ServerCertificateCustomValidationCallback = (message, sert, chain, SslPolicyErrors) => true;

                // Создание HTTP-клиента с использованием настроенного обработчика
                using (HttpClient Client = new HttpClient(Handler))
                {
                    // Создание POST-запроса к URL получения токена
                    HttpRequestMessage Request = new HttpRequestMessage(HttpMethod.Post, Url);

                    // Добавление необходимых заголовков в запрос
                    Request.Headers.Add("Accept", "application/json"); // Ожидание ответа в формате JSON
                    Request.Headers.Add("RqUID", rqUID); // Уникальный идентификатор запроса
                    Request.Headers.Add("Authorization", $"Bearer {bearer}"); // Авторизационный токен

                    // Создание данных для отправки в теле запроса
                    var Data = new List<KeyValuePair<string, string>>
                    {
                        // Добавление параметра scope с указанием требуемых разрешений
                        new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
                    };

                    // Установка данных запроса в формате application/x-www-form-urlencoded
                    Request.Content = new FormUrlEncodedContent(Data);

                    // Асинхронная отправка HTTP-запроса и получение ответа
                    HttpResponseMessage Response = await Client.SendAsync(Request);

                    // Проверка успешности HTTP-запроса
                    if (Response.IsSuccessStatusCode)
                    {
                        // Чтение содержимого ответа в виде строки
                        string ResponseContent = await Response.Content.ReadAsStringAsync();
                        // Десериализация JSON-ответа в объект ResponseToken
                        ResponseToken Token = JsonConvert.DeserializeObject<ResponseToken>(ResponseContent);

                        // Извлечение токена доступа из объекта
                        ReturnToken = Token.access_token;
                    }
                }
            }
            // Возврат полученного токена (или null в случае ошибки)
            return ReturnToken;
        }

        // Метод для получения ответа от модели GigaChat
        public static async Task<ResponseMessage> GetAnswer(string token, List<Request.Message> history)
        {
            ResponseMessage responseMessage = null; // Переменная для хранения ответа от API

            string Url = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions"; // URL API для чата

            // Создание обработчика HTTP-запросов с настройками
            using (HttpClientHandler Handler = new HttpClientHandler())
            {
                // Отключение проверки SSL-сертификата (не рекомендуется для продакшена)
                Handler.ServerCertificateCustomValidationCallback = (message, sert, chain, SslPolicyErrors) => true;

                // Создание HTTP-клиента с использованием настроенного обработчика
                using (HttpClient Client = new HttpClient(Handler))
                {
                    // Создание POST-запроса к API чата
                    HttpRequestMessage HttpRequest = new HttpRequestMessage(HttpMethod.Post, Url);

                    // Добавление необходимых заголовков в запрос
                    HttpRequest.Headers.Add("Accept", "application/json"); // Ожидание ответа в формате JSON
                    HttpRequest.Headers.Add("Authorization", $"Bearer {token}"); // Авторизационный токен доступа

                    // Создание объекта запроса с параметрами для API
                    Request DataRequest = new Request
                    {
                        model = "GigaChat", // Используемая модель ИИ
                        stream = false, // Отключение потокового ответа
                        repetition_penalty = 1, // Коэффициент штрафа за повторения
                        messages = history // Передача истории сообщений для поддержания контекста
                    };

                    // Сериализация объекта запроса в JSON-строку
                    string JsonContent = JsonConvert.SerializeObject(DataRequest);

                    // Установка JSON-содержимого в тело запроса с указанием кодировки и типа контента
                    HttpRequest.Content = new StringContent(JsonContent, Encoding.UTF8, "application/json");

                    // Асинхронная отправка HTTP-запроса и получение ответа
                    HttpResponseMessage Response = await Client.SendAsync(HttpRequest);

                    // Проверка успешности HTTP-запроса
                    if (Response.IsSuccessStatusCode)
                    {
                        // Чтение содержимого ответа в виде строки
                        string ResponseContent = await Response.Content.ReadAsStringAsync();
                        // Десериализация JSON-ответа в объект ResponseMessage
                        responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(ResponseContent);
                    }
                    else
                    {
                        // Вывод информации об ошибке HTTP-запроса
                        Console.WriteLine($"Ошибка API: {Response.StatusCode}");
                    }
                }
            }
            // Возврат полученного ответа (или null в случае ошибки)
            return responseMessage;
        }
    }
}