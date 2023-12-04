using Newtonsoft.Json;
using System;
using System.Linq;

namespace KaguyaPC88File
{
    public class SectorHead
    {
        [JsonIgnore]
        public const int HEADER_SIZE = 0x10;

        [JsonConverter(typeof(HexStringToByteJsonConverter))]
        public byte Cmd { get; set; }
        [JsonConverter(typeof(HexStringToByteJsonConverter))]
        public byte C { get; set; }
        [JsonConverter(typeof(HexStringToByteJsonConverter))]
        public byte H { get; set; }
        [JsonConverter(typeof(HexStringToByteJsonConverter))]
        public byte R { get; set; }
        [JsonConverter(typeof(HexStringToByteJsonConverter))]
        public byte N { get; set; }
        [JsonConverter(typeof(HexStringToByteJsonConverter))]
        public byte SectorCount { get; set; }

        public SectorHead(byte[] b)
        {
            if (b.Length != HEADER_SIZE)
            {
                throw new Exception($"Header length invalid: {HEADER_SIZE}");
            }

            if (!b.Skip(5).Take(0xA).ToArray().All(o => o == 0x0))
            {
                throw new Exception("Not all zero.");
            }

            Cmd = b[0];
            C = b[1];
            H = b[2];
            R = b[3];
            N = b[4];

            SectorCount = b[0xF];
        }

        public SectorHead()
        {

        }

        public byte[] GetBytes()
        {
            return new byte[0x10]
            {
                Cmd,
                C,
                H,
                R,
                N,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                SectorCount,
            };
        }
    }

    public sealed class HexStringToUshortJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(ushort).Equals(objectType);
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => writer.WriteValue(MyMath.DecToHex((ushort)value, Prefix.X));
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => (ushort)MyMath.HexToDec((string)reader.Value);
    }

    public sealed class HexStringToByteJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(byte).Equals(objectType);
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => writer.WriteValue(MyMath.DecToHex((byte)value, Prefix.X));
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => (byte)MyMath.HexToDec((string)reader.Value);
    }
}
