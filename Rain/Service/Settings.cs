using System;
using System.Collections;
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
    public abstract class Settings : IEnumerable<KeyValuePair<string, object>>
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

        public string GetString(string path, string @default = default) { return Get(path, @default) as string; }

        public Settings GetSubset(string path) { return Get(path) as Settings; }

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

        public void Set<T>(string path, T value = default)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Setting path is invalid.");

            if (!char.IsLetterOrDigit(path[0]))
                throw new ArgumentException("Setting path is invalid.");

            _cache[path] = value;
        }

        private void Deserialize(JToken tok, string path = "")
        {
            switch (tok)
            {
                case JObject obj:
                    foreach (var property in obj.Properties())
                        Deserialize(property.Value, path.Length == 0 ? property.Name : path + "." + property.Name);

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
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Setting path is invalid.");

            if (!Contains(path))
            {
                var subpaths = (from kv in _cache where kv.Key.StartsWith(path) select kv).ToList();

                if (subpaths.Count > 0)
                {
                    var sub = new DerivedSettings(this);

                    foreach (var kv in subpaths)

                        // is the value a reference?
                        if (kv.Value is string str &&
                            str.StartsWith("@"))
                            sub.Set(kv.Key.Substring(path.Length + 1), Get(kv.Key));
                        else
                            sub.Set(kv.Key.Substring(path.Length + 1), kv.Value);

                    return sub;
                }

                return @default;
            }

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

        #region IEnumerable<KeyValuePair<string,object>> Members

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            var groups = from kv in _cache let path = kv.Key.Split('.')[0] group kv.Key by path;

            foreach (var group in groups)
                yield return new KeyValuePair<string, object>(group.Key, Get(group.Key));
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        #endregion

        #region Nested type: DerivedSettings

        private class DerivedSettings : Settings
        {
            private readonly Settings _parent;

            public DerivedSettings(Settings parent) { _parent = parent; }

            /// <inheritdoc />
            protected override Stream GetReadStream() { return _parent.GetReadStream(); }

            /// <inheritdoc />
            protected override Stream GetWriteStream() { return _parent.GetWriteStream(); }
        }

        #endregion
    }
}