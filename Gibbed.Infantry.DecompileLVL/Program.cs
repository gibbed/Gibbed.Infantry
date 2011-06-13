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
using System.IO;
using Gibbed.Helpers;
using Gibbed.Infantry.FileFormats;
using NDesk.Options;

/* TODO: for old levels that use .lvb for resources,
 * detect object/floor .blos they really came from. */

namespace Gibbed.Infantry.DecompileLVL
{
    public class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        public static void Main(string[] args)
        {
            bool showHelp = false;

            OptionSet options = new OptionSet()
            {
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
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_lvl [output_map]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string inputPath = extras[0];
            string outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, ".map");
            string lvbPath = Path.ChangeExtension(Path.GetFileName(inputPath), ".lvb");

            using (var input = File.OpenRead(inputPath))
            {
                var level = new LevelFile();
                level.Deserialize(input);

                using (var output = File.Create(outputPath))
                {
                    var header = new Map.Header();

                    header.Version = 9;
                    header.Width = level.Width;
                    header.Height = level.Height;
                    header.OffsetX = level.OffsetX;
                    header.OffsetY = level.OffsetY;
                    header.EntityCount = level.Entities.Count;

                    header.PhysicsLow = new short[32];
                    Array.Copy(level.PhysicsLow, header.PhysicsLow, level.PhysicsLow.Length);
                    header.PhysicsHigh = new short[32];
                    Array.Copy(level.PhysicsHigh, header.PhysicsHigh, level.PhysicsHigh.Length);

                    header.LightColorWhite = level.LightColorWhite;
                    header.LightColorRed = level.LightColorRed;
                    header.LightColorGreen = level.LightColorGreen;
                    header.LightColorBlue = level.LightColorBlue;

                    output.WriteStructure(header);

                    for (int i = 0; i < 8192; i++)
                    {
                        if (i < level.TerrainIds.Length)
                        {
                            output.WriteValueU8((byte)level.TerrainIds[i]);
                        }
                        else
                        {
                            output.WriteValueU8(0);
                        }
                    }

                    for (int i = 0; i < 2048; i++)
                    {
                        var reference = new Map.BlobReference();

                        if (i < level.Floors.Count)
                        {
                            var floor = level.Floors[i];

                            reference.Path = string.Format("{0},{1}",
                                floor.FileName ?? lvbPath,
                                floor.Id);
                        }

                        output.WriteStructure(reference);
                    }

                    var tiles = new byte[level.Width * level.Height * 8];
                    int offset = 0;

                    for (int i = 0; i < level.Tiles.Length; i++, offset += 2)
                    {
                        tiles[offset + 0] = 1;
                        tiles[offset + 1] = level.Tiles[i].A;
                        tiles[offset + 1] &= 0x7F;
                    }

                    for (int i = 0; i < level.Tiles.Length; i++, offset += 2)
                    {
                        tiles[offset + 0] = 1;
                        tiles[offset + 1] = 0;
                    }

                    for (int i = 0; i < level.Tiles.Length; i++, offset += 2)
                    {
                        tiles[offset + 0] = 1;
                        tiles[offset + 1] = level.Tiles[i].C;
                    }

                    for (int i = 0; i < level.Tiles.Length; i++, offset += 2)
                    {
                        tiles[offset + 0] = 1;
                        tiles[offset + 1] = level.Tiles[i].B;
                    }

                    output.WriteValueS32(tiles.Length);
                    output.Write(tiles, 0, tiles.Length);

                    for (int i = 0; i < level.Entities.Count; i++)
                    {
                        output.WriteStructure(level.Entities[i]);

                        var obj = level.Objects[level.Entities[i].ObjectId];

                        var reference = new Map.BlobReference();
                        reference.Path = string.Format("{0},{1}",
                                obj.FileName ?? lvbPath,
                                obj.Id);

                        output.WriteStructure(reference);
                    }
                }
            }
        }
    }
}
