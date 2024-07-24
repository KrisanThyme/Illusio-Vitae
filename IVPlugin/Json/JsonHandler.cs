using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IVPlugin.Json
{
    public static class JsonHandler
    {
        private readonly static JsonSerializerOptions _serializeOptions;

        static JsonHandler()
        {
            _serializeOptions = new();
            _serializeOptions.WriteIndented = true;

            _serializeOptions.Converters.Add(new JsonStringEnumConverter());
            _serializeOptions.Converters.Add(new Vector2Converter());
            _serializeOptions.Converters.Add(new Vector3Converter());
            _serializeOptions.Converters.Add(new Vector4Converter());
            _serializeOptions.Converters.Add(new QuaternionConverter());
        }

        public static T Deserialize<T>(string json)
        {
            var obj = JsonSerializer.Deserialize<T>(json, _serializeOptions);
            if (obj == null)
                throw new Exception($"Failed to deserialize");

            return obj;
        }

        public static object Deserialize(string json, Type type)
        {
            var obj = JsonSerializer.Deserialize(json, type, _serializeOptions);
            if (obj == null)
                throw new Exception($"Failed to deserialize");

            return obj;
        }

        public static string Serialize(object obj)
        {
            return JsonSerializer.Serialize(obj, _serializeOptions);
        }
    }
}
