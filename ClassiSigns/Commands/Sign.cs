using MCGalaxy;
using System.Linq;

namespace ClassiSigns.Commands
{
    public class Sign : Command2
    {
        public override string name => "sign";

        public override string type => "Building";

        public override LevelPermission defaultRank => LevelPermission.AdvBuilder;
        public override void Help(Player p)
        {
            p.Message("/sign model text here\\nline2\\nline3\\nline4");
            p.Message("eg: /sign sign text here\\nline2\\nline3\\nline4");
            p.Message("eg: /sign signwall text here\\nline2\\nline3\\nline4");
        }

        public override void Use(Player p, string message)
        {
            var args = message.Split(' ');
            if (args.Length < 2)
            {
                Help(p);
                return;
            }
            if (!ClassiSigns.SignModels.ContainsKey(args[0]))
            {
                p.Message(args[0] + " is not a valid model");
                return;
            }

            var signmodelname = args[0]+"_"+string.Join(" ", args, 1, args.Count() - 1).TrimEnd();

            var playerbot = new PlayerBot($"sign_{p.name}_{p.level.Bots.Items.Where((x => { return x.name.StartsWith($"sign_{p.name}") || (x.Model.StartsWith("sign") && x.Owner == p.name); })).Count()}", p.level);

            playerbot.SetModel(signmodelname);
          //  playerbot.DisplayName = "";
            playerbot.SkinName = ClassiSigns.DefaultSkinLink;
            playerbot.AIName = signmodelname;
            playerbot.SetInitialPos(p.Pos);
            playerbot.Rot = p.Rot;

            PlayerBot.Add(playerbot);
            playerbot.GlobalSpawn();
            p.Message($"Spawned sign, model {signmodelname}");
        }
    }
}
