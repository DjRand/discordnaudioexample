using Discord;
using Discord.Audio;
using Discord.Commands;
using NAudio.Wave;
using System.Threading.Tasks;

namespace DiscordAudioExample
{
    class Program
    {
        // This example shows you how to play a file with NAudio.
        // I will do an example for ffmpeg later.
        //               - WARNING: READ THIS -
        // NOTE: You need to include both "libsodium.dll" and "opus.dll" in your solution.
        // Make sure you set it to "Copy always" in your Solution Explorer >
        // If you do not have BOTH of these, your bot will not join or play audio.
        // 
        // You can find out where to get these files from here: http://rtd.discord.foxbot.me/en/legacy/features/voice.html
        // For the lazy people:
        // libsodium.dll 
        // https://github.com/RogueException/Discord.Net/blob/master/src/Discord.Net.Audio/libsodium.dll
        // opus.dll
        // https://github.com/RogueException/Discord.Net/blob/master/src/Discord.Net.Audio/opus.dll

        // This is the "audio client" we'll use it later.
        public static IAudioClient _vClient;

        // Bool, when playing a song, set it to true, so you don't play two songs at the same time >_>
        private static bool playingSong = false;

        // Your bots Token.  Change this in order to connect.
        private static string botToken = "BOT TOKEN HERE.";

        // The discord client.
        public static DiscordClient _client;

        static void Main(string[] args)
        {
            Start();
        }
        public static void Start()
        {
            _client = new DiscordClient();

            // We're using Discord.Commands, got to tell it what you want to use.
            // the '!' in the line below, is the 'command prefix'
            // example: !play, !skip.
            // If you change '!' to '$' you would use $play
            _client.UsingCommands(x =>
            {
                x.PrefixChar = '!';
                x.HelpMode = HelpMode.Public;
            });

            // Discord.Audio stuff.  Got to set the mode to outgoing.
            _client.UsingAudio(x => 
            {
                x.Mode = AudioMode.Outgoing;
            });


            // Variable to make creating commands look a little more clean.
            var cmds = _client.GetService<CommandService>();

            // Play command.
            cmds.CreateCommand("play")
                .Do(async e =>
                {
                    // Check to see if a song is already playing.  If it is, return;
                    if (playingSong == true) return;

                    // Check to see if the person who used the !play command is on a voice channel:
                    Channel voiceChan = e.User.VoiceChannel;
                    if (voiceChan == null)
                    {
                        // If they aren't, call them out on their stupidity.
                        // Note:  Some times discord bugs, and the bot won't see  you in a voice channel.
                        // If this happens, disconnect and reconnect to voice while the bot is online.
                        await e.Channel.SendMessage("You want me to play a song for you, but you're not even connected to voice? Pfftt.");
                        return;
                    }
                    // Okay, they're on a voice channel.

                    await e.Channel.SendMessage("Playing file...");

                    // Set the "PlayingSong" to true.
                    playingSong = true;

                    // You'll need to change the file location, of course.
                    await SendAudio(@"F:\Stuff\Music\somefile.mp3", voiceChan);

                    
                    await e.Channel.SendMessage("Finished playing file..");

                    // Song is finished, set the playingSong to false:
                    playingSong = false;
                });

            // Bonus.  Simple skip command.
            cmds.CreateCommand("skip")
                .Do(async e =>
                {
                    // If there is no song playing, no need to skip.
                    if (playingSong == false) return;

                    // In the SendAudio method, we use "playingSong" in the while loop.
                    // If we set this to false, it'll jump out of the while loop.
                    playingSong = false;
                    await e.Channel.SendMessage("Skipping the song.");
                });


            // "Proper login" xD
            // Note:  All commands should be ABOVE ExecuteAndWait()
            _client.ExecuteAndWait(async () =>
            {
                while (true)
                {
                    try
                    {
                        await _client.Connect(botToken, TokenType.Bot);
                        break;
                    }
                    catch
                    {
                        System.Console.WriteLine("Could not connect.  Are you using a proper bot token? or maybe services are down?");
                        await Task.Delay(3000);
                    }
                }
            });

        }

        public static async Task SendAudio(string filepath, Channel voiceChannel)
        {
            // When we use the !play command, it'll start this method

            // The comment below is how you'd find the first voice channel on the server "Somewhere"
            //var voiceChannel = _client.FindServers("Somewhere").FirstOrDefault().VoiceChannels.FirstOrDefault();
            // Since we already know the voice channel, we don't need that.
            // So... join the voice channel:
            _vClient = await _client.GetService<AudioService>().Join(voiceChannel);

            // Simple try and catch.
            try
            {

                var channelCount = _client.GetService<AudioService>().Config.Channels; // Get the number of AudioChannels our AudioService has been configured to use.
                var OutFormat = new WaveFormat(48000, 16, channelCount); // Create a new Output Format, using the spec that Discord will accept, and with the number of channels that our client supports.

                using (var MP3Reader = new Mp3FileReader(filepath)) // Create a new Disposable MP3FileReader, to read audio from the filePath parameter
                using (var resampler = new MediaFoundationResampler(MP3Reader, OutFormat)) // Create a Disposable Resampler, which will convert the read MP3 data to PCM, using our Output Format
                {
                    resampler.ResamplerQuality = 60; // Set the quality of the resampler to 60, the highest quality
                    int blockSize = OutFormat.AverageBytesPerSecond / 50; // Establish the size of our AudioBuffer
                    byte[] buffer = new byte[blockSize];
                    int byteCount;
                    // Add in the "&& playingSong" so that it only plays while true. For our cheesy skip command.
                    // AGAIN
                    // WARNING
                    // YOU NEED
                    // vvvvvvvvvvvvvvv
                    // opus.dll
                    // libsodium.dll
                    // ^^^^^^^^^^^^^^^
                    // If you do not have these, this will not work.
                    while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0 && playingSong) // Read audio into our buffer, and keep a loop open while data is present
                    {
                        if (byteCount < blockSize)
                        {
                            // Incomplete Frame
                            for (int i = byteCount; i < blockSize; i++)
                                buffer[i] = 0;
                        }

                        _vClient.Send(buffer, 0, blockSize); // Send the buffer to Discord
                    }
                    await _vClient.Disconnect();
                }
            }
            catch
            {
                System.Console.WriteLine("Something went wrong. :(");
            }
            await _vClient.Disconnect();
        }
    }
}