using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Rain.Core.Utility;

using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Rain.Core.Model;

namespace Rain.Service
{
    public abstract class Settings
    {
        private readonly IDictionary<string, object> _cache = new Dictionary<string, object>();

        public virtual object this[string path] => Get(path);

        protected abstract Stream GetReadStream();

        protected abstract Stream GetWriteStream();

        public bool Contains(string path) { return _cache.ContainsKey(path); }

        public Color GetColor(string path, Color @default = new Color())
        {
            return Contains(path) ? Color.Parse(GetString(path, "none")) : @default;
        }

        public T GetEnum<T>(string path, T @default = default) where T : struct
        {
            return Enum.TryParse(GetString(path)?.Dedash(), out T e) ? e : @default;
        }

        public float GetFloat(string path, float @default = default)
        {
            var x = Get(path, @default);

            return x is float f ? f : Convert.ToSingle(x);
        }

        public int GetInt(string path, int @default = default)
        {
            var x = Get(path, @default);

            return x is int i ? i : Convert.ToInt32(x);
        }

        public string GetString(string path, string @default = default)
        {
            return Get(path, @default) as string;
        }

        public void Load()
        {
            using (var file = GetReadStream())
            {
                var reader = new JsonTextReader(new StreamReader(file));

                Deserialize(JObject.Load(reader));
            }
        }

        public async Task LoadAsync()
        {
            using (var file = GetReadStream())
            {
                var reader = new JsonTextReader(new StreamReader(file));

                Deserialize(await JObject.LoadAsync(reader));
            }
        }

        public void Save()
        {
            using (var file = GetWriteStream())
            {
                var writer = new JsonTextWriter(new StreamWriter(file));

                var obj = Serialize();

                obj.WriteTo(writer);

                writer.Flush();
            }
        }

        public async Task SaveAsync()
        {
            using (var file = GetWriteStream())
            {
                var writer = new JsonTextWriter(new StreamWriter(file));

                var obj = Serialize();

                await obj.WriteToAsync(writer);

                await writer.FlushAsync();
            }
        }

        private void Deserialize(JToken tok, string path = "")
        {
            switch (tok)
            {
                case JObject obj:
                    foreach (var property in obj.Properties())
                        Deserialize(property.Value,
                                    path.Length == 0 ? property.Name : path + "." + property.Name);

                    break;
                case JArray arr:
                    for (var i = 0; i < arr.Count; i++)
                        Deserialize(arr[i], path + $"[{i}]");

                    _cache[path + ".$count"] = arr.Count;

                    break;
                default:
                    _cache[path] = tok.ToObject<dynamic>();

                    break;
            }
        }

        private object Get(string path, object @default = default)
        {
            if (!Contains(path)) return @default;

            var data = _cache[path];

            while (data is string str &&
                   str.StartsWith("@"))
            {
                path = str.Substring(1);
                data = _cache[path];
            }

            return data;
        }

        private JToken Serialize(IDictionary<string, object> set = null)
        {
            if (set == null)
                set = _cache.OrderBy(k => k.Key).ToDictionary();

            if (set.ContainsKey("$count"))
            {
                // array!
                var count = (int) set["$count"];
                var arr = new JArray();

                for (var i = 0; i < count; i++)
                {
                    var indice = $"[{i}]";
                    var newSet = set.Where(k => k.Key.StartsWith(indice))
                                    .Select(k => (k.Key.Substring(indice.Length), k.Value))
                                    .ToDictionary();

                    arr.Add(Serialize(newSet));
                }

                return arr;
            }

            if (set.ContainsKey(""))
                return new JValue(set[""]);

            var obj = new JObject();

            var props = set.Select(k => k.Key.Split('.', '[')[1]).Distinct();

            foreach (var prop in props)
            {
                var newSet = from k in set
                             where k.Key.StartsWith(prop)
                             select (k.Key.Substring(prop.Length + 1), k.Value);

                obj[prop] = Serialize(newSet.ToDictionary());
            }

            return obj;
        }
    }
}