using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IVPlugin.Json
{
    internal class Vector2Converter : JsonConverter<Vector2>
    {
        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString() ?? throw new Exception("Cannot convert null to Vector2");
            var parts = str.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                throw new FormatException();

            Vector2 v = default;
            v.X = float.Parse(parts[0], CultureInfo.InvariantCulture);
            v.Y = float.Parse(parts[1], CultureInfo.InvariantCulture);
            return v;
        }

        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            var newString = value.X.ToString(CultureInfo.InvariantCulture) + ", " + value.Y.ToString(CultureInfo.InvariantCulture);
            writer.WriteStringValue(newString);
        }
    }

    internal class Vector3Converter : JsonConverter<Vector3>
    {
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString() ?? throw new Exception("Cannot convert null to Vector3");
            var parts = str.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 3)
                throw new FormatException();

            Vector3 v = default;
            v.X = float.Parse(parts[0], CultureInfo.InvariantCulture);
            v.Y = float.Parse(parts[1], CultureInfo.InvariantCulture);
            v.Z = float.Parse(parts[2], CultureInfo.InvariantCulture);
            return v;
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
        {
            var newString = value.X.ToString(CultureInfo.InvariantCulture) + ", " + value.Y.ToString(CultureInfo.InvariantCulture) + ", " + value.Z.ToString(CultureInfo.InvariantCulture);
            writer.WriteStringValue(newString);
        }
    }

    internal class Vector4Converter : JsonConverter<Vector4>
    {
        public override Vector4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString() ?? throw new Exception("Cannot convert null to Vector4");
            var parts = str.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 4)
                throw new FormatException();

            Vector4 v = default;
            v.X = float.Parse(parts[0], CultureInfo.InvariantCulture);
            v.Y = float.Parse(parts[1], CultureInfo.InvariantCulture);
            v.Z = float.Parse(parts[2], CultureInfo.InvariantCulture);
            v.W = float.Parse(parts[3], CultureInfo.InvariantCulture);
            return v;
        }

        public override void Write(Utf8JsonWriter writer, Vector4 value, JsonSerializerOptions options)
        {
            var newString = value.X.ToString(CultureInfo.InvariantCulture) + ", " + value.Y.ToString(CultureInfo.InvariantCulture) + ", " + value.Z.ToString(CultureInfo.InvariantCulture) + ", " + value.W.ToString(CultureInfo.InvariantCulture);
            writer.WriteStringValue(newString);
        }
    }

    internal class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? str = reader.GetString() ?? throw new Exception("Cannot convert null to Vector3");
            string[] parts = str.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 4)
                throw new FormatException();

            Quaternion q = default;
            q.X = float.Parse(parts[0], CultureInfo.InvariantCulture);
            q.Y = float.Parse(parts[1], CultureInfo.InvariantCulture);
            q.Z = float.Parse(parts[2], CultureInfo.InvariantCulture);
            q.W = float.Parse(parts[3], CultureInfo.InvariantCulture);
            return q;
        }

        public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options)
        {
            var newString = value.X.ToString(CultureInfo.InvariantCulture) + ", " + value.Y.ToString(CultureInfo.InvariantCulture) + ", " + value.Z.ToString(CultureInfo.InvariantCulture) + ", " + value.W.ToString(CultureInfo.InvariantCulture);
            writer.WriteStringValue(newString);
        }
    }
}
