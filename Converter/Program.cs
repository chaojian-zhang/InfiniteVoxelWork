using System;
using System.IO;
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
                foreach (var node in region.Nodes)
                {
                    writer.Write((byte)random.Next(1, 255));
                }

                // Color Palette Buffer
                for (int i = 0; i < 256; i++)
                {
                    string hexString = $"{i:X2}{i:X2}{i:X2}FF"; // RGBA
                    int dec = Int32.Parse(hexString, System.Globalization.NumberStyles.HexNumber);
                    writer.Write((int)dec);
                }

                // Dispose
                writer.Flush();
                writer.Close();
            }
            Console.WriteLine("Finished.");

            // Return success
            return 0;
        }
    }
}
