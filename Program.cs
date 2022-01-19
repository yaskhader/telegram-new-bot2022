using System;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.InputFiles;
using System.IO;

namespace Text_To_Speech_Bot
{
    class Program
    {
        private static TelegramBotClient? Bot;


        public static async Task Main()
        {
            Bot = new TelegramBotClient("5008816643:AAGISJ10I7w0TWqvZuSgl1tjfxUthTaHlZQ");

            User me = await Bot.GetMeAsync();
            Console.Title = me.Username ?? "Text_To_Speech_Bot";
            using var cts = new CancellationTokenSource();

            ReceiverOptions receiverOptions = new() { AllowedUpdates = { } };
            Bot.StartReceiving(HandleUpdateAsync,
                               HandleErrorAsync,
                               receiverOptions,
                               cancellationToken: cts.Token);

            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();

            cts.Cancel();
        }

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                UpdateType.Message => BotOnMessageReceived(botClient, update.Message!),
                UpdateType.EditedMessage => BotOnMessageReceived(botClient, update.EditedMessage!),
                _ => UnknownUpdateHandlerAsync(botClient, update)
            };

            try
            {
                await handler;
            }
            catch (Exception exception)
            {
                await HandleErrorAsync(botClient, exception, cancellationToken);
            }
        }

        private static async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
        {
            Console.WriteLine($"Receive message type: {message.Type}");
            if (message.Type != MessageType.Text)
                return;


            var action = message.Text switch
            {
                "/help" or "/start" => help(botClient, message),
                _ => textToSpeech(botClient, message)
            };
            Message sentMessage = await action;
            Console.WriteLine($"The message was sent with id: {sentMessage.MessageId}");


            static async Task<Message> help(ITelegramBotClient botClient, Message message)
            {


                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                            text:
                                                                  "/help - Get help\n" +
                                                                  "Please send the text you want to convert into speech.\n",
                                                            replyMarkup: new ReplyKeyboardRemove());
            }


        }



        private static Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
        {
            Console.WriteLine($"Unknown update type: {update.Type}");
            return Task.CompletedTask;
        }



        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }


        static async Task<Message> textToSpeech(ITelegramBotClient botClient, Message message)
        {

            var synthesizer = new SpeechSynthesizer();
            synthesizer.SetOutputToDefaultAudioDevice();

            // Configure the audio output.   
            synthesizer.SetOutputToWaveFile("D:\\output.wav",
              new SpeechAudioFormatInfo(32000, AudioBitsPerSample.Sixteen, AudioChannel.Mono));

            PromptBuilder builder = new PromptBuilder();
            builder.AppendText(message.Text);

            // Speak the prompt and play the output file.
            synthesizer.Speak(builder);
            //synthesizer.Pause();
            synthesizer.SetOutputToNull();

            using (FileStream stream = System.IO.File.OpenRead("D:\\output.wav"))
            {
                InputOnlineFile inputOnlineFile = new InputOnlineFile(stream, "D:\\output.wav");


                return await botClient.SendAudioAsync(
                                    chatId: message.Chat.Id,
                                    replyToMessageId: message.MessageId,
                                    audio: inputOnlineFile
                                        );
            }
        }



    }
}


