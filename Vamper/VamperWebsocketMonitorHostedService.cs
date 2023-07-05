using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using Serilog.Formatting.Compact;
using System.Globalization;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.EventSub;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Stream;

namespace Vamper
{
    public struct VamperSettings
    {
        public string ChannelId { get; set; }
        public string TwitchAccessToken { get; set; }
        public string TwitchClientId { get; set; }
        public bool DoBackups { get; set; }
    }

    internal class VamperWebsocketMonitorHostedService : IHostedService
    {
        public const string FeelsOkayMan = "@@@@@@@@@@@@@@@@@@@@@@@@@@@&%%%%%&@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@\r\n@@@@@@@@@@@@@@@@@@@@@&/,*/*********/***(&@@@@@@@@@%/**********#@@@@@@@@@@@@@@@@@\r\n@@@@@@@@@@@@@@@@@@#,********************/*,(%(**/**************/*(@@@@@@@@@@@@@@\r\n@@@@@@@@@@@@@@@@*****************************.*********************%@@@@@@@@@@@@\r\n@@@@@@@@@@@@@@/**********/*,,,*******,,,,*/**/,*********************#@@@@@@@@@@@\r\n@@@@@@@@@@@@#,/*******,,*/******************,,*,,,,,,,,*******,,,,,*,%@@@@@@@@@@\r\n@@@@@@@@@@@(**************************/////////*.*************************(@@@@@\r\n@@@@@@@@@@***********************,,,**/////////***,.******,,,,,,*****,,,,,,,,&@@\r\n@@@@@@@%(,***************//*,,*/*,,,,*/(((/***,***,,,.***,,,,**********,,**,,,**\r\n@@@@/*/**,/************,*//*,,,,/%&, %*    *@@@@@@@#/.*/**,*#@% ,&,    ,@@@&*.,*\r\n@@%,/***,***********.****//*/&@@@&      %&.  @@@@@@@@@&.%@@@@#   , .%#. .&@@@@@@\r\n@/***********************,*&@@@@@%           &@@@@@@@@(@@@@@@(          .&@@@@@@\r\n./*************************/*,*%@@#        /@@@@@@&/,,,#@@@@@@%       .&@@&(/,,,\r\n/*************************/*,,,*/*,,*****,,,,,,*//**,,/**///**,,****/**//**/*%@@\r\n**************************************,,,,*,,,**************************/**%@@@@\r\n********************************************.,*/********,,*////////**,,#@@@@@@@@\r\n*************************************,,,**/****************//,*////*****/&@@@@@@\r\n*************************************************************************/*@@@@@\r\n*********************.*****.,/**********************************************&@@@\r\n*******************,*//*/////*,,*/******************************************/@@@\r\n********************,////,,//**///*,,,**////***************************//*,,,*%@\r\n*********************.///*//*,,*/////**/////**,,,,,,,,**********,,,,,,*////*/*(@\r\n**********************/,.*///**///**,,,,,****//////////////////////////****/&@@@\r\n**************************/*,,,,**////*/*/*///////*****************////////%@@@@\r\n*********************************///**,,,,,,***//****///////////////////**#@@@@@\r\n************************************************//****************,(&@@@@@@@@@@@\r\n.**,,**/*****************************************************//*#@@@@@@@@@@@@@@@\r\n///*,,*/*,,,,*********************************************/(&@@@@@@@@@@@@@@@@@@@\r\n////////**,,**///******,,,,,,,,,,,,,,,******,,,,,,,,..#@@@@@@@@@@@@@@@@@@@@@@@@@\r\n////////////////***,,,,,,,,,,*********///*****,,,,,////**#@@@@@@@@@@@@@@@@@@@@@@\r\n///////////////////////////////////////////////////////////*/&@@@@@@@@@@@@@@@@@@\r\n//////////////////////////////////////////////////////////////,&@@@@@@@@@@@@@@@@";

        public const string Banner =
            "\n\n ██▒   █▓ ▄▄▄       ███▄ ▄███▓ ██▓███  ▓█████  ██▀███  \r\n▓██░   █▒▒████▄    ▓██▒▀█▀ ██▒▓██░  ██▒▓█   ▀ ▓██ ▒ ██▒\r\n ▓██  █▒░▒██  ▀█▄  ▓██    ▓██░▓██░ ██▓▒▒███   ▓██ ░▄█ ▒\r\n  ▒██ █░░░██▄▄▄▄██ ▒██    ▒██ ▒██▄█▓▒ ▒▒▓█  ▄ ▒██▀▀█▄  \r\n   ▒▀█░   ▓█   ▓██▒▒██▒   ░██▒▒██▒ ░  ░░▒████▒░██▓ ▒██▒\r\n   ░ ▐░   ▒▒   ▓▒█░░ ▒░   ░  ░▒▓▒░ ░  ░░░ ▒░ ░░ ▒▓ ░▒▓░\r\n   ░ ░░    ▒   ▒▒ ░░  ░      ░░▒ ░      ░ ░  ░  ░▒ ░ ▒░\r\n     ░░    ░   ▒   ░      ░   ░░          ░     ░░   ░ \r\n      ░        ░  ░       ░               ░  ░   ░     \r\n     ░    \n\n";

        public const string TparT = "  _____            _ __                           _____  \r\n |_   _|   ___    | '_ \\  __ _      _ _    ___   |_   _| \r\n   | |    |___|   | .__/ / _` |    | '_|  |___|    | |   \r\n  _|_|_   _____   |_|__  \\__,_|   _|_|_   _____   _|_|_  \r\n_|\"\"\"\"\"|_|     |_|\"\"\"\"\"|_|\"\"\"\"\"|_|\"\"\"\"\"|_|     |_|\"\"\"\"\"| \r\n\"`-0-0-'\"`-0-0-'\"`-0-0-'\"`-0-0-'\"`-0-0-'\"`-0-0-'\"`-0-0-' \n\n";

        private readonly string WorkPath;
        private readonly string VamperAppDataPath;

        private readonly CsvConfiguration configuration = new(CultureInfo.InvariantCulture)
        {
            IncludePrivateMembers = true,
        };

        private VamperSettings _settings { get; set; }

        private readonly EventSubWebsocketClient _client;
        private readonly TwitchAPI _api;

        private DateTime StreamStartTime;
        private string CurrentTitle { get; set; } = "N/A";
        private string CurrentCatagoryName { get; set; } = "N/A";
        private string CurrentCatagoryId { get; set; } = "N/A";
        private string CurrentStreamId { get; set; } = "N/A";

        public VamperWebsocketMonitorHostedService(ILogger<VamperWebsocketMonitorHostedService> logger, EventSubWebsocketClient eventSubWebsocketClient)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(FeelsOkayMan);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(Banner);

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("\t\t\tMADE BY:\n");
            Console.WriteLine(TparT);

            WorkPath = Path.GetDirectoryName(Directory.GetCurrentDirectory())!;
            VamperAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vamper");

            Console.Title = "Vamper - Made by T-par-T | Your free trial will end in 30 days :D";

            Log.Logger = new LoggerConfiguration()
                            .WriteTo.Async(wt => wt.Console())
                            .WriteTo.Async(wt => wt.File($"{WorkPath}/logs/text/log.txt", rollingInterval: RollingInterval.Day, shared: true))
                            .WriteTo.Async(wt => wt.File(new CompactJsonFormatter(), $"{WorkPath}/logs/json/log.json", rollingInterval: RollingInterval.Day, shared: true))
                            .CreateLogger();

            string configJson = File.ReadAllText($"{WorkPath}/config.json");
            _settings = JsonConvert.DeserializeObject<VamperSettings>(configJson);

            Log.Information("Excel backups are set to: {value} !", _settings.DoBackups);
            if (_settings.DoBackups)
            {
                Log.Warning("FOR EMERGENCY CASES ONLY!!! DO NOT OPEN WHILE VAMPER IS RUNNING:");
                Log.Information("Backups are saved at \"{path}\"\n", $"{VamperAppDataPath}\\Backups");
            }
            else
            {
                Log.Error("THIS IS YOUR LAST BIG WARNING!!! You should turn on backups in the config.json file!", false);
                Log.Error("A little bit of safety never hurt anyone :D better be safe than sorry YesYes\n");
            }

            _api = new TwitchAPI();
            _api.Settings.AccessToken = _settings.TwitchAccessToken;
            _api.Settings.ClientId = _settings.TwitchClientId;

            _client = eventSubWebsocketClient ?? throw new ArgumentNullException(nameof(eventSubWebsocketClient));
            _client.WebsocketConnected += OnWebsocketConnected;
            _client.WebsocketDisconnected += OnWebsocketDisconnected;
            _client.WebsocketReconnected += OnWebsocketReconnected;
            _client.ErrorOccurred += OnErrorOccurred;

            _client.ChannelUpdate += ChannelUpdate;
            _client.StreamOnline += StreamOnline;
            _client.StreamOffline += StreamOffline;

            Task.Run(async () => await GetAndSetLatestStreamDataAsync(_settings.ChannelId));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
            => await _client.ConnectAsync();

        public async Task StopAsync(CancellationToken cancellationToken)
            => await _client.DisconnectAsync();

        private async Task OnWebsocketConnected(object sender, WebsocketConnectedArgs e)
        {
            Log.Information("Websocket {SessionId} connected!\n", _client.SessionId);

            await Task.Run(async () =>
            {
                if (!e.IsRequestedReconnect)
                {
                    await Task.Delay(500);
                    await Task.WhenAll(
                        SubscribeAsync("channel.update"),
                        SubscribeAsync("stream.offline"),
                        SubscribeAsync("stream.online")
                    );
                }
            });

            Console.WriteLine();
            Log.Information("{line}", "---------------------------------------------------\n");
        }

        private async Task OnWebsocketDisconnected(object sender, EventArgs e)
        {
            Log.Error("Websocket {SessionId} disconnected!\n", _client.SessionId);

            // Don't do this in production. You should implement a better reconnect strategy
            while (!await _client.ReconnectAsync())
            {
                Log.Error("Websocket reconnect failed!\n");
                await Task.Delay(1000);
            }
        }

        private async Task OnWebsocketReconnected(object sender, EventArgs e)
            => Log.Warning("Websocket {SessionId} reconnected!", _client.SessionId);

        private async Task OnErrorOccurred(object sender, ErrorOccuredArgs e)
            => Log.Error("Websocket {SessionId} - Error occurred!\n", _client.SessionId);

        private async Task<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream> GetStreamAsync(string id)
            => (await _api.Helix.Streams.GetStreamsAsync(userIds: new() { id }).ConfigureAwait(false)).Streams[0];

        private async Task GetAndSetLatestStreamDataAsync(string channelId)
        {
            TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream stream = await GetStreamAsync(channelId);

            StreamStartTime = stream.StartedAt;
            CurrentTitle = stream.Title;
            CurrentCatagoryName = stream.GameName;
            CurrentCatagoryId = stream.GameId;
            CurrentStreamId = stream.Id;
        }

        private async Task ChannelUpdate(object sender, ChannelUpdateArgs e)
        {
            TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream stream = await GetStreamAsync(e.Notification.Payload.Event.BroadcasterUserId);

            StreamStartTime = stream.StartedAt;
            CurrentStreamId = stream.Id;

            ChannelUpdate update = e.Notification.Payload.Event;
            TimeSpan ets = e.Notification.Metadata.MessageTimestamp - StreamStartTime;

            Log.Information("{BroadcasterUserName}'s stream details just got updated!\n", update.BroadcasterUserName);

            EventType eventType = EventType.None;

            bool title = CurrentTitle != update.Title;
            bool catagory = CurrentCatagoryId != update.CategoryId;

            if (title)
            {
                Log.Information("{BroadcasterUserName} just changed their stream title:", update.BroadcasterUserName);
                Log.Information("From: \"{from}\"", CurrentTitle);
                Log.Information("To: \"{to}\"", update.Title);
                Log.Information("Estimated timestamp: {ets}\n", ets);

                CurrentTitle = update.Title;

                eventType = EventType.Title_Changed;
            }

            if (catagory)
            {
                Log.Information("{BroadcasterUserName} just changed their stream catagory:", update.BroadcasterUserName);
                Log.Information("From: \"{from}\"", CurrentCatagoryName);
                Log.Information("To: \"{to}\"", update.CategoryName);
                Log.Information("Estimated timestamp: {ets}\n", ets);

                CurrentCatagoryName = update.CategoryName;
                CurrentCatagoryId = update.CategoryId;

                eventType = EventType.Catagory_Changed;
            }

            if (title && catagory)
                eventType = EventType.Title_And_Catagory_Changed;

            await LogEventCsvAsync(update.BroadcasterUserName, eventType, ets);
        }

        private async Task StreamOnline(object sender, StreamOnlineArgs e)
        {
            TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream stream = await GetStreamAsync(e.Notification.Payload.Event.BroadcasterUserId);

            StreamStartTime = stream.StartedAt;
            CurrentTitle = stream.Title;
            CurrentCatagoryName = stream.GameName;
            CurrentCatagoryId = stream.GameId;
            CurrentStreamId = stream.Id;

            await LogEventCsvAsync(e.Notification.Payload.Event.BroadcasterUserName, EventType.Stream_Online, TimeSpan.Zero);

            Log.Information("{BroadcasterUserName} just went {online}!", e.Notification.Payload.Event.BroadcasterUserName, "online");
            Log.Information("Catagory: {CurrentCatagoryName}", CurrentCatagoryName);
            Log.Information("Title: \"{CurrentTitle}\"\n", CurrentTitle);
        }

        private async Task StreamOffline(object sender, StreamOfflineArgs e)
        {
            await LogEventCsvAsync(e.Notification.Payload.Event.BroadcasterUserName, EventType.Stream_Offline, TimeSpan.Zero);

            Log.Information("{BroadcasterUserName} just went {offine}!\n", e.Notification.Payload.Event.BroadcasterUserName, "offline");
        }

        private async Task<CreateEventSubSubscriptionResponse> SubscribeAsync(string subscriptionType)
        {
            Log.Information("Subscribing to {subscriptionType} on channel Id {ChannelId}", subscriptionType, _settings.ChannelId);

            return await _api.Helix.EventSub.CreateEventSubSubscriptionAsync(
                subscriptionType,
                "1",
                new Dictionary<string, string> { { "broadcaster_user_Id", _settings.ChannelId! } },
                EventSubTransportMethod.Websocket,
                _client.SessionId,
                null,
                null,
                _api.Settings.ClientId,
                _api.Settings.AccessToken);
        }

        private async Task LogEventCsvAsync(string streamerName, EventType eventType, TimeSpan ets)
        {
            string ts = $"{ets:h'h'm'm's's'}";

            VamperData vamperData = new VamperData
            {
                TimeUTC = DateTime.UtcNow,
                StreamerName = streamerName,
                EventType = eventType,
                StreamCatagoryName = CurrentCatagoryName,
                StreamCatagoryId = CurrentCatagoryId,
                StreamTitle = CurrentTitle,
                StreamTimeStamp = ts,
                VodTimestampedLink = new Uri($"https://www.twitch.tv/vIdeos/{CurrentStreamId}?t={ts}"),
                StreamId = CurrentStreamId
            };

            if (_settings.DoBackups)
            {
                string backupsFolder = Directory.CreateDirectory($"{VamperAppDataPath}/Backups").FullName;
                string backupPath = $"{backupsFolder}/{streamerName}.csv.bkp";
                WriteCSV(vamperData, backupPath);
            }

            string path = $"{WorkPath}\\RecordedData\\{streamerName}.csv";

            Log.Warning("Trying to save new data to \"{path}\" ...\n", path);

            while (WriteCSV(vamperData, path) is not true)
            {
                Log.Error("Could not save to \"{path}\"", path);
                Log.Error("Because its propbably used by another program (Most likely to be Excel).");
                Log.Error("Make sure to close all programs that have the file opened!");
                Log.Error("Vamper will *automatically* try to save again in {seconds} seconds.", 20);
                Log.Error("You have {seconds} seconds to close all programs that use the file before Vamper will do another retry!\n", 20);

                Log.Fatal("****** DO NOT CLOSE VAMPER OTHERWISE THE CURRENT CAPTURED DATA WILL BE LOST! ******\n");

                await Task.Delay(20 * 1000);
            }

            Log.Information("Done saving new data to \"{path}\"!\n", path);
            Log.Information("{line}", "---------------------------------------------------\n");
        }

        private bool WriteCSV(VamperData data, string path)
        {
            bool successfulWrite = false;
            bool fileExists = File.Exists(path);
            try
            {
                using (StreamWriter writer = new StreamWriter(path, true))
                using (CsvWriter csv = new CsvWriter(writer, configuration))
                {
                    csv.Context.RegisterClassMap<VamperDataMap>();

                    if (!fileExists)
                    {
                        csv.WriteHeader<VamperData>();
                        csv.NextRecord();
                    }

                    csv.WriteRecord(data);
                    csv.NextRecord();

                    successfulWrite = true;
                }

                return successfulWrite;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }

    public enum EventType
    {
        None,
        Stream_Online,
        Stream_Offline,
        Title_Changed,
        Catagory_Changed,
        Title_And_Catagory_Changed
    }

    public class VamperData
    {
        public DateTime TimeUTC { get; set; }
        public string StreamerName { get; set; }
        public EventType EventType { get; set; }
        public string StreamCatagoryName { get; set; }
        public string StreamCatagoryId { get; set; }
        public string StreamTitle { get; set; }
        public string StreamTimeStamp { get; set; }
        public Uri VodTimestampedLink { get; set; }
        public string StreamId { get; set; }

        public override string ToString()
            => $"Time = {TimeUTC:G}\nStreamerName = {StreamerName}\nEventType = {EventType}\nStreamCatagoryName = {StreamCatagoryName}\nStreamCatagoryId = {StreamCatagoryId}\nStreamTitle = {StreamTitle}\nStreamTimeStamp = {StreamTimeStamp}\nVodTimestampedLink = {VodTimestampedLink}\nStreamId = {StreamId}";
    }

    public class VamperDataMap : ClassMap<VamperData>
    {
        public VamperDataMap()
        {
            Map(m => m.TimeUTC).Index(0).Name("Time (UTC)");
            Map(m => m.StreamerName).Index(1).Name("Streamer Name");
            Map(m => m.EventType).Index(2).Name("Event Type");
            Map(m => m.StreamCatagoryName).Index(3).Name("Stream Catagory Name");
            Map(m => m.StreamCatagoryId).Index(4).Name("Stream Catagory Id");
            Map(m => m.StreamTitle).Index(5).Name("Stream Title");
            Map(m => m.StreamTimeStamp).Index(6).Name("Stream TimeStamp");
            Map(m => m.VodTimestampedLink).Index(7).Name("VOD Link");
            Map(m => m.StreamId).Index(8).Name("Stream Id");
        }
    }
}
