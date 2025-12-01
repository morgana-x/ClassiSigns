using ClassiSigns.Commands;
using MCGalaxy;
using System.Collections.Generic;
using System.IO;

namespace ClassiSigns
{
    public class ClassiSigns : Plugin
    {
        public override string name => "classisigns";

        SignSender SignSender = new SignSender();

        public static Dictionary<string, SignModel> SignModels = new Dictionary<string, SignModel>()
        {
        };


        public class SignModel
        {
            public NamedPart[] Parts;

            public CustomModel Model;

            public SignModel(string path)
            {
                var model = BlockBench.Parse(System.IO.File.ReadAllText(path));

                Parts = model.ToParts();

                Model = new CustomModel()
                {
                    name = "sign",
                    partCount = (byte)Parts.Length,
                    uScale = model.resolution.width,
                    vScale = model.resolution.height,
                    bobbing = false,
                    collisionBounds = new MCGalaxy.Maths.Vec3F32(0, 0, 0),
                    pickingBoundsMax = new MCGalaxy.Maths.Vec3F32(16, 16, 16),
                    pickingBoundsMin = new MCGalaxy.Maths.Vec3F32(0, 0, 0),
                    usesHumanSkin = false,
                    calcHumanAnims = false,
                    eyeY = 0,
                    nameY = 0,
                    pushes = false
                };
            }
        }

        public static string DefaultSkinLink;
        public override void Load(bool auto)
        {
            if (!File.Exists("plugins/signskin.txt"))
                File.WriteAllText("plugins/signskin.txt", "https://garbage.loan/f/morgana/sign.png");
            else
                DefaultSkinLink = File.ReadAllText("plugins/signskin.txt").Trim();

            foreach(var s in FileIO.TryGetFiles("plugins/models/", "*.bbmodel"))
            {
                var filename = Path.GetFileNameWithoutExtension(s);
                if (!filename.StartsWith("sign"))
                    continue;
                SignModels.Add(filename, new SignModel(s));
                Player.Console.Message($"Added sign {filename} " + s);
            }

            SignSender.Load();
            Command.Register(new Commands.Sign());
        }

        public override void Unload(bool auto)
        {
            Command.Unregister(Command.Find("sign"));
            SignSender.Unload();
        }

    }
}
