using MCGalaxy;
using MCGalaxy.Util;
using MCGalaxy.Util.Imaging;
using System;
using System.Collections.Generic;
using System.Drawing;
namespace ClassiSigns
{
    public unsafe class FontWidth
    {

        public static Dictionary<int, float> CalculateTextWidths(string path="plugins/models/sign.png")
        {
            
            return CalculateTextWidths(System.IO.File.ReadAllBytes(path));
        }

       
        public static Dictionary<int, float> CalculateTextWidths(byte[] bitmapdata)
        {
            var texture = ImageUtils.DecodeImage(bitmapdata, Player.Console);

            Dictionary<int, float> tileWidths = new Dictionary<int, float>();
            if (texture == null) return tileWidths;
            int width = texture.Width;
            int height = texture.Height;
            Logger.Log(LogType.ConsoleMessage, width.ToString());
            Logger.Log(LogType.ConsoleMessage, height.ToString());
            int i = 0;
            int x = 0;
            int y = 0;
            int xx = 0;
            int tileY=0;

            int tileSize = 8;

            texture.LockBits();
            for (y = 0; y < height; y++)
            {
                tileY = y / tileSize;

                i = 0 | (tileY << 4);
                /* Iterate through each tile on current scanline */
                for (x = 0; x < width; x += tileSize, i++)
                {
                    /* Iterate through each pixel of the given character, on the current scanline */
                    for (xx = tileSize - 1; xx >= 0; xx--)
                    {
                        if (texture.Get(x+xx, y).A == 0) { continue; }
                       
                        /* Check if this is the pixel furthest to the right, for the current character */
                        tileWidths[i]= Math.Max(tileWidths.ContainsKey(i) ? tileWidths[i] : 0, xx + 1);
                        //Logger.Log(LogType.ConsoleMessage,($"{((char)i).ToString()}:  {tileWidths[i]}"));
                        break;
                    }
                }
            }
            texture.UnlockBits();
            texture.Dispose();
            tileWidths[' '] = tileSize / 4;

            return tileWidths;
        }

    }
}
