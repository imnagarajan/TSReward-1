using System;
using Terraria;
using TShockAPI;
using TerrariaApi.Server;
using System.Reflection;
using System.Net;

namespace TSREWARD
{
    [ApiVersion(1, 16)]
    public class TSReward : TerrariaPlugin
    {
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
                    args.Player.SendErrorMessage("Vote not found!");
                    args.Player.SendInfoMessage("If you haven't voted yet, please go to terraria-servers.com and"); //replace with config messages
                    args.Player.SendInfoMessage("vote for the server to receive ingame rewards!"); //replace with config messages
                    return;
                case Response.VotedAndClaimed:
                    args.Player.SendInfoMessage("You have already claimed your reward today!");
                    return;
                case Response.VotedNotClaimed:
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

        public class Config
        {
            public string ServerKey = "ServerKeyGoesHere";
            public bool UsingSeconomy = false;
            public int SEconomyReward = 1000;
            public RewardItem[] RewardItems = new RewardItem[] { new RewardItem(5,1), new RewardItem(6,1) };
        }
    }
}
