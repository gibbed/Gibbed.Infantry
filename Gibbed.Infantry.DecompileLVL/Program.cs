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
using System.Security.Cryptography;
using System.Xml.XPath;
using Gibbed.Helpers;
using Gibbed.Infantry.FileFormats;
using NDesk.Options;
using Map = Gibbed.Infantry.FileFormats.Map;

namespace Gibbed.Infantry.DecompileLVL
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        public static void Main(string[] args)
        {
            bool showHelp = false;
            bool resourceMapping = true;
            bool generateBlobs = false;
            bool stripDumbBlobs = false;

            OptionSet options = new OptionSet()
            {
                {
                    "no-resource-mapping",
                    "don't do resource mapping for older level files", 
                    v => resourceMapping = v == null
                },
                {
                    "g|generate-blobs",
                    "generate blobs for older level files when resource mapping fails", 
                    v => generateBlobs = v != null
                },
                {
                    "s|strip-lvb-blob-references",
                    "strip references to .lvb.blo blob files",
                    v => stripDumbBlobs = v != null
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
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_lvl [output_map]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string inputPath = extras[0];
            string outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, ".map");
            string lvbPath = Path.ChangeExtension(inputPath, ".lvb");
            string lvbName = Path.GetFileName(lvbPath);

            var level = new LevelFile();

            using (var input = File.OpenRead(inputPath))
            {
                level.Deserialize(input);
            }

            if (stripDumbBlobs == true)
            {
                for (int i = 0; i < level.Floors.Count; i++)
                {
                    var floor = level.Floors[i];
                    if (floor.FileName != null &&
                        floor.FileName.EndsWith(".lvb.blo") == true)
                    {
                        floor.FileName = null;
                        level.Floors[i] = floor;
                    }
                }

                for (int i = 0; i < level.Objects.Count; i++)
                {
                    var obj = level.Objects[i];
                    if (obj.FileName != null &&
                        obj.FileName.EndsWith(".lvb.blo") == true)
                    {
                        obj.FileName = null;
                        level.Objects[i] = obj;
                    }
                }
            }

            var hasLevelBlob =
                level.Floors.Any(f => f.FileName == null) == true ||
                level.Objects.Any(o => o.FileName == null) == true;

            var remap =
                new Dictionary<string, string>();

            if (hasLevelBlob == true)
            {
                // TODO: decrap this block of code

                var resources = resourceMapping == true ?
                    LoadBLOTable() :
                    new SortedDictionary<string, string>();

                if (File.Exists(lvbPath) == false)
                {
                    Console.WriteLine("Could not open '{0}'!", lvbPath);
                    Console.WriteLine();
                    Console.WriteLine("Mapping of LVB resources to real locations isn't going to happen.");
                }
                else
                {
                    using (var lvb = File.OpenRead(lvbPath))
                    {
                        var levelBlob = new BlobFile();
                        levelBlob.Deserialize(lvb);

                        var md5 = MD5.Create();

                        var unknownEntries = new List<BlobFile.Entry>();

                        foreach (var entry in levelBlob.Entries)
                        {
                            lvb.Seek(entry.Offset, SeekOrigin.Begin);

                            var data = new byte[entry.Size];
                            if (lvb.Read(data, 0, data.Length) != data.Length)
                            {
                                throw new FormatException();
                            }

                            var hash = md5.ComputeHash(data);
                            var friendlyHash = BitConverter
                                .ToString(hash)
                                .Replace("-", "")
                                .ToLowerInvariant();

                            if (resources.ContainsKey(friendlyHash) == true)
                            {
                                remap[entry.Name] = resources[friendlyHash];
                            }
                            else
                            {
                                Console.WriteLine("Could not find real location for '{0},{1}'.", lvbName, entry.Name);
                                unknownEntries.Add(entry);
                            }
                        }

                        if (unknownEntries.Count > 0)
                        {
                            Console.WriteLine("Could not find all of the level blob resources.");

                            if (generateBlobs == true)
                            {
                                string floorBlobName;
                                floorBlobName = "f_!";
                                floorBlobName += Path.GetFileNameWithoutExtension(inputPath);
                                floorBlobName = Path.ChangeExtension(floorBlobName, ".blo");

                                string floorBlobPath;
                                floorBlobPath = Path.GetDirectoryName(inputPath);
                                floorBlobPath = Path.Combine(floorBlobPath, floorBlobName);

                                Console.WriteLine("Creating '{0}'...", floorBlobPath);

                                using (var output = File.Create(floorBlobPath))
                                {
                                    var floorBlob = new BlobFile();
                                    floorBlob.Version = 2;

                                    // generate fake entries
                                    var floors = unknownEntries.Where(f => f.Name.StartsWith("f"));
                                    foreach (var floor in floors)
                                    {
                                        floorBlob.Entries.Add(new BlobFile.Entry()
                                            {
                                                Name = floor.Name,
                                            });
                                    }
                                    floorBlob.Serialize(output);

                                    // generate real entries
                                    floorBlob.Entries.Clear();
                                    foreach (var floor in floors)
                                    {
                                        Console.WriteLine("  adding '{0}'", floor.Name);

                                        lvb.Seek(floor.Offset, SeekOrigin.Begin);

                                        floorBlob.Entries.Add(new BlobFile.Entry()
                                        {
                                            Name = floor.Name,
                                            Offset = output.Position,
                                            Size = floor.Size,
                                        });

                                        output.WriteFromStream(lvb, floor.Size);

                                        remap[floor.Name] = string.Format("{0},{1}", floorBlobName, floor.Name);
                                    }

                                    output.Seek(0, SeekOrigin.Begin);
                                    floorBlob.Serialize(output);
                                }

                                string objectBlobName;
                                objectBlobName = "o_!";
                                objectBlobName += Path.GetFileNameWithoutExtension(inputPath);
                                objectBlobName = Path.ChangeExtension(objectBlobName, ".blo");

                                string objectBlobPath;
                                objectBlobPath = Path.GetDirectoryName(inputPath);
                                objectBlobPath = Path.Combine(objectBlobPath, objectBlobName);

                                Console.WriteLine("Creating '{0}'...", objectBlobPath);

                                using (var output = File.Create(objectBlobPath))
                                {
                                    var objectBlob = new BlobFile();
                                    objectBlob.Version = 2;

                                    // generate fake entries
                                    var objects = unknownEntries.Where(o => o.Name.StartsWith("o"));
                                    foreach (var obj in objects)
                                    {
                                        objectBlob.Entries.Add(new BlobFile.Entry()
                                        {
                                            Name = obj.Name,
                                        });
                                    }
                                    objectBlob.Serialize(output);

                                    // generate real entries
                                    objectBlob.Entries.Clear();
                                    foreach (var obj in objects)
                                    {
                                        Console.WriteLine("  adding '{0}'", obj.Name);

                                        lvb.Seek(obj.Offset, SeekOrigin.Begin);

                                        objectBlob.Entries.Add(new BlobFile.Entry()
                                        {
                                            Name = obj.Name,
                                            Offset = output.Position,
                                            Size = obj.Size,
                                        });

                                        output.WriteFromStream(lvb, obj.Size);

                                        remap[obj.Name] = string.Format("{0},{1}", objectBlobName, obj.Name);
                                    }

                                    output.Seek(0, SeekOrigin.Begin);
                                    objectBlob.Serialize(output);
                                }
                            }
                        }
                    }
                }
            }

            using (var output = File.Create(outputPath))
            {
                var header = new Map.Header();

                header.Version = 9;
                header.Width = level.Width;
                header.Height = level.Height;
                // not right? DERP
                //header.OffsetX = level.OffsetX;
                //header.OffsetY = level.OffsetY;
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

                        if (floor.FileName == null)
                        {
                            if (remap.ContainsKey(floor.Id) == true)
                            {
                                reference.Path = remap[floor.Id];
                            }
                            else
                            {
                                reference.Path = string.Format("{0},{1}",
                                    lvbName,
                                    floor.Id);
                            }
                        }
                        else
                        {
                            reference.Path = string.Format("{0},{1}",
                                floor.FileName,
                                floor.Id);
                        }
                    }

                    output.WriteStructure(reference);
                }

                var tiles = new byte[level.Width * level.Height * 4];
                int offset = 0;

                for (int i = 0, j = 0; i < level.Tiles.Length; i++, j += 4)
                {
                    tiles[j + 0] = level.Tiles[i].BitsA;
                    tiles[j + 0] &= 0x7F;
                    tiles[j + 1] = 0;
                    tiles[j + 2] = level.Tiles[i].BitsC;
                    tiles[j + 3] = level.Tiles[i].BitsB;
                }

                using (var rle = new MemoryStream())
                {
                    rle.WriteRLE(tiles, 4, level.Tiles.Length, false);
                    rle.Position = 0;

                    output.WriteValueS32((int)rle.Length);
                    output.WriteFromStream(rle, rle.Length);
                }

                for (int i = 0; i < level.Entities.Count; i++)
                {
                    output.WriteStructure(level.Entities[i]);

                    var obj = level.Objects[level.Entities[i].ObjectId];

                    var reference = new Map.BlobReference();

                    if (obj.FileName == null)
                    {
                        if (remap.ContainsKey(obj.Id) == true)
                        {
                            reference.Path = remap[obj.Id];
                        }
                        else
                        {
                            reference.Path = string.Format("{0},{1}",
                                lvbName,
                                obj.Id);
                        }
                    }
                    else
                    {
                        reference.Path = string.Format("{0},{1}",
                            obj.FileName,
                            obj.Id);
                    }

                    output.WriteStructure(reference);
                }
            }
        }

        private static SortedDictionary<string, string> LoadBLOTable()
        {
            var resources = new SortedDictionary<string, string>();

            string inputPath;
            inputPath = Path.GetDirectoryName(GetExecutableName());
            inputPath = Path.Combine(inputPath, "blotable.xml");

            if (File.Exists(inputPath) == false)
            {
                return resources;
            }

            using (var input = File.OpenRead(inputPath))
            {
                var doc = new XPathDocument(input);
                var nav = doc.CreateNavigator();

                var nodes = nav.Select("/resources/*/resource");
                while (nodes.MoveNext() == true)
                {
                    var hash = nodes.Current.GetAttribute("hash", "");

                    if (resources.ContainsKey(hash) == true)
                    {
                        continue;
                    }

                    var source = nodes.Current.SelectSingleNode("source");
                    if (source == null)
                    {
                        continue;
                    }

                    var filename = source.GetAttribute("filename", "");
                    var id = source.GetAttribute("id", "");

                    resources[hash] = string.Format("{0},{1}", filename, id);
                }
            }

            return resources;
        }
    }
}
