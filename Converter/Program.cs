using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using YamlDotNet.Serialization;

namespace Converter
{
    internal class Region
    {
        public int SizeX { get; set; }
        public int SizeY { get; set; }
        public int SizeZ { get; set; }
        public Node[] Nodes { get; set; }
    }
    internal class Node
    {
        public int[] Pos { get; set; }
        public string Name { get; set; }
    }

    internal class ColorDefinition
    {
        public string Name { get; set; }
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }

        public ColorDefinition()
        {
        }

        public ColorDefinition(string name, int r, int g, int b)
        {
            Name = name;
            R = r;
            G = g;
            B = b;
        }
    }

    class Program
    {
        static int Main(string[] args)
        {
            // Exception handling
            if (args.Length == 0)
            {
                Console.WriteLine("Not enough arguments.");
                return -1; 
            }

            // Extract parameters
            string sourcePath = args[0];
            string targetPath = args[1];

            // Start extraction
            Region region = new Deserializer().Deserialize<Region>(File.ReadAllText(sourcePath));
            Console.WriteLine($"{region.Nodes.Length} nodes loaded.");

            // Read colors
            Dictionary<string, ColorDefinition> colors = ParseColors(ReadResource("Data.Colors.txt"));

            // Colors preprocessing
            HashSet<string> uniqueNodes = region.Nodes.Select(n => n.Name).Distinct().ToHashSet();
            colors = colors.Where(c => uniqueNodes.Contains(c.Key)).ToDictionary(p => p.Key, p => p.Value);
            List<ColorDefinition> indices = colors.Values.ToList();
            var colorIndex = indices.Select((c, i) => new KeyValuePair<string, int>(c.Name, i))
                .ToDictionary(p => p.Key, p => p.Value);
            // Exception handling
            if(colors.Count() > 255)
            {
                Console.WriteLine("[Error] More than 255 colors are used.");
                return -2;
            }

            // Perform conversion
            using (BinaryWriter writer = new BinaryWriter(File.Open(targetPath, FileMode.Create)))
            {
                // Header
                // Magic Number
                writer.Write(Encoding.ASCII.GetBytes("XRAW"));
                // Color Meta: unsigned RGBA 8-bit 256-color palette
                writer.Write((byte)0);
                writer.Write((byte)4);
                writer.Write((byte)8);
                writer.Write((byte)8);
                // Size: 2x2x1 - 256 Colors
                writer.Write((int)region.SizeX);
                writer.Write((int)region.SizeY);
                writer.Write((int)region.SizeZ);
                writer.Write((int)256);

                // Voxel Buffer
                Random random = new Random();
                foreach (Node node in region.Nodes)
                {
                    if(colorIndex.ContainsKey(node.Name))
                        writer.Write((byte)colorIndex[node.Name]);
                    else
                    {
                        writer.Write((byte)1);
                        Console.WriteLine($"Node type \"{node.Name}\" is not defined in color file; Used `{indices[1].Name}` instead (color index 1).");
                    }
                }

                // Color Palette Buffer
                for (int i = 0; i < 256; i++)
                {
                    // Real colors
                    if (i < indices.Count)
                    {
                        writer.Write((byte)indices[i].R); // R
                        writer.Write((byte)indices[i].G); // G
                        writer.Write((byte)indices[i].B); // B
                        writer.Write((byte)255); // A
                    }
                    // Padding
                    else
                    {
                        writer.Write((byte)i); // R
                        writer.Write((byte)i); // G
                        writer.Write((byte)i); // B
                        writer.Write((byte)255); // A
                    }
                }

                // Dispose
                writer.Flush();
                writer.Close();
            }
            Console.WriteLine("Finished.");

            // Return success
            return 0;
        }

        public static string ReadResource(string name)
        {
            // Determine path
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = name;
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            if (!name.StartsWith(nameof(Converter)))
            {
                resourcePath = assembly.GetManifestResourceNames()
                    .Single(str => str.EndsWith(name));
            }

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static Dictionary<string, ColorDefinition> ParseColors(string text)
        {
            Dictionary<string, ColorDefinition> colors = new Dictionary<string, ColorDefinition>();
            // Parse each line
            StringReader strReader = new StringReader(text);
            while (true)
            {
                string line  = strReader.ReadLine();
                // EOF
                if (line == null) break;
                // Skip comment lines
                if (line.StartsWith('#')) continue;
                // Parse space delimited values
                else
                {
                    // Split
                    var values = line.Split(' ');
                    // Skip ill-formatted lines
                    if(values.Length != 4)
                    {
                        Console.WriteLine($"Color definition line \"{line}\" is ill-formatted; Skip.");
                        continue;
                    }
                        
                    // Extract
                    string name = values[0];
                    int r = int.Parse(values[1]);
                    int g = int.Parse(values[2]);
                    int b = int.Parse(values[3]);
                    // Save
                    colors[values[0]] = new ColorDefinition(name, r, g, b);
                }
            }
            return colors;
        }
    }
}
