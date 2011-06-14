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
using System.Linq;
using System.Xml.XPath;
using Gibbed.Helpers;
using Gibbed.Infantry.FileFormats;
using NDesk.Options;
using Level = Gibbed.Infantry.FileFormats.Level;
using Map = Gibbed.Infantry.FileFormats.Map;

namespace GenerateSpaceMap
{
    internal class Program
    {
        private static string GetExecutablePath()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
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

            if (extras.Count < 0 || extras.Count > 1 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ [output_map]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            var outputPath = extras.Count > 0 ? extras[0] : "space.map";

            var templates = LoadEntities();

            const int width = 1024;
            const int height = 1024;

            var rng = new MersenneTwister();
            var noise = PerlinNoise.Generate(
                width, height, 0.0325f, 1.0f, 0.5f, 16, rng);

            var physics = new bool[width, height];
            var vision = new bool[width, height];

            var entities = new List<Entity>();

            for (int x = 8; x < width - 8; x++)
            {
                for (int y = 8; y < height - 8; y++)
                {
                    var magic = noise[x, y];

                    if (magic >= 200)
                    {
                    }
                    else if (magic >= 180)
                    {
                        if (rng.Next(100) >= 60 &&
                            (x % 2) == 0 &&
                            (y % 2) == 0)
                        {
                            var template = templates
                                .Where(t => t.Category == "asteroid")
                                .OrderBy(t => rng.Next())
                                .FirstOrDefault();

                            if (template != null &&
                                template.CanPlaceWithPhysics(x, y, physics, width, height) == true)
                            {
                                template.BlockPhysics(x, y, physics);
                                entities.Add(new Entity(x, y, template));
                            }
                        }
                    }
                    else if (magic >= 100)
                    {
                    }
                    else if (magic >= 15)
                    {
                    }
                    else
                    {
                        if (rng.Next(100) >= 80)
                        {
                            var template = templates
                                .Where(t => t.Category == "nebula")
                                .OrderBy(t => rng.Next())
                                .FirstOrDefault();

                            if (template != null &&
                                template.CanPlaceWithVision(x, y, vision, width, height) == true)
                            {
                                template.BlockVision(x, y, vision);
                                entities.Add(new Entity(x, y, template));
                            }
                        }
                    }
                }
            }

            var tiles = new Level.Tile[width, height];

            foreach (var entity in entities)
            {
                for (int rx = 0; rx < entity.Template.Width; rx++)
                {
                    for (int ry = 0; ry < entity.Template.Height; ry++)
                    {
                        if (entity.Template.Physics[rx, ry] > 0)
                        {
                            tiles[entity.X + rx, entity.Y + ry].Physics =
                                entity.Template.Physics[rx, ry];
                        }

                        if (entity.Template.Vision[rx, ry] > 0)
                        {
                            tiles[entity.X + rx, entity.Y + ry].Vision =
                                entity.Template.Vision[rx, ry];
                        }
                    }
                }
            }

            var floors = new List<Map.BlobReference>();
            floors.Add(new Map.BlobReference() { Path = "f_default.blo,default.cfs" });
            //floors.Add(new Map.BlobReference() { Path = "f_colors.blo,color3.cfs" });

            using (var output = File.Create(outputPath))
            {
                var header = new Map.Header();

                header.Version = 9;
                header.Width = width;
                header.Height = height;

                header.EntityCount = entities.Count;

                header.LightColorWhite = 0xFFFFFF00u;
                header.LightColorRed = 0x0000FF00u;
                header.LightColorGreen = 0x00FF0000u;
                header.LightColorBlue = 0xFF000000u;

                header.PhysicsLow = new short[32];
                
                header.PhysicsHigh = new short[32];
                header.PhysicsHigh[0] = 0;
                header.PhysicsHigh[1] = 1024;
                header.PhysicsHigh[2] = 1024;
                header.PhysicsHigh[3] = 1024;
                header.PhysicsHigh[4] = 1024;
                header.PhysicsHigh[5] = 1024;
                header.PhysicsHigh[6] = 16;
                header.PhysicsHigh[7] = 16;
                header.PhysicsHigh[8] = 16;
                header.PhysicsHigh[9] = 16;
                header.PhysicsHigh[10] = 16;
                header.PhysicsHigh[11] = 32;
                header.PhysicsHigh[12] = 32;
                header.PhysicsHigh[13] = 32;
                header.PhysicsHigh[14] = 32;
                header.PhysicsHigh[15] = 32;
                header.PhysicsHigh[16] = 64;
                header.PhysicsHigh[17] = 64;
                header.PhysicsHigh[18] = 64;
                header.PhysicsHigh[19] = 64;
                header.PhysicsHigh[20] = 64;
                header.PhysicsHigh[21] = 128;
                header.PhysicsHigh[22] = 128;
                header.PhysicsHigh[23] = 128;
                header.PhysicsHigh[24] = 128;
                header.PhysicsHigh[25] = 128;
                header.PhysicsHigh[26] = 1024;
                header.PhysicsHigh[27] = 1024;
                header.PhysicsHigh[29] = 1024;
                header.PhysicsHigh[28] = 1024;
                header.PhysicsHigh[30] = 1024;
                header.PhysicsHigh[31] = 1024;

                output.WriteStructure(header);

                for (int i = 0; i < 8192; i++)
                {
                    if (i < 16)
                    {
                        output.WriteValueU8((byte)i);
                    }
                    else
                    {
                        output.WriteValueU8(0);
                    }
                }

                for (int i = 0; i < 2048; i++)
                {
                    if (i < floors.Count)
                    {
                        output.WriteStructure(floors[i]);
                    }
                    else
                    {
                        output.Seek(64, SeekOrigin.Current);
                    }
                }

                var buffer = new byte[width * height * 4];
                for (int y = 0, offset = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++, offset += 4)
                    {
                        buffer[offset + 0] = tiles[x, y].BitsA;
                        buffer[offset + 1] = 0;
                        buffer[offset + 2] = tiles[x, y].BitsC;
                        buffer[offset + 3] = tiles[x, y].BitsB;
                    }
                }

                using (var rle = new MemoryStream())
                {
                    rle.WriteRLE(buffer, 4, width * height, false);
                    rle.Position = 0;

                    output.WriteValueS32((int)rle.Length);
                    output.WriteFromStream(rle, rle.Length);
                }

                foreach (var entity in entities)
                {
                    var levelEntity = new Level.Entity();
                    levelEntity.X = (short)((entity.X - entity.Template.OffsetX) * 16);
                    levelEntity.Y = (short)((entity.Y - entity.Template.OffsetY) * 16);
                    output.WriteStructure(levelEntity);

                    var reference = new Map.BlobReference();
                    reference.Path = string.Format("{0},{1}", entity.Template.BloName, entity.Template.CfsName);
                    output.WriteStructure(reference);
                }
            }
        }

        private static List<TemplateEntity> LoadEntities()
        {
            var entities = new List<TemplateEntity>();

            string inputPath;
            inputPath = GetExecutablePath();
            inputPath = Path.Combine(inputPath, "space_entities.xml");

            if (File.Exists(inputPath) == false)
            {
                return entities;
            }

            using (var input = File.OpenRead(inputPath))
            {
                var doc = new XPathDocument(input);

                var nav = doc.CreateNavigator();

                var nodes = nav.Select("/entities/entity");
                while (nodes.MoveNext() == true)
                {
                    var current = nodes.Current;

                    var category = current.GetAttribute("category", "");
                    var x = current.GetAttribute("x", "");
                    var y = current.GetAttribute("x", "");
                    var width = current.GetAttribute("width", "");
                    var height = current.GetAttribute("height", "");
                    var blo = current.GetAttribute("blo", "");
                    var cfs = current.GetAttribute("cfs", "");

                    var entity = new TemplateEntity(
                        string.IsNullOrWhiteSpace(x) ? 0 : int.Parse(x),
                        string.IsNullOrWhiteSpace(y) ? 0 : int.Parse(y),
                        int.Parse(width),
                        int.Parse(height),
                        category,
                        blo,
                        cfs);

                    var physics = current.SelectSingleNode("physics");
                    if (physics != null)
                    {
                        entity.SetupPhysics(physics.Value);
                    }

                    var vision = current.SelectSingleNode("vision");
                    if (vision != null)
                    {
                        entity.SetupVision(vision.Value);
                    }

                    entities.Add(entity);
                }
            }

            return entities;
        }
    }
}
