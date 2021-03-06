﻿using CriminalDanceBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace CriminalDanceBot
{
    public class Bot
    {
        public static ITelegramBotClient Api;
        public static User Me;
        public static GameManager Gm;

        internal static HashSet<Models.Command> Commands = new HashSet<Models.Command>();
        public delegate void CommandMethod(Message msg, string[] args);

        
        internal static Message Send(long chatId, string text, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            return BotMethods.Send(chatId, text, replyMarkup, parseMode, disableWebPagePreview, disableNotification);
        }

        internal static Message Edit(long chatId, int oldMessageId, string text, InlineKeyboardMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            try
            {
                return BotMethods.Edit(chatId, oldMessageId, text, replyMarkup, parseMode, disableWebPagePreview, disableNotification);
            }
            catch (Exception ex)
            {
                ex.LogError();
                return null;
            }
        }

        internal static List<GroupAdmin> GetChatAdmins(long chatid, bool forceCacheUpdate = false)
        {
            try
            {   // Admins are cached for 1 hour
                string itemIndex = $"{chatid}";
                List<GroupAdmin> admins = Program.AdminCache[itemIndex] as List<GroupAdmin>; // Read admin list from cache
                if (admins == null || forceCacheUpdate)
                {
                    admins = Api.GetChatAdministratorsAsync(chatid).Result.Select(x =>
                        new GroupAdmin(x.User.Id, chatid, x.User.FirstName)).ToList();

                    CacheItemPolicy policy = new CacheItemPolicy() { AbsoluteExpiration = DateTime.Now.AddHours(1) };
                    Program.AdminCache.Set(itemIndex, admins, policy); // Write admin list into cache
                }

                return admins;
            }
            catch (Exception e)
            {
                e.LogError();
                return null;
            }
        }
    }

    public static class BotMethods
    { 
        #region Messages
        public static Message Send(long chatId, string text, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            try
            {
                return Bot.Api.SendTextMessageAsync(chatId, text, parseMode, disableWebPagePreview, disableNotification, 0, replyMarkup).Result;
            }
            catch (Exception e)
            {
                //e.LogError();
                return null;
            }
        }

        public static Message Send(this Chat chat, string text, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            try
            {
                return Bot.Api.SendTextMessageAsync(chat.Id, text, parseMode, disableWebPagePreview, disableNotification, 0, replyMarkup).Result;
            }
            catch (Exception e)
            {
                e.LogError();
                return null;
            }
        }

        public static Message Reply(this Message m, string text, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            try
            {
                return Bot.Api.SendTextMessageAsync(m.Chat.Id, text, parseMode, disableWebPagePreview, disableNotification, m.MessageId, replyMarkup).Result;
            }
            catch (Exception e)
            {
                e.LogError();
                return null;
            }
        }

        public static Message Reply(long chatId, int oldMessageId, string text, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            try
            {
                return Bot.Api.SendTextMessageAsync(chatId, text, parseMode, disableWebPagePreview, disableNotification, oldMessageId, replyMarkup).Result;
            }
            catch (Exception e)
            {
                e.LogError();
                return null;
            }
        }

        public static Message ReplyNoQuote(this Message m, string text, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            try
            {
                return Bot.Api.SendTextMessageAsync(m.Chat.Id, text, parseMode, disableWebPagePreview, disableNotification, 0, replyMarkup).Result;
            }
            catch (Exception e)
            {
                e.LogError();
                return null;
            }
        }

        public static Message ReplyPM(this Message m, string text, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            try
            {
                var r = Bot.Api.SendTextMessageAsync(m.From.Id, text, parseMode, disableWebPagePreview, disableNotification, 0, replyMarkup).Result;
                if (r == null)
                {
                    return m.Reply(Commands.GetTranslation("NotStartedBot", Commands.GetLanguage(m.From.Id)), new InlineKeyboardMarkup(new InlineKeyboardButton[] {
                        InlineKeyboardButton.WithUrl("Start me!", $"https://t.me/{Bot.Me.Username}") }));
                }
                if (m.Chat.Type != ChatType.Private)
                    m.Reply(Commands.GetTranslation("SentPM", Commands.GetLanguage(m.From.Id)));
                return r;
            }
            catch (Exception e)
            {
                e.LogError();
                return null;
            }
        }

        public static void ReplyPM(this Message m, string[] texts, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            foreach (var text in texts)
            {
                try
                {
                    var r = Bot.Api.SendTextMessageAsync(m.From.Id, text, parseMode, disableWebPagePreview, disableNotification, 0, replyMarkup).Result;
                    if (r == null)
                    {
                        m.Reply(Commands.GetTranslation("NotStartedBot", Commands.GetLanguage(m.From.Id)), new InlineKeyboardMarkup(new InlineKeyboardButton[] {
                            InlineKeyboardButton.WithUrl("Start me!", $"https://t.me/{Bot.Me.Username}") }));
                        return;
                    }
                }
                catch (Exception e)
                {
                    e.LogError();
                }
            }
            if (m.Chat.Type != ChatType.Private)
                m.Reply(Commands.GetTranslation("SentPM", Commands.GetLanguage(m.From.Id)));
        }

        public static Message Edit(long chatId, int oldMessageId, string text, InlineKeyboardMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            try
            {
                var t = Bot.Api.EditMessageTextAsync(chatId, oldMessageId, text, parseMode, disableWebPagePreview, replyMarkup);
                t.Wait();
                return t.Result;
            }
            catch (Exception e)
            {
                if (e is AggregateException Agg && Agg.InnerExceptions.Any(x => x.Message.ToLower().Contains("message is not modified")))
                {
                    /*
                    var m = "Messae not modified." + Environment.NewLine;
                    m += $"Chat: {chatId}" + Environment.NewLine;
                    m += $"Text: {text}" + Environment.NewLine;
                    m += $"Time: {DateTime.UtcNow.ToLongTimeString()} UTC";
                    Send(Constants.LogGroupId, m);
                    */
                    return null;
                }
                e.LogError();
                return null;
            }
        }

        public static Message SendDocument(long chatId, InputOnlineFile fileToSend, string caption = null, IReplyMarkup replyMarkup = null, bool disableNotification = false, ParseMode parseMode = ParseMode.Html)
        {
            try
            {
                return Bot.Api.SendDocumentAsync(chatId, fileToSend, caption, parseMode, disableNotification, 0, replyMarkup).Result;
            }
            catch (Exception e)
            {
                e.LogError();
                return null;
            }
        }
        #endregion

        #region Callbacks
        public static bool AnswerCallback(CallbackQuery query, string text = null, bool popup = false)
        {
            try
            {
                var t = Bot.Api.AnswerCallbackQueryAsync(query.Id, text, popup); 
                t.Wait();
                return true;            // Await this call in order to be sure it is sent in time
            }
            catch (Exception e)
            {
                e.LogError();
                return false;
            }
        }
        #endregion

    }
}
