using MCGalaxy.Events.EntityEvents;
using MCGalaxy;
using System.Collections.Generic;
using System.Linq;
using MCGalaxy.Network;
using MCGalaxy.Events.PlayerEvents;

namespace ClassiSigns
{
    public class SignSender
    {
        public void Load()
        {
            OnEntityDespawnedEvent.Register(EntityDespawned, Priority.Normal);
            OnPlayerDisconnectEvent.Register(Disconnect, Priority.Normal);
            OnSentMapEvent.Register(SentMap, Priority.Normal);
            OnSendingModelEvent.Register(SendingModel, Priority.Low);
        }

        public void Unload()
        {
            OnEntityDespawnedEvent.Unregister(EntityDespawned);
            OnSendingModelEvent.Unregister(SendingModel);
            OnPlayerDisconnectEvent.Unregister(Disconnect);
            OnSentMapEvent.Unregister(SentMap);
        }

        public class SignInstance
        {
            public byte Id;
            public DefinedSign DefinedSign;
        }


        public static Dictionary<Player, Dictionary<string, SignInstance>> DefinedSigns = new Dictionary<Player, Dictionary<string, SignInstance>>();

        public static bool Defined(Player dst, string signmodel)
        {
            return DefinedSigns.ContainsKey(dst) && DefinedSigns[dst].ContainsKey(signmodel);
        }
        public static void Define(Player dst, string model)
        {
            if (!dst.Session.hasCpe || (!dst.Session.Supports(CpeExt.CustomModels, 1) && !dst.Session.Supports(CpeExt.CustomModels, 2)))
                return;

            lock (DefinedSigns)
            {
                if (Defined(dst, model))
                    return;

                if (!TryParseSignModel(model, out string signmodel, out string signtext))
                    return;

                var signid = GetFreeID(dst);
                var signinstance = new SignInstance() { Id = signid, DefinedSign = SignGen.GenerateSignModel(signid, model, signtext, signmodel) };

                if (!DefinedSigns.ContainsKey(dst))
                    DefinedSigns.Add(dst, new Dictionary<string, SignInstance>());

                if (Defined(dst, model))
                    return;
                DefinedSigns[dst].Add(model, signinstance);
                SignGen.DefineSignPacket(dst, signinstance.Id, signinstance.DefinedSign);
            }
        }
        public static void Undefine(Player dst, string signmodel)
        {
            if (!DefinedSigns.ContainsKey(dst))
                return;

            if (!DefinedSigns[dst].ContainsKey(signmodel))
                return;

            dst.Send(Packet.UndefineModel(DefinedSigns[dst][signmodel].Id));
            DefinedSigns[dst].Remove(signmodel);
        }

        void EntityDespawned(Entity e, Player dst)
        {
            Undefine(dst, e.Model);
        }

        void SendingModel(Entity e, ref string model, Player dst)
        {
            if (e is PlayerBot && ((PlayerBot)e).AIName != null && ClassiSigns.SignModels.ContainsKey(e.Model))
            {
                string storedsign = e.Model + "_" + ((PlayerBot)e).AIName;
                Define(dst, storedsign);
                return;
            }

            if (!model.StartsWith("sign") && !model.Contains("_"))
                return;

            Define(dst, model);
        }

        void SentMap(Player p, Level prevLevl, Level level)
        {
            if (DefinedSigns.ContainsKey(p))
                DefinedSigns.Remove(p);
        }
 
        void Disconnect(Player p, string reason)
        {
            if (DefinedSigns.ContainsKey(p))
                DefinedSigns.Remove(p);
        }

        static bool TryParseSignModel(string modelname, out string signmodel, out string signtext)
        {
            signmodel = string.Empty;
            signtext = string.Empty;

            if (!modelname.StartsWith("sign"))
                return false;

            if (!modelname.Contains("_"))
                return false;

            var underscore = modelname.IndexOf('_');
            if (underscore >= modelname.Length - 1)
                return false;

            signmodel = modelname.Substring(0, underscore).Trim();

            if (!ClassiSigns.SignModels.ContainsKey(signmodel))
                return false;

            signtext = modelname.Substring(underscore + 1);
            return true;
        }


        public static byte GetFreeID(Player p)
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
      
    }
}
