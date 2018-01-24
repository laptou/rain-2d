using System;
using System.Collections.Generic;
using System.IO;

using Ibinimator.Core.Utility;

using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ibinimator.Service
{
    public static class Settings
    {
        private static readonly IDictionary<string, object>
            Cache = new Dictionary<string, object>();

        public static bool Contains(string path) { return Cache.ContainsKey(path); }

        public static T GetEnum<T>(string path) where T : struct
        {
            return Enum.TryParse(GetString(path).Dedash(), out T e) ? e : default;
        }

        public static float GetFloat(string path)
        {
            return Cache[path] is float f ? f : Convert.ToSingle(Cache[path]);
        }

        public static int GetInt(string path)
        {
            return Cache[path] is int i ? i : Convert.ToInt32(Cache[path]);
        }

        public static dynamic GetObject(string path) { return Cache[path]; }

        public static string GetString(string path) { return Cache[path] as string; }

        public static string GetString(string path, string @default)
        {
            return Contains(path) ? GetString(path) : @default;
        }

        public static void Load()
        {
            Cache.Clear();

            if (!App.IsDesigner)
            {
                var filePath = AppDomain.CurrentDomain.BaseDirectory + "settings.json";

                if (!File.Exists(filePath))
                    using (var file = File.Open(filePath, FileMode.Create, FileAccess.Write,
                                                FileShare.None))
                    {
                        var defaultFile = Application
                                         .GetResourceStream(
                                              new Uri(
                                                  "/Ibinimator;component" +
                                                  "/settings.default.json", UriKind.Relative))
                                        ?.Stream;

                        if (defaultFile == null)
                            throw new Exception("Default settings file is missing.");

                        defaultFile.CopyTo(file);
                        file.Flush(true);
                        defaultFile.Dispose();
                    }

                using (var file =
                    File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var reader = new JsonTextReader(new StreamReader(file));

                    var obj = JObject.Load(reader);

                    Load(obj);
                }
            }
            else
            {
                using (var file = Application
                                 .GetResourceStream(
                                      new Uri("/Ibinimator;component" + "/settings.default.json",
                                              UriKind.Relative))
                                ?.Stream)
                {
                    if (file == null)
                        throw new Exception("Default settings file is missing.");

                    var reader = new JsonTextReader(new StreamReader(file));

                    var obj = JObject.Load(reader);

                    Load(obj);
                }
            }
        }

        public static async Task SaveAsync()
        {
            using (var file = File.Open(AppDomain.CurrentDomain.BaseDirectory + "settings.json",
                                        FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                var writer = new JsonTextWriter(new StreamWriter(file));

                var obj = await Serialize();

                await obj.WriteToAsync(writer);

                await writer.FlushAsync();
            }
        }

        public static void Set<T>(string path, T value) { Cache[path] = value; }

        private static void Load(JToken tok, string path = "")
        {
            switch (tok)
            {
                case JObject obj:
                    foreach (var property in obj.Properties())
                        Load(property.Value,
                             path.Length == 0 ? property.Name : path + "." + property.Name);

                    break;
                case JArray arr:
                    for (var i = 0; i < arr.Count; i++)
                        Load(arr[i], path + $"[{i}]");

                    Cache[path + ".$count"] = arr.Count;

                    break;
                default:
                    Cache[path] = tok.ToObject<dynamic>();

                    break;
            }
        }

        private static async Task<JToken> Serialize(IDictionary<string, object> set = null)
        {
            if (set == null)
                set = Cache.OrderBy(k => k.Key).ToDictionary();

            if (set.ContainsKey(".$count"))
            {
                // array!
                var count = (int) set[".$count"];
                var arr = new JArray();

                for (var i = 0; i < count; i++)
                {
                    var indice = $"[{i}]";
                    var newSet = set.Where(k => k.Key.StartsWith(indice))
                                    .Select(k => (k.Key.Substring(indice.Length), k.Value))
                                    .ToDictionary();

                    arr.Add(await Serialize(newSet));
                }

                return arr;
            }

            if (set.ContainsKey(""))
                return new JValue(set[""]);

            var obj = new JObject();

            var props = set.Select(k => "." + k.Key.Split('.', '[')[1]).Distinct();

            foreach (var prop in props)
            {
                var newSet = from k in set
                             where k.Key.StartsWith(prop)
                             select (k.Key.Substring(prop.Length), k.Value);

                obj[prop.Substring(1)] = await Serialize(newSet.ToDictionary());
            }

            return obj;
        }
    }
}