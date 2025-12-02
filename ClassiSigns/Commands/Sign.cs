using MCGalaxy;
using System.Linq;
using System.Reflection;

namespace ClassiSigns.Commands
{
    public class Sign : Command2
    {
        public override string name => "sign";

        public override string type => "Building";

        public override LevelPermission defaultRank => LevelPermission.AdvBuilder;
        public override void Help(Player p)
        {
            p.Message("&a/sign [message]");
            p.Message("&a/sign [model] [message]");
            p.Message("eg: &5/sign Hello!");
            p.Message("eg: &5/sign signwall Hi there!");
        }

        public override void Use(Player p, string message)
        {
            if (p == Player.Console)
            {
                p.Message("have to be in-game to use this!");
                return;
            }

            var args = message.Split(' ').ToList();
            if (args.Count < 1)
            {
                Help(p);
                return;
            }

            if (args.Count == 1 || !ClassiSigns.SignModels.ContainsKey(args[0]))
            {
                args.Insert(0, ClassiSigns.SignModels.First().Key);
            }

            string signtext = string.Join(" ", args.ToArray(), 1, args.Count - 1).TrimEnd();

            if (signtext.Trim() == "")
            {
                p.Message("Cannot have a blank sign!!!");
                return;
            }

            string signmodel = args[0];// +"_"+string.Join(" ", args.ToArray(), 1, args.Count - 1).TrimEnd();

            var playerbot = new PlayerBot($"sign_{p.name}_{p.level.Bots.Items.Where((x => { return x.name.StartsWith($"sign_{p.name}") || (x.Model.StartsWith("sign") && x.Owner == p.name); })).Count()}", p.level);


            playerbot.SkinName = ClassiSigns.DefaultSkinLink;
            playerbot.SetInitialPos(p.Pos);
            playerbot.Rot = p.Rot;
            playerbot.AIName = signtext;
            playerbot.SetModel(signmodel);
            playerbot.Owner = p.name;
            PlayerBot.Add(playerbot);

            p.Message($"Spawned sign with text \"{signtext}\"");
        }
    }
}
