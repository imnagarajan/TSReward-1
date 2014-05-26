using System;
using Terraria;
using TShockAPI;
using TerrariaApi.Server;
using System.Reflection;
using System.Net;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Wolfje.Plugins.SEconomy;

namespace TSREWARD
{
    [ApiVersion(1, 16)]
    public class TSReward : TerrariaPlugin
    {
        public List<Message> VoteNotFoundMessages = new List<Message>();
        public List<Message> OnRewardClaimMessages = new List<Message>();
        public static Config config;
        WebClient wc = new WebClient { Proxy = null };
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
            switch (CheckVote(args.Player.Name))
            {
                case Response.Error:
                    args.Player.SendErrorMessage("There was an issue reading your vote, this is either because");
                    args.Player.SendErrorMessage("the server key is incorrect or terraria-servers.com is offline!");
                    return;
                case Response.NotFound:
                    for (int i = 0; i < VoteNotFoundMessages.Count; i++)
                        args.Player.SendMessage(VoteNotFoundMessages[i].Text, VoteNotFoundMessages[i].GetColor());
                    return;
                case Response.VotedAndClaimed:
                    args.Player.SendInfoMessage("You have already claimed your reward today!");
                    return;
                case Response.VotedNotClaimed:
                    for (int i = 0; i < OnRewardClaimMessages.Count; i++)
                        args.Player.SendMessage(OnRewardClaimMessages[i].Text, OnRewardClaimMessages[i].GetColor());
                    //hand out reward
                    return;
            }
        }

        public Response CheckVote(string Username)
        {
            int I = -1;
            string s = wc.DownloadString(string.Format("http://terraria-servers.com/api/?object=votes&element=claim&key={0}&username={1}", config.ServerKey, Username));
            int.TryParse(s, out I);
            return (Response)I;
        }

        public void SetAsClaimed(string Username)
        {
            wc.DownloadString(string.Format("http://terraria-servers.com/api/?action=post&object=votes&element=claim&key={0}&username={1}", config.ServerKey, Username));
        }

        public enum Response
        {
            Error = -1,
            NotFound = 0,
            VotedNotClaimed = 1,
            VotedAndClaimed = 2
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

        public class Message
        {
            public string Text = "";
            public string Color = "41,179,169";
            public Message(string text)
            {
                Text = text;
            }
            public Color GetColor()
            {
                try
                {
                    string[] split = Color.Split(',');
                    float r = float.Parse(split[0]);
                    float g = float.Parse(split[1]);
                    float b = float.Parse(split[2]);
                    return new Color(r, g, b);
                }
                catch
                {
                    Console.WriteLine("Error parsing color in config.json, proper color format: 000,000,000");
                    Console.WriteLine("At line: " + Text);
                    return new Color(41, 179, 169);
                }
            }
        }

        public class Config
        {
            public string ServerKey = "ServerKeyGoesHere";
            public bool UsingSeconomy = false;
            public int SEconomyReward = 1000;
            public bool AnnounceOnReceive = true;
            public RewardItem[] RewardItems = new RewardItem[] { new RewardItem(5, 1), new RewardItem(6, 1) };
            public Message[] VoteNotFoundMessage = new Message[] {
                new Message("Vote not found!"),
                new Message("If you haven't voted yet, please go to terraria-servers.com and"),
                new Message("vote for the server to receive ingame rewards!")
            };
            public Message[] OnRewardClaimMessage = new Message[] {
                new Message("Thank you for voting on terraria-servers.com"),
                new Message("We really appreciate it!")            
            };
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
                VoteNotFoundMessages = new List<Message>(config.VoteNotFoundMessage);
                OnRewardClaimMessages = new List<Message>(config.OnRewardClaimMessage);
                args.Player.SendMessage("TSReward config reloaded sucessfully.", Color.Green);
            }
        }
    }
}
