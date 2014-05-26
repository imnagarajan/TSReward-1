using System;
using Terraria;
using TShockAPI;
using TerrariaApi.Server;
using System.Reflection;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Wolfje.Plugins.SEconomy;
using Wolfje.Plugins.SEconomy.Journal;
using Wolfje.Plugins.SEconomy.Economy;
using System.Threading;

namespace TSREWARD
{
    [ApiVersion(1, 16)]
    public class TSReward : TerrariaPlugin
    {
        public static Config config;
        WebClient wc = new WebClient { Proxy = null };
        public System.Timers.Timer Timer = new System.Timers.Timer();
        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }
        public override string Author
        {
            get { return "Ancientgods"; }
        }
        public override string Name
        {
            get { return "TSReward"; }
        }

        public override string Description
        {
            get { return "Lets you claim ingame rewards for voting on Terraria-Servers.com"; }
        }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command(Reward, "reward"));
            Commands.ChatCommands.Add(new Command("tsreward.reload", Reload_Config, "tsreload"));
            ReadConfig();
            Timer.Interval = 1000 * config.IntervalInSeconds;
            Timer.Enabled = config.ShowIntervalMessage;
            Timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed);
        }

        void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            for (int i = 0; i < config.IntervalMessage.Text.Length; i++)
                TSPlayer.All.SendMessage(config.IntervalMessage.Text[i], config.IntervalMessage.GetColor());
        }

        public TSReward(Main game)
            : base(game)
        {
            Order = -1;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }

        private void Reward(CommandArgs args)
        {
            if (!args.Player.IsLoggedIn)
            {
                args.Player.SendErrorMessage("You need to be logged in to use this command!");
                return;
            }
            Thread t = new Thread(() =>
                {
                    switch (CheckVote(args.Player.Name))
                    {
                        case Response.Error:
                            args.Player.SendErrorMessage("There was an issue reading your vote, this is either because");
                            args.Player.SendErrorMessage("the server key is incorrect or terraria-servers.com is offline!");
                            return;
                        case Response.NotFound:
                            for (int i = 0; i < config.VoteNotFoundMessage.Text.Length; i++)
                                args.Player.SendMessage(config.VoteNotFoundMessage.Text[i], config.VoteNotFoundMessage.GetColor());
                            return;
                        case Response.VotedAndClaimed:
                            args.Player.SendErrorMessage("You have already claimed your reward today!");
                            return;
                        case Response.VotedNotClaimed:
                            for (int i = 0; i < config.OnRewardClaimMessage.Text.Length; i++)
                                args.Player.SendMessage(config.OnRewardClaimMessage.Text[i], config.OnRewardClaimMessage.GetColor());

                            //if (config.UsingSeconomy)
                            {
                                EconomyPlayer Server = SEconomyPlugin.GetEconomyPlayerSafe(TSServerPlayer.Server.UserID);
                                Server.BankAccount.TransferToAsync(args.Player.Index, config.SEconomyReward, config.AnnounceOnReceive ? BankAccountTransferOptions.AnnounceToReceiver : BankAccountTransferOptions.SuppressDefaultAnnounceMessages, "voting on terraria-servers.com", "Voted on terraria-servers.com");
                                SetAsClaimed(args.Player.Name);
                            }
                            return;
                    }
                });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            
        }

        public Response CheckVote(string Username)
        {
            try
            {
                int I = int.Parse(wc.DownloadString(string.Format("http://terraria-servers.com/api/?object=votes&element=claim&key={0}&username={1}", config.ServerKey, Username)));
                return (Response)I;
            }
            catch { return Response.Error; }
        }

        public void SetAsClaimed(string Username)
        {
            wc.DownloadString(string.Format("http://terraria-servers.com/api/?action=post&object=votes&element=claim&key={0}&username={1}", config.ServerKey, Username));
        }

        public enum Response
        {
            NotFound = 0,
            VotedNotClaimed = 1,
            VotedAndClaimed = 2,
            Error=3
        }

        public class RewardItem
        {
            public int Id;
            public int Amount;
            public RewardItem(int id, int amount)
            {
                Id = id;
                Amount = amount;
            }
        }

        public class Clr
        {
            public int R;
            public int G;
            public int B;
            public Clr(int r, int g, int b)
            {
                R = r;
                G = g;
                B = b;
            }
        }

        public class Message
        {
            public Clr Color;
            public string[] Text;
            public Message(string[] text)
            {
                Text = text;
                Color = new Clr(40,160,240);
            }
            public Color GetColor()
            {
                return new Color(Color.R, Color.G, Color.B);
            }
        }

        public class Config
        {
            public string ServerKey = "ServerKeyGoesHere";
            //public bool UsingSeconomy = false;
            public int SEconomyReward = 1000;
            public bool AnnounceOnReceive = true;
            /*public bool GiveRewardItems = false;
            public RewardItem[] RewardItems = new RewardItem[] { new RewardItem(5, 1), new RewardItem(6, 1) };*/
            public Message VoteNotFoundMessage = new Message(new string[]{
                "Vote not found!",
                "If you haven't voted yet, please go to terraria-servers.com and",
                "vote for the server to receive ingame rewards!"
            });
            public Message OnRewardClaimMessage = new Message(new string[] {
                "Thank you for voting on terraria-servers.com",
                "We really appreciate it!"          
            });
            public bool ShowIntervalMessage = true;
            public int IntervalInSeconds = 300;
            public Message IntervalMessage = new Message(new string[]{
                "Vote on terraria-servers.com and receive 1000 coins!",
                "After voting you can use the command /reward!"
            });
        }

        private static void CreateConfig()
        {
            string filepath = Path.Combine(TShock.SavePath, "TSReward.json");
            try
            {
                using (var stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    using (var sr = new StreamWriter(stream))
                    {
                        config = new Config();
                        var configString = JsonConvert.SerializeObject(config, Formatting.Indented);
                        sr.Write(configString);
                    }
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError(ex.Message);
                config = new Config();
            }
        }

        private static bool ReadConfig()
        {
            string filepath = Path.Combine(TShock.SavePath, "TSReward.json");
            try
            {
                if (File.Exists(filepath))
                {
                    using (var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            var configString = sr.ReadToEnd();
                            config = JsonConvert.DeserializeObject<Config>(configString);
                        }
                        stream.Close();
                    }
                    return true;
                }
                else
                {
                    Log.ConsoleError("TSReward config not found. Creating new one...");
                    CreateConfig();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError(ex.Message);
            }
            return false;
        }

        private void Reload_Config(CommandArgs args)
        {
            if (ReadConfig())
            {
                Timer.Interval = 1000 * config.IntervalInSeconds;
                Timer.Enabled = config.ShowIntervalMessage;
                args.Player.SendMessage("TSReward config reloaded sucessfully.", Color.Green);
            }
        }
    }
}
