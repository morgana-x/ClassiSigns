using MCGalaxy.Events.EntityEvents;
using MCGalaxy;
using System;
using System.Collections.Generic;
using System.Linq;
using MCGalaxy.Network;
using MCGalaxy.Events.PlayerEvents;
namespace ClassiSigns
{
    public class SignSender
    {
        Dictionary<int, float> WidthMap;
        public void Load()
        {
            WidthMap = FontWidth.CalculateTextWidths();

            OnEntityDespawnedEvent.Register(EntityDespawned, Priority.Normal);
            OnSendingModelEvent.Register(SendingModel, Priority.Critical);
            OnPlayerDisconnectEvent.Register(Disconnect, Priority.Normal);
        }

        public void Unload()
        {
            OnEntityDespawnedEvent.Unregister(EntityDespawned);
            OnSendingModelEvent.Unregister(SendingModel);
            OnPlayerDisconnectEvent.Unregister(Disconnect);
        }


        void EntityDespawned(Entity e, Player dst)
        {
            if (DefinedSigns[dst].ContainsKey(e.Model))
                DefinedSigns[dst].Remove(e.Model);
        }

        void SendingModel(Entity e, ref string model, Player dst)
        {
            if (!model.StartsWith("sign") && !model.Contains("_"))
                return;

            SendSign(dst, ref model);
        }

        public class DefinedSign
        {
            public byte Id;
            public CustomModel Model;
            public List<CustomModelPart> Parts;
        }

        volatile Dictionary<Player, Dictionary<string, DefinedSign>> DefinedSigns = new Dictionary<Player, Dictionary<string, DefinedSign>>();

        void Disconnect(Player p, string reason)
        {
            if (DefinedSigns.ContainsKey(p))
                DefinedSigns.Remove(p);
        }
        public void SendSign(Player p, ref string modelname)
        {
            if (!modelname.StartsWith("sign"))
                return;

            if (!p.Session.hasCpe || (!p.Session.Supports(CpeExt.CustomModels, 1) && !p.Session.Supports(CpeExt.CustomModels, 2)))
                return;

            if (!modelname.Contains("_"))
                return;

            var underscore = modelname.IndexOf('_');
            if (underscore >= modelname.Length - 1)
                return;

            string signmodel = modelname.Substring(0, underscore);

            if (!ClassiSigns.SignModels.ContainsKey(signmodel))
                return;

            if (!DefinedSigns.ContainsKey(p))
                DefinedSigns[p] = new Dictionary<string, DefinedSign>();
          

            if (!DefinedSigns[p].ContainsKey(modelname))
                modelname = DefineSign(p, modelname.Substring(underscore+1), signmodel);

            if (!DefinedSigns[p].ContainsKey(modelname))
                return;

            var sign = DefinedSigns[p][modelname];

            p.Send(Packet.DefineModel(sign.Id, sign.Model));

            foreach(var part in sign.Parts)
            {
                if (part.anims.Length < 4)
                {
                    part.anims = new CustomModelAnim[4];
                    for (int i = 0; i < Packet.MaxCustomModelAnims; i++)
                        part.anims[i] = new CustomModelAnim() { type = CustomModelAnimType.None};
                }
                if (p.Supports(CpeExt.CustomModels, 2))
                    p.Send(Packet.DefineModelPartV2(sign.Id, part));
                else
                    p.Send(Packet.DefineModelPart(sign.Id, part));
            }
        }

        public void UndefineSign(Player p, string modelname)
        {
            if (!DefinedSigns.ContainsKey(p))
                return;
            if (!DefinedSigns[p].ContainsKey(modelname))
                return;

             p.Send(Packet.UndefineModel(DefinedSigns[p][modelname].Id));

            DefinedSigns[p].Remove(modelname);
        }

        public byte GetFreeID(Player p)
        {
            byte id = Packet.MaxCustomModels-1;
            if (!DefinedSigns.ContainsKey(p))
                return id;
            lock (DefinedSigns)
            {
                foreach (var s in DefinedSigns[p].ToList())
                    if (s.Value.Id == id)
                        id--;
            }
            
            return id;
        }
        public string DefineSign(Player p, string signtext, string signmodel)
        {
            List<string> lines = new List<string>();

            int idx = 0;
            int l = 0;
            string line = "";
            while (lines.Count < 4)
            {
                bool linebreak = ( idx < signtext.Length - 1 && signtext[idx] == '\\' && signtext[idx + 1] == 'n');
                if ( l >= 0xD || (idx >= signtext.Length) || linebreak)
                {
                    lines.Add(line);
                    line = "";
                    if (linebreak)
                        idx += 2;
                    l = 0;
                    continue;
                }
                line += signtext[idx];
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

            for (int x = 0; x < 4; x++)
            {
                string text = lines[x];
                float textX = 0f;
                for (int i=0;i<text.Length;i++)
                {
                    int c = text[i].UnicodeToCp437();
          
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
                    var pos = new MCGalaxy.Maths.Vec3F32((6.75f - ((textX/8f)*1.05f)) / 16f, (topofsign - (x * 1.55f)) / 16f, -0.505f / 16f); //new MCGalaxy.Maths.Vec3F32((6.75f - (i * 1.05f)) / 16f, (14.15f - (x * 1.75f))/16f, -0.505f / 16f);
                    
                    textX += (WidthMap.ContainsKey(c) ? WidthMap[c] : 8) + 1;
                   
                    parts.Add(new CustomModelPart()
                    {
                        max = pos + new MCGalaxy.Maths.Vec3F32(1.05f / 16f, 1.05f / 16f,0),
                        min = pos + new MCGalaxy.Maths.Vec3F32(0, 0, 0),

                        u1 = new ushort[6] { 0, 0, 0,  srcX, 0, 0 },
                        u2 = new ushort[6] { 1, 1, 1,  dstX, 1, 1 },
                        v1 = new ushort[6] { 16, 16, 16,  srcY, 16, 16 },
                        v2 = new ushort[6] { 17, 17, 17,  dstY, 17, 17 },
                        rotation = new MCGalaxy.Maths.Vec3F32(0, 0, 0),
                        anims = new CustomModelAnim[Packet.MaxCustomModelAnims] { new CustomModelAnim() { type = CustomModelAnimType.None }, new CustomModelAnim() { type = CustomModelAnimType.None }, new CustomModelAnim() { type = CustomModelAnimType.None }, new CustomModelAnim() { type = CustomModelAnimType.None } }
                    });
                }
            }

       
    

            if (!DefinedSigns.ContainsKey(p))
                DefinedSigns[p] = new Dictionary<string, DefinedSign>();

            string modelname = $"{signmodel}_{signtext}";
            var definedsign = new DefinedSign() { Model = new CustomModel()
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
            }, Id = GetFreeID(p), Parts = parts };


            if (DefinedSigns[p].ContainsKey(modelname))
                DefinedSigns[p][modelname] = definedsign;
            else
                DefinedSigns[p].Add(modelname, definedsign);

            return modelname;
        }
    }
}
