using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace UyiCore.Save
{
    /// <summary>
    /// JSON file-based save system với slot pattern.
    /// File layout: {persistentDataPath}/{Subdirectory}/{SlotFilePrefix}{slot}{Extension}
    /// Settings và auto-save có file riêng (không theo slot).
    ///
    /// Pattern dùng:
    ///   [Serializable] class MyData { public int level; public int coins; }
    ///   SaveSystem.Save(0, myData);
    ///   var d = SaveSystem.Load&lt;MyData&gt;(0);
    /// </summary>
    public static class SaveSystem
    {
        private static SaveOptions _opts = SaveOptions.Default;
        private static bool _configured;

        public static SaveOptions Options => _opts;
        public static string RootPath => GetRootPath();

        /// <summary>Gọi 1 lần ở Bootstrap (tuỳ chọn). Không gọi → dùng default.</summary>
        public static void Configure(SaveOptions options)
        {
            if (string.IsNullOrEmpty(options.FileExtension)) options.FileExtension = ".json";
            if (options.SlotFilePrefix == null) options.SlotFilePrefix = "save_";
            if (string.IsNullOrEmpty(options.SettingsFileName)) options.SettingsFileName = "settings";
            if (string.IsNullOrEmpty(options.AutoSaveFileName)) options.AutoSaveFileName = "save_auto";
            _opts = options;
            _configured = true;
        }

        // ----- Slot API -----

        public static bool Save<T>(int slot, T data, string label = null) where T : class
        {
            return WriteEnvelope(GetSlotPath(slot), data, label);
        }

        public static T Load<T>(int slot) where T : class
        {
            return ReadData<T>(GetSlotPath(slot));
        }

        public static bool Exists(int slot) => File.Exists(GetSlotPath(slot));

        public static bool Delete(int slot)
        {
            return DeleteFile(GetSlotPath(slot));
        }

        public static SaveMeta? GetMeta(int slot)
        {
            var meta = ReadMeta(GetSlotPath(slot));
            if (meta == null) return null;
            var m = meta.Value;
            m.slot = slot;
            return m;
        }

        public static List<SaveMeta> ListSlots()
        {
            var result = new List<SaveMeta>();
            var dir = GetRootPath();
            if (!Directory.Exists(dir)) return result;

            string prefix = _opts.SlotFilePrefix;
            string ext = _opts.FileExtension;
            string autoName = _opts.AutoSaveFileName + ext;

            foreach (var path in Directory.GetFiles(dir, prefix + "*" + ext))
            {
                var name = Path.GetFileName(path);
                if (name == autoName) continue;

                var slotStr = Path.GetFileNameWithoutExtension(name).Substring(prefix.Length);
                if (!int.TryParse(slotStr, out int slot)) continue;

                var meta = ReadMeta(path);
                if (meta == null) continue;
                var m = meta.Value;
                m.slot = slot;
                result.Add(m);
            }
            result.Sort((a, b) => a.slot.CompareTo(b.slot));
            return result;
        }

        public static bool Copy(int fromSlot, int toSlot)
        {
            var src = GetSlotPath(fromSlot);
            if (!File.Exists(src)) return false;
            try
            {
                EnsureDir();
                File.Copy(src, GetSlotPath(toSlot), overwrite: true);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Copy slot {fromSlot}→{toSlot} fail: {e.Message}");
                return false;
            }
        }

        // ----- Auto-save -----

        public static bool SaveAuto<T>(T data, string label = null) where T : class
        {
            return WriteEnvelope(GetAutoPath(), data, label);
        }

        public static T LoadAuto<T>() where T : class
        {
            return ReadData<T>(GetAutoPath());
        }

        public static bool ExistsAuto() => File.Exists(GetAutoPath());
        public static bool DeleteAuto() => DeleteFile(GetAutoPath());

        // ----- Settings (không theo slot) -----

        public static bool SaveSettings<T>(T data) where T : class
        {
            return WriteEnvelope(GetSettingsPath(), data, null);
        }

        public static T LoadSettings<T>() where T : class
        {
            return ReadData<T>(GetSettingsPath());
        }

        public static bool ExistsSettings() => File.Exists(GetSettingsPath());

        // ----- Bulk -----

        public static int DeleteAll()
        {
            var dir = GetRootPath();
            if (!Directory.Exists(dir)) return 0;
            int count = 0;
            foreach (var path in Directory.GetFiles(dir, "*" + _opts.FileExtension))
            {
                try { File.Delete(path); count++; }
                catch (Exception e) { Debug.LogWarning($"[SaveSystem] Delete '{path}' fail: {e.Message}"); }
            }
            return count;
        }

        // ----- Internals -----

        static bool WriteEnvelope<T>(string path, T data, string label) where T : class
        {
            if (data == null)
            {
                Debug.LogError("[SaveSystem] data null.");
                return false;
            }

            var env = new SaveEnvelope<T>
            {
                version = 1,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                label = label,
                data = data,
            };

            try
            {
                EnsureDir();
                string json = JsonUtility.ToJson(env);
                if (_opts.Obfuscate) json = Obfuscate(json, _opts.ObfuscationKey);
                File.WriteAllText(path, json, Encoding.UTF8);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Write '{path}' fail: {e.Message}");
                return false;
            }
        }

        static T ReadData<T>(string path) where T : class
        {
            if (!File.Exists(path)) return null;
            try
            {
                string text = File.ReadAllText(path, Encoding.UTF8);
                if (_opts.Obfuscate) text = Deobfuscate(text, _opts.ObfuscationKey);
                var env = JsonUtility.FromJson<SaveEnvelope<T>>(text);
                return env != null ? env.data : null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Read '{path}' fail: {e.Message}");
                return null;
            }
        }

        static SaveMeta? ReadMeta(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                string text = File.ReadAllText(path, Encoding.UTF8);
                if (_opts.Obfuscate) text = Deobfuscate(text, _opts.ObfuscationKey);
                // Parse vào envelope rỗng (kiểu data = object) — chỉ lấy version/timestamp/label
                var env = JsonUtility.FromJson<SaveEnvelope<MetaProbe>>(text);
                if (env == null) return null;
                return new SaveMeta
                {
                    version = env.version,
                    timestamp = env.timestamp,
                    label = env.label,
                };
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] ReadMeta '{path}' fail: {e.Message}");
                return null;
            }
        }

        [Serializable] private class MetaProbe { } // empty — JsonUtility skip unknown fields

        static bool DeleteFile(string path)
        {
            if (!File.Exists(path)) return false;
            try { File.Delete(path); return true; }
            catch (Exception e) { Debug.LogError($"[SaveSystem] Delete '{path}' fail: {e.Message}"); return false; }
        }

        static void EnsureDir()
        {
            var dir = GetRootPath();
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        static string GetRootPath()
        {
            return string.IsNullOrEmpty(_opts.Subdirectory)
                ? Application.persistentDataPath
                : Path.Combine(Application.persistentDataPath, _opts.Subdirectory);
        }

        static string GetSlotPath(int slot)
        {
            return Path.Combine(GetRootPath(), _opts.SlotFilePrefix + slot + _opts.FileExtension);
        }

        static string GetAutoPath()
        {
            return Path.Combine(GetRootPath(), _opts.AutoSaveFileName + _opts.FileExtension);
        }

        static string GetSettingsPath()
        {
            return Path.Combine(GetRootPath(), _opts.SettingsFileName + _opts.FileExtension);
        }

        // ----- Obfuscation: XOR + Base64. Deter casual tampering, KHÔNG phải mã hoá. -----

        static string Obfuscate(string s, string key)
        {
            if (string.IsNullOrEmpty(key)) return s;
            var data = Encoding.UTF8.GetBytes(s);
            var k = Encoding.UTF8.GetBytes(key);
            for (int i = 0; i < data.Length; i++) data[i] ^= k[i % k.Length];
            return Convert.ToBase64String(data);
        }

        static string Deobfuscate(string s, string key)
        {
            if (string.IsNullOrEmpty(key)) return s;
            var data = Convert.FromBase64String(s);
            var k = Encoding.UTF8.GetBytes(key);
            for (int i = 0; i < data.Length; i++) data[i] ^= k[i % k.Length];
            return Encoding.UTF8.GetString(data);
        }
    }
}
