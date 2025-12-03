using MCGalaxy;
using System.Linq;

namespace ClassiSigns.Commands
{
    public class SignEdit : Command2
    {
        public override string name => "signedit";

        public override string type => "Building";

        public override LevelPermission defaultRank => LevelPermission.Admin;
        public override void Help(Player p)
        {
            p.Message("&a/signedit [bot] [message]");
            p.Message($"eg: &5/signedit sign_{p.name}_0 Hello!");
        }

        public override void Use(Player p, string message)
        {
            if (p == Player.Console)
            {
                p.Message("have to be in-game to use this!");
                return;
            }
            var args = message.Split(' ').ToList();
            if (args.Count < 2)
            {
                Help(p);
                return;
            }

            var found = p.level.Bots.Items.ToList().Where((x) => { return x.name == args[0]; });
            if (found.Count() == 0)
            {
                p.Message($"&cCouldn't find bot {args[0]}!");
                return;
            }
            var playerbot = found.First();

            string signmodel = playerbot.Model;

            if (!ClassiSigns.SignModels.ContainsKey(signmodel))
            {
                p.Message("Bot is not a valid sign!");
                return;
            }

            if (!PlayerBot.CanEditAny(p) && !playerbot.Owner.CaselessEq(p.name))
            {
                p.Message("You cannot edit this sign!");
                return;
            }

            string signtext =  string.Join(" ", args.ToArray(), 1, args.Count - 1).TrimEnd();

            playerbot.SkinName = ClassiSigns.DefaultSkinLink;
            playerbot.AIName = signtext;
            playerbot.UpdateModel(signmodel);

            p.Message($"Changed signs text to \"{signtext}\"");
        }
    }
}
