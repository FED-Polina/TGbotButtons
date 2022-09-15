using System;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using System.Collections;
using System.Data;
using Microsoft.Data.Sqlite;
using NLog;
using Telegram.Bot.Types.Enums;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.IO;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Data.SqlClient;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotExperiments
{
    struct BotUpdate
    {
        public string text;
        public long id;
        public string? username;
    }

    class Program
    {
        static string kod = System.IO.File.ReadAllText(@"tokenTG.txt");
        static ITelegramBotClient bot = new TelegramBotClient(kod.ToString());
        private static Logger logger = LogManager.GetCurrentClassLogger();
        //static string fileName = "User.json";
        //static List<BotUpdate> botUpdates = new List<BotUpdate>();

        static void Main(string[] args)
        {
            Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.FirstName);

            logger.Debug("log {1}", "EventHandler"); //лог
            //Подключение SQLite
            CreateTable();
          
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            //подключение событий
            {
                AllowedUpdates = { }, //получать все типы обновлений
            };
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
            Console.ReadLine();

            //подключение БД
            using (var connection = new SqliteConnection("Data Source=Films.db"))
            {
                connection.Open();
            }
            Console.Read();
        }

        //public void SaveJson(object sender, EventArgs e)//Сохранение списка в файл Json
        //{
        //    DataContractJsonSerializer jsonList = new DataContractJsonSerializer(typeof(List<BotUpdate>));
        //    FileStream fileList = new FileStream("User.json", FileMode.Create);
        //    jsonList.WriteObject(fileList, botUpdates);
        //    fileList.Close();
        //    Console.WriteLine("Файл успешно сохранен!");
        //}

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Некоторые действия
            //logger.Debug("log {0}", "Start/Info/Help.Debug"); //лог
            Console.WriteLine(JsonConvert.SerializeObject(update));
            if (update.Type == UpdateType.Message)
            {
                var message = update.Message;
                InsertData(message);
                //Вывод в файл инфы о пользователях
                // System.IO.File.AppendAllText("user.txt", $"Message:{message.Text}, message_id:{message.MessageId}, FROMid:{message.From.Id}, FROMisBot:{message.From.IsBot}, date:{message.Date}\n");

                logger.Debug("log {0}", "Кнопка Start"); //лог
                if (message.Text.ToLower() == "/start")
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Добро пожаловать, юный киноман!");
                    return;
                }
                logger.Debug("log {0}", "Кнопка Info"); //лог
                if (message.Text.ToLower() == "/info")
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Моя задача помочь пользователю в подборке фильма.");
                    return;
                }
                logger.Debug("log {0}", "Кнопка Help"); //лог
                if (message.Text.ToLower() == "/help")
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Для работы с ботом необходимо воспользоваться кнопками: выбрать категорию жанр, затем сам фильм из предложенных.");
                    return;
                }
                logger.Debug("log {0}", "Кнопка Menu"); //лог
                if (message.Text.ToLower() == "/menu") //запуск кнопок
                {
                    var replyKeyboard_1 = new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton("Триллер"),
                        new KeyboardButton("Мелодрама"),
                        new KeyboardButton("Ужасы"),
                        new KeyboardButton("Комедии")
                    }
                    );
                    await bot.SendTextMessageAsync(message.From.Id, "Для работы с ботом необходимо воспользоваться кнопками: выбрать категорию жанр, затем сам фильм из предложенных.", replyMarkup: replyKeyboard_1);
                    return;
                }
                await botClient.SendTextMessageAsync(message.Chat, "Извините, я не могу Вас понять.");
            }
        }

        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }

        //Работа с БД для аналитики пользователей
        //Соединение с БД
        static SQLiteConnection CreateConnection()
        {
            SQLiteConnection sqlite_conn;
            //Create a new database connection:
            sqlite_conn = new SQLiteConnection("Data Source= users.db; Version = 3; New = True; Compress = True; ");
            // Open the connection:
            try
            {
                sqlite_conn.Open();
            }
            catch (Exception ex)
            {

            }
            return sqlite_conn;
        }

        //Создание таблиц
        static void CreateTable()
        {
            SQLiteConnection conn = CreateConnection();
            SQLiteCommand sqlite_cmd;
            string Createsql = "CREATE TABLE IF NOT EXISTS Users (Text text, ID INT, FromID INT, Bot boolean, Date string(40), Username string(30), Firstname string(25), Lastname string(25))";
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = Createsql;
            sqlite_cmd.ExecuteNonQuery();
        }

        //Вставка данных в таблицу
        static void InsertData(Message message)
        {
            SQLiteConnection conn = CreateConnection();
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = $"INSERT INTO Users (Text, ID, FromID, Bot, Date, Username, Firstname, Lastname) " +
                $"VALUES( '{message.Text}', {message.MessageId}, {message.From.Id}, {message.From.IsBot}, '{message.Date}', '{message.From.Username}', '{message.From.FirstName}', '{message.From.LastName}' ); ";
           sqlite_cmd.ExecuteNonQuery();
        }


    }

}
