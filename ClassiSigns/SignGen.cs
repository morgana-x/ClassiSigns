using MCGalaxy.Network;
using MCGalaxy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassiSigns
{
    public class DefinedSign
    {
        public CustomModel Model { get; set; }
        public List<CustomModelPart> Parts { get; set; }
        public string Text { get; set; }
        public string Name { get; set;}
    }

    public class SignGen
    {
        public static Dictionary<int, float> WidthMap = FontWidth.CalculateTextWidths();
        public static DefinedSign GenerateSignModel(byte modelID, string modelname, string signtext, string signmodel)
        {
            signtext = signtext.Replace("\n", "\\n");
            List<string> lines = new List<string>();

            int idx = 0;
            int l = 0;
            string line = "";
            while (lines.Count < 4)
            {
                bool linebreak = (idx < signtext.Length - 1 && signtext[idx] == '\\' && signtext[idx + 1] == 'n');
                if (l > 15 || (idx >= signtext.Length) || linebreak)
                {
                    lines.Add(line);
                    line = "";
                    if (linebreak)
                        idx += 2;
                    l = 0;
                    continue;
                }
                line += signtext[idx];
              //  if (signtext[idx] != ' ')
                    l++;
                idx++;
            }
            var signModel = ClassiSigns.SignModels.ContainsKey(signmodel) ? ClassiSigns.SignModels[signmodel] : ClassiSigns.SignModels.Values.FirstOrDefault();
            var signParts = Array.ConvertAll(signModel.Parts, new Converter<NamedPart, NamedPart>(x => x));



            List<CustomModelPart> parts = new List<CustomModelPart>();
            foreach (var part in signParts)
                parts.Add(part.Part);


            var topofsign = 13.85f;
            var board = signParts.Where((spart) => { return spart.Name == "board"; }).First();
            if (board != null && parts.Contains(board.Part))
                topofsign = (board.Part.max.Y * 16f) - 1.75f;

            float characterSize = 1.5f; // 1.05f

            for (int x = 0; x < 4; x++)
            {
                string text = lines[x];
                float textX = 0f;
                for (int i = 0; i < text.Length; i++)
                {
                    int c = text[i];

                    int cx = (c & 0xF) * 8;
                    int cy = (c >> 4) * 8;

                    ushort srcX = (ushort)(cx); // u1
                    ushort srcY = (ushort)(cy); // v1
                    ushort dstX = (ushort)(cx + 8); // u2
                    ushort dstY = (ushort)(cy + 8); // v2

                    if (text[i].ToString().ToUpper() == text[i].ToString())
                    {
                        dstY -= 16;
                        srcY -= 16;
                    }

                    
                    var pos = new MCGalaxy.Maths.Vec3F32((6.3f - ((textX / 8f) * characterSize)) / 16f, (topofsign - (x * 1.55f)) / 16f, -0.505f / 16f); //new MCGalaxy.Maths.Vec3F32((6.75f - (i * 1.05f)) / 16f, (14.15f - (x * 1.75f))/16f, -0.505f / 16f);

                    textX += (WidthMap.ContainsKey(c) ? WidthMap[c] : 8) + 1;
                    if (text[i] == ' ') continue; // Don't add a part for a space
                    parts.Add(new CustomModelPart()
                    {
                        max = pos + new MCGalaxy.Maths.Vec3F32(characterSize / 16f, characterSize / 16f, 0),
                        min = pos + new MCGalaxy.Maths.Vec3F32(0, 0, 0),

                        u1 = new ushort[6] { 0, 0, 0, srcX, 0, 0 },
                        u2 = new ushort[6] { 1, 1, 1, dstX, 1, 1 },
                        v1 = new ushort[6] { 16, 16, 16, srcY, 16, 16 },
                        v2 = new ushort[6] { 17, 17, 17, dstY, 17, 17 },
                        rotation = new MCGalaxy.Maths.Vec3F32(0, 0, 0),
                        anims = new CustomModelAnim[Packet.MaxCustomModelAnims] { new CustomModelAnim() { type = CustomModelAnimType.None }, new CustomModelAnim() { type = CustomModelAnimType.None }, new CustomModelAnim() { type = CustomModelAnimType.None }, new CustomModelAnim() { type = CustomModelAnimType.None } }
                    });
                }
            }




         
            return new DefinedSign()
            {
                Model = new CustomModel()
                {
                    name = modelname,
                    partCount = (byte)parts.Count,
                    uScale = signModel.Model.uScale,
                    vScale = signModel.Model.vScale,
                    bobbing = false,
                    collisionBounds = new MCGalaxy.Maths.Vec3F32(0, 0, 0),
                    pickingBoundsMax = new MCGalaxy.Maths.Vec3F32(16, 16, 16),
                    pickingBoundsMin = new MCGalaxy.Maths.Vec3F32(0, 0, 0),
                    usesHumanSkin = false,
                    calcHumanAnims = false,
                    eyeY = 1f,
                    nameY = 1f,
                    pushes = false
                },
                Parts = parts,
                Text = string.Join("\n", lines.ToArray()),
                Name = $"{signmodel}_{string.Join("\\n", lines.ToArray())}",
            }; ;
        }

        public static void DefineSignPacket(Player p, byte id, DefinedSign sign)
        {
            if (!p.Session.Supports(CpeExt.CustomModels, 1) && !p.Session.Supports(CpeExt.CustomModels, 2))
                return;

            bool v2 = p.Session.Supports(CpeExt.CustomModels, 2);

            p.Send(Packet.DefineModel(id, sign.Model));

            foreach (var part in sign.Parts)
            {

                if (part.anims.Length < 4)
                {
                    part.anims = new CustomModelAnim[4];
                    for (int i = 0; i < Packet.MaxCustomModelAnims; i++)
                        part.anims[i] = new CustomModelAnim() { type = CustomModelAnimType.None };
                }

                if (v2)
                    p.Send(Packet.DefineModelPartV2(id, part));
                else
                    p.Send(Packet.DefineModelPart(id, part));
            }
        }
    }
}
