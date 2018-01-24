using System;
using System.Collections.Generic;
using System.IO;

using Ibinimator.Core.Utility;

using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Ibinimator.Core.Model;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ibinimator.Service
{
    public class Settings
    {
        private readonly IDictionary<string, object> _cache = new Dictionary<string, object>();

        public static Settings Current { get; private set; }

        public bool Contains(string path) { return _cache.ContainsKey(path); }

        public T GetEnum<T>(string path) where T : struct
        {
            return Enum.TryParse(GetString(path).Dedash(), out T e) ? e : default;
        }

        public float GetFloat(string path)
        {
            return _cache[path] is float f ? f : Convert.ToSingle(_cache[path]);
        }

        public int GetInt(string path)
        {
            return _cache[path] is int i ? i : Convert.ToInt32(_cache[path]);
        }

        public Color GetColor(string path, Color @default = new Color())
        {
            return Contains(path) ? Color. : @default;
        }

        public string GetString(string path, string @default = null)
        {
            return Contains(path) ? _cache[path] as string : @default;
        }

        public static void Load()
        {
            var defaultUri = new Uri("/Ibinimator;component/settings.default.json",
                                     UriKind.Relative);

            if (!App.IsDesigner)
            {
                var filePath = AppDomain.CurrentDomain.BaseDirectory + "settings.json";

                if (!File.Exists(filePath))
                    using (var file = File.Open(filePath, FileMode.Create, FileAccess.Write))
                    {
                        var defaultFile = Application.GetResourceStream(defaultUri)?.Stream;

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

                    Current = new Settings();
                    Current.Load(JObject.Load(reader));
                }
            }
            else
            {
                using (var file = Application.GetResourceStream(defaultUri)?.Stream)
                {
                    if (file == null)
                        throw new Exception("Default settings file is missing.");

                    var reader = new JsonTextReader(new StreamReader(file));

                    Current = new Settings();
                    Current.Load(JObject.Load(reader));
                }
            }
        }

        public async Task SaveAsync()
        {
            using (var file = File.Open(AppDomain.CurrentDomain.BaseDirectory + "settings.json",
                                        FileMode.Create,
                                        FileAccess.Write,
                                        FileShare.Read))
            {
                var writer = new JsonTextWriter(new StreamWriter(file));

                var obj = await Serialize();

                await obj.WriteToAsync(writer);

                await writer.FlushAsync();
            }
        }

        private void Load(JToken tok, string path = "")
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

                    _cache[path + ".$count"] = arr.Count;

                    break;
                default:
                    _cache[path] = tok.ToObject<dynamic>();

                    break;
            }
        }

        private async Task<JToken> Serialize(IDictionary<string, object> set = null)
        {
            if (set == null)
                set = _cache.OrderBy(k => k.Key).ToDictionary();

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