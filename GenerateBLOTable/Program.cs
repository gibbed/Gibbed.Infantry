/* Copyright (c) 2012 Rick (rick 'at' gibbed 'dot' us)
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
using System.Security.Cryptography;
using System.Xml;
using System.Xml.XPath;
using Gibbed.Infantry.FileFormats;
using NDesk.Options;

namespace GenerateBLOTable
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

        public class ResourceLocation
        {
            public string FileName;
            public string Id;
        }

        public static void Main(string[] args)
        {
            bool showHelp = false;

            var options = new OptionSet()
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
                Console.WriteLine("Usage: {0} [OPTIONS]+ [input_directory]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            var directoryPath = extras.Count == 0
                                    ? Directory.GetCurrentDirectory()
                                    : extras[0];

            var md5 = MD5.Create();
            var resources = new SortedDictionary<string, List<ResourceLocation>>();

            foreach (var inputPath in Directory.GetFiles(directoryPath, "*.blo"))
            {
                var fileName = Path.GetFileName(inputPath);
                if (fileName == null)
                {
                    continue;
                }

                if (fileName.StartsWith("o_") == false &&
                    fileName.StartsWith("f_") == false)
                {
                    continue;
                }

                if (fileName.EndsWith(".lvb.blo") == true)
                {
                    continue;
                }

                Console.WriteLine("Processing '{0}'...", Path.GetFileName(inputPath));

                using (var input = File.OpenRead(inputPath))
                {
                    var blob = new BlobFile();
                    blob.Deserialize(input);

                    foreach (var entry in blob.Entries)
                    {
                        Console.WriteLine("  Hashing '{0}'", entry.Name);

                        input.Seek(entry.Offset, SeekOrigin.Begin);

                        var data = new byte[entry.Size];
                        if (input.Read(data, 0, data.Length) != data.Length)
                        {
                            throw new FormatException();
                        }

                        var hash = md5.ComputeHash(data);
                        var friendlyHash = BitConverter
                            .ToString(hash)
                            .Replace("-", "")
                            .ToLowerInvariant();

                        if (resources.ContainsKey(friendlyHash) == false)
                        {
                            resources[friendlyHash] = new List<ResourceLocation>();
                        }

                        resources[friendlyHash].Add(
                            new ResourceLocation()
                            {
                                FileName = fileName.ToLowerInvariant(),
                                Id = entry.Name.ToLowerInvariant(),
                            });
                    }
                }
            }

            var custom = LoadBloTableCustom();

            // ReSharper disable JoinDeclarationAndInitializer
            string outputPath;
            // ReSharper restore JoinDeclarationAndInitializer

            outputPath = Path.GetDirectoryName(GetExecutablePath());
            if (outputPath == null)
            {
                throw new InvalidOperationException();
            }

            outputPath = Path.Combine(outputPath, "blotable.xml");

            var settings = new XmlWriterSettings()
            {
                Indent = true,
            };

            using (var xml = XmlWriter.Create(outputPath, settings))
            {
                xml.WriteStartDocument();
                xml.WriteStartElement("resources");

                xml.WriteStartElement("auto");
                foreach (var resource in resources)
                {
                    xml.WriteStartElement("resource");
                    xml.WriteAttributeString("hash", resource.Key);

                    foreach (var location in resource.Value)
                    {
                        xml.WriteStartElement("source");
                        xml.WriteAttributeString("filename", location.FileName);
                        xml.WriteAttributeString("id", location.Id);
                        xml.WriteEndElement();
                    }

                    xml.WriteEndElement();
                }
                xml.WriteEndElement();

                if (custom.Count > 0)
                {
                    xml.WriteStartElement("custom");
                    foreach (var resource in custom)
                    {
                        xml.WriteStartElement("resource");
                        xml.WriteAttributeString("hash", resource.Key);

                        foreach (var location in resource.Value)
                        {
                            xml.WriteStartElement("source");
                            xml.WriteAttributeString("filename", location.FileName);
                            xml.WriteAttributeString("id", location.Id);
                            xml.WriteEndElement();
                        }

                        xml.WriteEndElement();
                    }
                    xml.WriteEndElement();
                }

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }
        }

        private static SortedDictionary<string, List<ResourceLocation>> LoadBloTableCustom()
        {
            var resources = new SortedDictionary<string, List<ResourceLocation>>();

            // ReSharper disable JoinDeclarationAndInitializer
            string inputPath;
            // ReSharper restore JoinDeclarationAndInitializer

            inputPath = Path.GetDirectoryName(GetExecutablePath());
            if (inputPath == null)
            {
                throw new InvalidOperationException();
            }

            inputPath = Path.Combine(inputPath, "blotable.xml");

            if (File.Exists(inputPath) == false)
            {
                return resources;
            }

            using (var input = File.OpenRead(inputPath))
            {
                var doc = new XPathDocument(input);

                var nav = doc.CreateNavigator();

                var nodes = nav.Select("/resources/custom/resource");
                while (nodes.MoveNext() == true)
                {
                    var node = nodes.Current;
                    if (node == null)
                    {
                        throw new InvalidOperationException();
                    }

                    var hash = node.GetAttribute("hash", "");
                    if (resources.ContainsKey(hash) == true)
                    {
                        continue;
                    }

                    var source = node.SelectSingleNode("source");
                    if (source == null)
                    {
                        continue;
                    }

                    var filename = source.GetAttribute("filename", "");
                    var id = source.GetAttribute("id", "");

                    if (resources.ContainsKey(hash) == false)
                    {
                        resources[hash] = new List<ResourceLocation>();
                    }

                    resources[hash].Add(new ResourceLocation()
                    {
                        FileName = filename,
                        Id = id,
                    });
                }
            }

            return resources;
        }
    }
}
