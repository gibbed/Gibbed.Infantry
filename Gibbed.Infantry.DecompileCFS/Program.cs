/* Copyright (c) 2011 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Gibbed.Infantry.FileFormats;
using NDesk.Options;

namespace Gibbed.Infantry.DecompileCFS
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        public static void Main(string[] args)
        {
            bool showHelp = false;
            bool dontFixSpecialColors = false;

            OptionSet options = new OptionSet()
            {
                {
                    "ncf|no-color-fix",
                    "don't fix special colors (such as shadows, lights)",
                    v => dontFixSpecialColors = v != null
                },
                {
                    "h|help",
                    "show this message and exit", 
                    v => showHelp = v != null
                },
            };

            List<string> extras;

            try
            {
                extras = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return;
            }

            if (extras.Count < 1 || extras.Count > 2 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_cfs [output_png]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string inputPath = extras[0];
            string outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, ".png");

            SpriteFile sprite;
            using (var input = File.OpenRead(inputPath))
            {
                sprite = new SpriteFile();
                sprite.Deserialize(input);
            }

            var bitmap = new Bitmap(
                sprite.Width * sprite.ColumnCount,
                sprite.Height * sprite.RowCount,
                PixelFormat.Format8bppIndexed);

            var palette = bitmap.Palette;
            var shadowIndex = 256 - sprite.ShadowCount;
            var lightIndex = shadowIndex - sprite.LightCount;

            for (int i = 0; i < 256; i++)
            {
                var color = sprite.Palette[i];
                
                var r = (int)((color >> 16) & 0xFF);
                var g = (int)((color >> 8) & 0xFF);
                var b = (int)((color >> 0) & 0xFF);
                //var a = (int)((color >> 24) & 0xFF);
                
                int a;
                
                if (i == 0)
                {
                    // transparent pixel
                    a = 0;
                }
                else if (sprite.ShadowCount > 0 && i >= shadowIndex)
                {
                    if (dontFixSpecialColors == false)
                    {
                        // make shadows black+alpha
                        a = 64 + (((i - shadowIndex) + 1) * 16);
                        r = g = b = 0;
                    }
                    else
                    {
                        a = 255;
                    }
                }
                else if (sprite.LightCount > 0 && i >= lightIndex)
                {
                    if (dontFixSpecialColors == false)
                    {
                        // make lights white+alpha
                        a = 64 + (((i - lightIndex) + 1) * 4);
                        r = g = b = 255;
                    }
                    else
                    {
                        a = 255;
                    }
                }
                /*else if (i > sprite.MaxSolidIndex)
                {
                    a = 0;
                }*/
                else
                {
                    a = 255;
                }

                palette.Entries[i] = Color.FromArgb(a, r, g, b);
            }
            bitmap.Palette = palette;
            
            for (int i = 0, y = 0; y < sprite.Height * sprite.RowCount; y += sprite.Height)
            {
                for (int x = 0; x < sprite.Width * sprite.ColumnCount; x += sprite.Width)
                {
                    var frame = sprite.Frames[i++];

                    if (frame.Width == 0 ||
                        frame.Height == 0)
                    {
                        continue;
                    }

                    var area = new Rectangle(
                        x + frame.X, y + frame.Y,
                        frame.Width, frame.Height);
                    
                    var data = bitmap.LockBits(area, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                    var scan = data.Scan0;
                    for (int o = 0; o < frame.Height * frame.Width; o += frame.Width)
                    {
                        Marshal.Copy(frame.Pixels, o, scan, frame.Width);
                        scan += data.Stride;
                    }
                    bitmap.UnlockBits(data);
                }
            }

            bitmap.Save(outputPath, ImageFormat.Png);
        }
    }
}
