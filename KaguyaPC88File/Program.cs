using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KaguyaPC88File
{
    public enum WriteArgs
    {
        action,
        inputDiskImagePath,
        outputDiskImagePath,
        inFolder,
    }

    public enum DumpArgs
    {
        action,
        diskImagePath,
        outFolder,
    }

    class Program
    {
        private const int SECTOR_SIZE = 0x100;
        private const string METADATA_FILE = "metadata.json";

        static void Main(string[] args)
        {

            if (args.Length == 0)
            {
                throw new Exception($"Cannot have 0 arguments.");
            }

            var action = args[0];

            switch (action)
            {
                case "Dump":
                    {
                        Console.WriteLine($"Dumping");

                        var requiredLength = (int)Enum.GetValues(typeof(DumpArgs)).Cast<DumpArgs>().Max() + 1;
                        if (args.Length != requiredLength)
                        {
                            throw new Exception($"Required argument number: {requiredLength}. Received: {args.Length}");
                        }

                        // Read arguments
                        var folder = args[(int)DumpArgs.outFolder];
                        var romPath = args[(int)DumpArgs.diskImagePath];

                        if (!Directory.Exists(folder))
                        {
                            throw new Exception($"Folder doesn't exist: {folder}");
                        }

                        if (!File.Exists(romPath))
                        {
                            throw new Exception($"File doesn't exist: {romPath}");
                        }

                        byte[] source = File.ReadAllBytes(romPath);

                        Dictionary<int, List<SectorHead>> jsonOutputMetadata = new();

                        List<uint> addresses = new();
                        for (int i = 0x20; i < 0x160; i += 0x4)
                        {
                            var a = BitConverter.ToUInt32(source.Skip(i).Take(4).ToArray());
                            addresses.Add(a);
                        }

                        var file = Array.Empty<byte>();

                        int ta = 0;
                        foreach (var trackAddress_ in addresses)
                        {
                            var outputFileStartAddress = file.Length;
                            jsonOutputMetadata.Add(outputFileStartAddress, new List<SectorHead>());

                            var trackAddress = trackAddress_;

                            var fileBytes = Array.Empty<byte>();

                            while (true)
                            {
                                var a = new SectorHead(source.Skip((int)trackAddress).Take(SectorHead.HEADER_SIZE).ToArray());

                                jsonOutputMetadata[outputFileStartAddress].Add(a);

                                var totalSize = a.SectorCount * SECTOR_SIZE;

                                fileBytes = fileBytes.Concat(source.Skip((int)trackAddress + SectorHead.HEADER_SIZE).Take(totalSize).ToArray()).ToArray();
                                trackAddress += (uint)(totalSize + SectorHead.HEADER_SIZE);

                                if (ta != addresses.Count - 1)
                                {
                                    if (trackAddress == addresses[ta + 1])
                                    {
                                        break;
                                    }

                                    if (trackAddress > addresses[ta + 1])
                                    {
                                        throw new Exception();
                                    }
                                } 
                                else
                                {
                                    if (trackAddress == source.Length)
                                    {
                                        break;
                                    }
                                }
                            }

                            file = file.Concat(fileBytes).ToArray();
                            ta++;
                        }

                        // Set new working directory
                        if (!Directory.Exists(folder))
                        {
                            throw new Exception($"Path does not exist: {folder}");
                        }

                        Directory.SetCurrentDirectory(folder);

                        File.WriteAllBytes("streamlined.bin", file);

                        // Will be used for writing
                        File.WriteAllText(METADATA_FILE, JsonConvert.SerializeObject(jsonOutputMetadata, Formatting.Indented));

                        break;
                    }
                case "Write":
                    {
                        Console.WriteLine($"Writing");

                        var requiredLength = (int)Enum.GetValues(typeof(WriteArgs)).Cast<WriteArgs>().Max() + 1;
                        if (args.Length != requiredLength)
                        {
                            throw new Exception($"Required argument number: {requiredLength}. Received: {args.Length}");
                        }

                        // Read arguments
                        var folder = args[(int)WriteArgs.inFolder];
                        var iRomPath = args[(int)WriteArgs.inputDiskImagePath];
                        var oRomPath = args[(int)WriteArgs.outputDiskImagePath];

                        Directory.SetCurrentDirectory(folder);

                        var settings = JsonConvert.DeserializeObject<Dictionary<int, List<SectorHead>>>(new StreamReader(METADATA_FILE).ReadToEnd());

                        var iRom = File.ReadAllBytes(iRomPath);

                        var diskImage = iRom.Take(0x20).ToList().Concat(new byte[0x290]).ToArray();

                        List<uint> fileLocations = new();

                        var b = File.ReadAllBytes("streamlined.bin");

                        var fileI = 0;
                        foreach (var h in settings)
                        {
                            fileLocations.Add((uint)diskImage.Length);

                            foreach (var v in h.Value)
                            {
                                var chunkSize = v.SectorCount * SECTOR_SIZE;
                                var chunk = b.Skip(fileI).Take(chunkSize).ToArray();
                                diskImage = diskImage.Concat(v.GetBytes()).Concat(chunk).ToArray();
                                fileI += chunkSize;
                            }
                        }

                        var locationTable = Array.Empty<byte>();
                        foreach(var a in fileLocations)
                        {
                            locationTable =locationTable.Concat(BitConverter.GetBytes(a)).ToArray();
                        }

                        Array.Copy(locationTable, 0, diskImage, 0x20, locationTable.Length);

                        File.WriteAllBytes(oRomPath, diskImage);

                        break;
                    }
                default:
                    throw new Exception($"Invalid first parameter: {action}");
            }
        }
    }
}