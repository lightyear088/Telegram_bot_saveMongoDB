using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using MongoDB.Driver;
using Telegram.Bot.Examples.WebHook.BD;
using Telegram.Bot.Examples.WebHook.BDUsers;
using System.Globalization;
using System;

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualBasic;
using static System.Net.Mime.MediaTypeNames;

namespace Telegram.Bot.Services;
public class UpdateHandlers
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandlers> _logger;

    public UpdateHandlers(ITelegramBotClient botClient, ILogger<UpdateHandlers> logger)
    {
        _botClient = botClient;
        _logger = logger;
    }

#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable RCS1163 // Unused parameter.
    public Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
#pragma warning restore RCS1163 // Unused parameter.
#pragma warning restore IDE0060 // Remove unused parameter
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);
        return Task.CompletedTask;
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {

        var handler = update switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
            { EditedMessage: { } message } => BotOnMessageReceived(message, cancellationToken),
            { CallbackQuery: { } callbackQuery } => BotOnCallbackQueryReceived(callbackQuery, cancellationToken),
            { InlineQuery: { } inlineQuery } => BotOnInlineQueryReceived(inlineQuery, cancellationToken),
            { ChosenInlineResult: { } chosenInlineResult } => BotOnChosenInlineResultReceived(chosenInlineResult, cancellationToken),
            _ => UnknownUpdateHandlerAsync(update, cancellationToken)
        };

        await handler;
    }
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {


        if (callbackQuery.Data.StartsWith("page"))
        {
            string[] slovar = callbackQuery.Data.Split(' ');
            int page = Int32.Parse(slovar[1]);
            int len = Int32.Parse(slovar[3]);
            if (slovar[2] == "forward")
            {
                page += 1;
                if (page == len - 1)
                    PageTop(callbackQuery, _botClient, page);
                else
                    PageMid(callbackQuery, _botClient, page);

            }
            else if (slovar[2] == "back")
            {
                page -= 1;
                if (page == 0)
                    PageBot(callbackQuery, _botClient, page);
                else
                    PageMid(callbackQuery, _botClient, page);
            }
        }
        else if (callbackQuery.Data.StartsWith("delete"))
        {
            string[] slovar = callbackQuery.Data.Split(' ');
            int page = Int32.Parse(slovar[1]);
            DeletePage(callbackQuery, _botClient, page);
        }
        else if (callbackQuery.Data.StartsWith("accept"))
        {
            string[] slovar = callbackQuery.Data.Split(' ');
            string owner = slovar[1];

            AnswerAccept(callbackQuery, _botClient, owner);
            await _botClient.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
        }
        else if (callbackQuery.Data.StartsWith("refuse"))
        {
            string[] slovar = callbackQuery.Data.Split(' ');
            string owner = slovar[1];

            AnswerRefuse(callbackQuery, _botClient, owner);
            await _botClient.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
        }

    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        try
        {


            _logger.LogInformation("Receive message type: {MessageType}", message.Type);
            if (message.Text is not { } messageText)
                return;

            string nick = message.From.Username;
            var chat_id = message.Chat.Id;
            var collection = GetDBTable<MongoDBTemp>();
            var filter = Builders<MongoDBTemp>.Filter.Eq("NickName", nick);
            var session = collection.Find(filter).FirstOrDefault();


            if (session == null)
            {
                session = await CreateBd(nick, chat_id);
            }

            if (message.Text == "/menu")
            {
                var action = messageText.Split(' ')[0] switch
                {
                    _ => Usage(_botClient, message, cancellationToken)
                };
                Message sentMessage = await action;
                _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);
            }
            else if (message.Text == "/Cancel")
            {
                Cancel(_botClient, message, cancellationToken);
            }

            else if (message.Text == "/create_event")
            {

                var updatestatus = Builders<MongoDBTemp>.Update.Set("Status", "being_created");

                collection.UpdateOne(filter, updatestatus);
                session.Status = "being_created";

                СreateEvent(_botClient, message, cancellationToken, messageText);
            }
            else if (message.Text == "/delete_event")
            {
                DeleteEvent(_botClient, message, cancellationToken, messageText);
            }

            else if (session.Status == "being_created")
            {
                СreateEvent(_botClient, message, cancellationToken, messageText);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"укажите описание события {ex.Message}");

        }
    }

    static async Task<Message> СreateEvent(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, string messageText)

    {

        /*
            await botClient.SendChatActionAsync(
            message.Chat.Id,
            ChatAction.Typing,
            cancellationToken: cancellationToken);
        */

        string nick = message.From.Username;
        var collection = GetDBTable<MongoDBTemp>();
        var filter = Builders<MongoDBTemp>.Filter.Eq("NickName", nick);

        int test = collection.Find(filter).FirstOrDefault().Step;
        var stat = collection.Find(filter).FirstOrDefault().Status;

        if (stat == "being_created")
        {
            if (test == null)
            {
                int step = 4;

                return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,

                text: $"0{messageText} {test}",
                cancellationToken: cancellationToken);
            }
            else if (test == 0)
            {
                int step = 4;
                string status = "";
                string guests = messageText;
                RemoveRequestContactButton(botClient, message);

                UpdateBd(step, status, nick);
                UpdateBDGuests(guests, nick);

                UserNotification(botClient, message, cancellationToken, guests);
                UpdateBDNumber(nick);

                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"событие успешно создано",
                    cancellationToken: cancellationToken);
            }
            else
            {
                int step = test - 1;
                string status = "being_created";

                UpdateBd(step, status, nick);

                switch (step)
                {
                    case 3:
                        bool statuskeyboard = true;
                        KeyBoard(botClient, message, statuskeyboard);
                        FillingName(message, botClient);
                        CreateBdStorage(nick);

                        return await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: $"",
                        cancellationToken: cancellationToken);

                    case 2:
                        string namebd = messageText;
                        FillingDate(message, botClient);
                        UpdateBDName(namebd, nick);

                        return await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "",
                        cancellationToken: cancellationToken);

                    case 1:
                        string databd = messageText;
                        DateTime dt = DateTime.ParseExact(databd, "dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
                        if (DateTime.TryParse(databd, out dt))
                        {
                            if (dt < DateTime.Now)
                            {
                                botClient.SendTextMessageAsync(message.Chat.Id, text: "Заданнное время уже прошло, укажите будущие значения времени и даты");
                                step = 2;
                                UpdateBd(step, status, nick);
                            }
                            else
                            {
                                UpdateBDate(databd, nick);
                                FillingDescription(message, botClient);
                            }
                        }
                        else
                        {
                            botClient.SendTextMessageAsync(message.Chat.Id, text: "Ой, неправильный формат даты, пожалуйста повторите попытку по примеру шаблона(20.11.2024 17:01)");
                            step = 2;
                            UpdateBd(step, status, nick);
                        }
                        return await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "",
                        cancellationToken: cancellationToken);

                    case 0:
                        string description = messageText;

                        UpdateBDdescription(description, nick);
                        FillingGuests(message, botClient);

                        return await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "",
                        cancellationToken: cancellationToken);

                    default:
                        return await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "",
                        cancellationToken: cancellationToken);
                }
            }
        }
        else
        {
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"stop{messageText} {test}",
                cancellationToken: cancellationToken);
        }
;
    }

    static async Task<Message> DeleteEvent(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, string messageText)
    {


        Сreating_Representation(message, botClient);


        return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "",
                cancellationToken: cancellationToken);

    }

    static async Task<Message> MyEvents(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {


        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Removing keyboard",
            cancellationToken: cancellationToken);
    }

    static async Task<Message> Cancel(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        string nick = message.From.Username;
        RemoveRequestContactButton(botClient, message);
        var collection = GetDBTable<MongoDBTemp>();
        var collection1 = GetDBTable1<MongoDBStorage>();
        var filter = Builders<MongoDBTemp>.Filter.Eq("NickName", nick);
        var update = Builders<MongoDBTemp>.Update.Set("Status", "");
        var update1 = Builders<MongoDBTemp>.Update.Set("Step", 4);


        collection.UpdateOne(filter, update);
        collection.UpdateOne(filter, update1);

        var filter1 = Builders<MongoDBStorage>.Filter.Where(x => x.NickName == nick && x.Number == true);

        await collection1.FindOneAndDeleteAsync(filter1);


        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "",
            cancellationToken: cancellationToken);
    }

    static async Task<Message> UserNotification(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, string guests)
    {

        string nick = message.From.Username;
        var collection1 = GetDBTable1<MongoDBStorage>();
        var filter1 = Builders<MongoDBStorage>.Filter.Where(x => x.Owner == nick);
        var all_data1 = await collection1.Find(filter1).ToListAsync();
        var collection = GetDBTable<MongoDBTemp>();

        var differentname = all_data1[all_data1.Count - 1].Name;

        List<string> people = new List<string>();
        foreach (string guest in guests.Split(' '))
        {
            if (guest.StartsWith("https://t.me/"))
            {
                people.Add(guest.Substring(13));

            }
            else if (guest.StartsWith("@"))
            {

                people.Add(guest.Substring(1));

            }
            else
            {
                people.Add(guest);
            }
        }
        foreach (string user in people)
        {
            var filter = Builders<MongoDBTemp>.Filter.Eq(x => x.NickName, user);
            var all_data = await collection.Find(filter).ToListAsync();
            if (all_data.Count == 0)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Пользователь {user} ещё не запускал этого бота, для уведомлений пользователя попросите его активировать бота и повторите регистрацию события");
            }
            else
            {
                var chat_id = all_data[0].ChatId;
                InlineKeyboardMarkup keyboard = new(new[]
{
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "я участвую!", callbackData: $"accept {message.Chat.Id}"),
                    InlineKeyboardButton.WithCallbackData(text: "я отказываюсь", callbackData: $"refuse {message.Chat.Id}")
                }
                });
                await botClient.SendTextMessageAsync(chat_id, $"{nick} создал событие '{differentname}'", replyMarkup: keyboard);
            }
        }






        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "",
            cancellationToken: cancellationToken);
    }

    static async Task<Message> Usage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        const string usage = "Usage:\n" +
                             "/create_event     - создание события\n" +
                             "/delete_event     - удаления события\n" +
                             "/all_events       - все  события\n";



        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: usage,
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }



    async static Task KeyBoard(ITelegramBotClient botClient, Message message, bool statuskeyboard)
    {
        ReplyKeyboardMarkup keyboard = new(new[]
                            {
                                new KeyboardButton[] {"/Cancel"}
                            })
        {
            ResizeKeyboard = statuskeyboard
        };
        botClient.SendTextMessageAsync(message.Chat.Id, text: "вы всегда можете остановить создание, нажав на отмену внизу", replyMarkup: keyboard);
    }
    static async Task RemoveRequestContactButton(ITelegramBotClient botClient, Message message)
    {
        var tgBot = botClient;

        await tgBot.SendTextMessageAsync(message.Chat.Id, "Заполнение завершено", replyMarkup: new ReplyKeyboardRemove()).ConfigureAwait(false);
    }

    async static Task<MongoDBTemp> CreateBd(string nick, long chat_id)
    {
        string connectionString = "mongodb://localhost:27017";
        string dataBaSeName = "Temp";
        string collectionName = "Data";

        var client = new MongoClient(connectionString);
        var db = client.GetDatabase(dataBaSeName);
        var collection = db.GetCollection<MongoDBTemp>(collectionName);
        var info = new MongoDBTemp { NickName = nick, Step = 4, Status = "being_created", ChatId = chat_id };


        await collection.InsertOneAsync(info);
        return info;
    }

    async static Task<MongoDBStorage> CreateBdStorage(string nick)
    {
        string connectionString = "mongodb://localhost:27017";
        string dataBaSeName = "Storage";
        string collectionName = "Da";

        var client = new MongoClient(connectionString);
        var db = client.GetDatabase(dataBaSeName);
        var storage = db.GetCollection<MongoDBStorage>(collectionName);
        var info = new MongoDBStorage { NickName = nick, Number = true, Owner = nick };

        await storage.InsertOneAsync(info);
        return info;
    }

    async static Task UpdateBd(int step, string status, string nick)
    {
        var collection = GetDBTable<MongoDBTemp>();
        var filter = Builders<MongoDBTemp>.Filter.Eq("NickName", nick);
        var update = Builders<MongoDBTemp>.Update.Set("Step", step);
        var updatestatus = Builders<MongoDBTemp>.Update.Set("Status", status);

        collection.UpdateOne(filter, update);
        collection.UpdateOne(filter, updatestatus);
    }

    async static Task UpdateBDNumber(string nick)
    {
        var collection = GetDBTable1<MongoDBStorage>();
        var find_object = collection.FindOneAndUpdate(Builders<MongoDBStorage>.Filter.Where(x => x.Owner == nick && x.Number == true), Builders<MongoDBStorage>.Update.Set(y => y.Number, false));

    }
    async static Task UpdateBDName(string namebd, string nick)
    {
        var collection = GetDBTable1<MongoDBStorage>();
        var find_object = collection.FindOneAndUpdate(Builders<MongoDBStorage>.Filter.Where(x => x.Owner == nick && x.Number == true), Builders<MongoDBStorage>.Update.Set(y => y.Name, namebd));

    }
    async static Task UpdateBDate(string databd, string nick)
    {
        DateTime dt = DateTime.ParseExact(databd, "dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);

        var collection = GetDBTable1<MongoDBStorage>();
        var find_object = collection.FindOneAndUpdate(Builders<MongoDBStorage>.Filter.Where(x => x.NickName == nick && x.Number == true), Builders<MongoDBStorage>.Update.Set(y => y.Date, dt));

    }
    async static Task UpdateBDGuests(string guests, string nick)
    {

        string user = null;
        foreach (string guest in guests.Split(' '))
        {
            if (guest.StartsWith("https://t.me/"))
            {
                if (user == null)
                {
                    user = guest.Substring(13);
                }
                else
                {
                    user = user + " " + guest.Substring(13);
                }

            }
            else if (guest.StartsWith("@"))
            {
                if (user == null)
                {
                    user = guest.Substring(1);
                }
                else
                {
                    user = user + " " + guest.Substring(1);
                }

            }
            else
            {
                if (user == null)
                {
                    user = guest;
                }
                else
                {
                    user = user + " " + guest;
                }
            }
        }

        var collection = GetDBTable1<MongoDBStorage>();
        var existingData = await collection
                .Find(x => x.NickName == nick && x.Number == true)
                .FirstOrDefaultAsync();

        if (existingData != null)
        {
            string[] users = user.Split(' ');
            var userobj = users.Select(x => new UserInfo()
            {
                Agreement = "Ожидание ответа",
                ForeName = x
            });

            existingData.Guests = userobj.ToList();

            await collection.ReplaceOneAsync(x => x.Id.Equals(existingData.Id), existingData);

        }
        //UpdateBDNumber(nick);

    }

    async static Task UpdateBDdescription(string descriptionbd, string nick)
    {
        var collection = GetDBTable1<MongoDBStorage>();
        var find_object = collection.FindOneAndUpdate(Builders<MongoDBStorage>.Filter.Where(x => x.NickName == nick && x.Number == true), Builders<MongoDBStorage>.Update.Set(y => y.Description, descriptionbd));

    }
    async static Task FillingName(Message message, ITelegramBotClient botClient)
    {
        await botClient.SendTextMessageAsync(message.Chat.Id, "укажите названия события");
        return;
    }
    async static Task FillingDate(Message message, ITelegramBotClient botClient)
    {
        await botClient.SendTextMessageAsync(message.Chat.Id, "укажите дату и время в формате(день.месяц.год час:минута), пример записи 20.11.2024 17:01");
        return;
    }
    async static Task FillingGuests(Message message, ITelegramBotClient botClient)
    {
        await botClient.SendTextMessageAsync(message.Chat.Id, "укажите приглашённых участников");
        return;
    }
    async static Task FillingDescription(Message message, ITelegramBotClient botClient)
    {
        await botClient.SendTextMessageAsync(message.Chat.Id, "укажите описание события");
        return;
    }
    async static Task PageMid(CallbackQuery callbackQuery, ITelegramBotClient _botClient, int page)
    {
        string nick = callbackQuery.Message.Chat.Username;
        var collection = GetDBTable1<MongoDBStorage>();
        var filter = Builders<MongoDBStorage>.Filter.Eq(x => x.Owner, nick);
        var all_data = await collection.Find(filter).ToListAsync();
        int len = all_data.Count;
        int pageunreal = page + 1;
        InlineKeyboardMarkup keyboard = new(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: $"удалить {pageunreal} страницу ", callbackData: $"delete {page}"),

            },new[]
            {
                InlineKeyboardButton.WithCallbackData(text: $"назад", callbackData: $"page {page} back {len}"),
                InlineKeyboardButton.WithCallbackData(text: $"вперёд", callbackData: $"page {page} forward {len}")
            }
        });

        string texts = ($"страница № {pageunreal}\n Название события - {all_data[page].Name}  \nДата и время мероприятия - {all_data[page].Date} " +
        $"\nПриглашённые участники - {string.Join(", ", all_data[page].Guests)} \nОписание события - {all_data[page].Description}");
        await _botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, $"{texts}", replyMarkup: keyboard);
    }

    async static Task PageTop(CallbackQuery callbackQuery, ITelegramBotClient _botClient, int page)
    {
        string nick = callbackQuery.Message.Chat.Username;
        var collection = GetDBTable1<MongoDBStorage>();
        var filter = Builders<MongoDBStorage>.Filter.Eq(x => x.Owner, nick);
        var all_data = await collection.Find(filter).ToListAsync();
        int len = all_data.Count;
        int pageunreal = page + 1;
        InlineKeyboardMarkup keyboard = new(new[]
    {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: $"удалить {pageunreal} страницу ", callbackData: $"delete {page}"),
            }, new[]
            {
                InlineKeyboardButton.WithCallbackData(text: $"назад", callbackData: $"page {page} back {len}"),
            }
        });

        string texts = ($"страница № {pageunreal}\n Название события - {all_data[page].Name}  \nДата и время мероприятия - {all_data[page].Date} " +
        $"\nПриглашённые участники - {string.Join(", ", all_data[page].Guests)} \nОписание события - {all_data[page].Description}");
        await _botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, $"{texts}", replyMarkup: keyboard);
    }

    async static Task PageBot(CallbackQuery callbackQuery, ITelegramBotClient _botClient, int page)
    {
        string nick = callbackQuery.Message.Chat.Username;
        var collection = GetDBTable1<MongoDBStorage>();
        var filter = Builders<MongoDBStorage>.Filter.Eq(x => x.Owner, nick);
        var all_data = await collection.Find(filter).ToListAsync();
        int len = all_data.Count;
        int pageunreal = page + 1;
        InlineKeyboardMarkup keyboard = new(new[]
    {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: $"удалить {pageunreal} страницу ", callbackData: $"delete {page}"),

            }, new[]
            {
                InlineKeyboardButton.WithCallbackData(text: $"вперёд", callbackData: $"page {page} forward {len}")
            }
        });

        string texts = ($"страница № {pageunreal}\n Название события - {all_data[page].Name}  \nДата и время мероприятия - {all_data[page].Date} " +
        $"\nПриглашённые участники - {string.Join(", ", all_data[page].Guests)} \nОписание события - {all_data[page].Description}");
        await _botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, $"{texts}", replyMarkup: keyboard);
    }

    async static Task DeletePage(CallbackQuery callbackQuery, ITelegramBotClient _botClient, int page)
    {
        string nick = callbackQuery.Message.Chat.Username;
        var collection = GetDBTable1<MongoDBStorage>();
        var filter = Builders<MongoDBStorage>.Filter.Eq(x => x.Owner, nick);

        var all_data = await collection.Find(filter).ToListAsync();
        var bson_number = all_data[page].Id;


        var filter1 = Builders<MongoDBStorage>.Filter.Where(x => x.Owner == nick && x.Id == bson_number);

        await collection.FindOneAndDeleteAsync(filter1);
        await _botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "УДАЛЕНИЕ УСПЕШНО");
        return;
    }


    async static Task Сreating_Representation(Message message, ITelegramBotClient botClient)
    {
        string nick = message.From.Username;
        var collection = GetDBTable1<MongoDBStorage>();

        var filter = Builders<MongoDBStorage>.Filter.Eq(x => x.Owner, nick);

        var all_data = await collection.Find(filter).ToListAsync();
        int len = all_data.Count;

        string alltext = "";
        switch (len)
        {
            case 0:
                await botClient.SendTextMessageAsync(message.Chat.Id, text: "здесь пока ничего нет");
                break;
            case 1:
                InlineKeyboardMarkup keyboard = new(new[]
{
                    new[]
                    {
                    InlineKeyboardButton.WithCallbackData(text: $"удалить 1 страницу ", callbackData: $"delete 0"),
                    }
                    });
                for (int i = 0; i < all_data[0].Guests.Count; i++)
                {
                    string text = $"\n{all_data[0].Guests[i].ForeName}" + " - " + $"{all_data[0].Guests[i].Agreement}";
                    alltext = alltext + text;
                }
                string runtext = $"страница № 1\n Название события - {all_data[0].Name}  \nДата и время мероприятия - {all_data[0].Date} \nПриглашённые участники -  " +
                    $"{alltext}" +
                    $"\nОписание события - {all_data[0].Description}";
                await botClient.SendTextMessageAsync(message.Chat.Id, text: runtext, replyMarkup: keyboard);
                break;
            default:

                InlineKeyboardMarkup keyboard1 = new(new[]
                {
                        new[]
                    {
                        InlineKeyboardButton.WithCallbackData(text: $"удалить 1 страницу ", callbackData: $"delete 0"),

                    }, new[]
                    {
                        InlineKeyboardButton.WithCallbackData(text: "вперёд", callbackData: $"page 0 forward {len}")
                    }
                });
                for (int i = 0; i < all_data[0].Guests.Count; i++)
                {
                    string text = $"\n{all_data[0].Guests[i].ForeName}" + " - " + $"{all_data[0].Guests[i].Agreement}";
                    alltext = alltext + text;
                }
                string runtext1 = $"ст1аница № 1\n Название события - {all_data[0].Name}  \nДата и время мероприятия - {all_data[0].Date} \nПриглашённые участники -  " +
                    $"{alltext}" +
                    $"\nОписание события - {all_data[0].Description}";
                await botClient.SendTextMessageAsync(message.Chat.Id, text: runtext1, replyMarkup: keyboard1);
                break;
        }

        return;
    }

    async static Task AnswerRefuse(CallbackQuery callbackQuery, ITelegramBotClient _botClient, string owner)
    {
        var filter = Builders<MongoDBStorage>.Filter.Eq(x => x.Owner, owner);

        await _botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Вы отклонили запрос");
        await _botClient.SendTextMessageAsync(owner, "гость отклонил запрос");
        /*
        var collection = GetDBTable1<MongoDBStorage>();
        var all_data1 = await collection.Find(filter).ToListAsync();
        string nick = callbackQuery.Message.Chat.Username;
        var find_object = collection.FindOneAndUpdate(Builders<MongoDBStorage>.Filter.Where(x => x.NickName == owner && x.Guests == nick), Builders<MongoDBStorage>.Update.Set(y => y.Description, descriptionbd));
        var filter1 = Builders<MongoDBStorage>.Filter.Where(x => x.NickName == owner && x.Guests.Contains(nick) && x.);

        await collection.FindOneAndDeleteAsync(filter1);

        //var find_object = collection.FindOneAndUpdate(Builders<MongoDBStorage>.Filter.Where(x => x.NickName == owner && x.Guests. ), Builders<MongoDBStorage>.Update.Set(y => y.Guests, userinfo));

        */
    }

    async static Task AnswerAccept(CallbackQuery callbackQuery, ITelegramBotClient _botClient, string owner)
    {
        await _botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Запрос успешно принят");
        //Transfer(callbackQuery, _botClient, owner);
        await _botClient.SendTextMessageAsync(owner, "гость принял запрос");
    }
    /*
    async static Task Transfer(CallbackQuery callbackQuery, ITelegramBotClient _botClient, string owner)
    {
        var collection = GetDBTable1<MongoDBStorage>();
        var filter = Builders<MongoDBStorage>.Filter.Eq(x => x.Guests, callbackQuery.Message.Chat.Username);
        var all_data1 = await collection.Find(filter).ToListAsync();


        string connectionString = "mongodb://localhost:27017";
        string dataBaSeName = "Storage";
        string collectionName = "Da";

        var client = new MongoClient(connectionString);
        var db = client.GetDatabase(dataBaSeName);
        var storage = db.GetCollection<MongoDBStorage>(collectionName);
        var info = new MongoDBStorage { NickName = callbackQuery.Message.Chat.Username, Number = 0, Description = "123"};

        await storage.InsertOneAsync(info);
    }
    */

    private static IMongoCollection<T> GetDBTable<T>()
    {
        string connectionString = "mongodb://localhost:27017";
        string dataBaSeName = "Temp";
        string collectionName = "Data";

        var client = new MongoClient(connectionString);
        var db = client.GetDatabase(dataBaSeName);
        return db.GetCollection<T>(collectionName);
    }

    private static IMongoCollection<T> GetDBTable1<T>()
    {
        string connectionString = "mongodb://localhost:27017";
        string dataBaSeName = "Storage";
        string collectionName = "Da";

        var client = new MongoClient(connectionString);
        var db = client.GetDatabase(dataBaSeName);
        return db.GetCollection<T>(collectionName);
    }



    // Process Inline Keyboard callback data


    #region Inline Mode

    private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results = {
            // displayed result
            new InlineQueryResultArticle(
                id: "1",
                title: "TgBots",
                inputMessageContent: new InputTextMessageContent("hello"))
        };

        await _botClient.AnswerInlineQueryAsync(
            inlineQueryId: inlineQuery.Id,
            results: results,
            cacheTime: 0,
            isPersonal: true,
            cancellationToken: cancellationToken);
    }

    private async Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);

        await _botClient.SendTextMessageAsync(
            chatId: chosenInlineResult.From.Id,
            text: $"You chose result with Id: {chosenInlineResult.ResultId}",
            cancellationToken: cancellationToken);
    }

    #endregion

#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable RCS1163 // Unused parameter.
    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
#pragma warning restore RCS1163 // Unused parameter.
#pragma warning restore IDE0060 // Remove unused parameter
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}