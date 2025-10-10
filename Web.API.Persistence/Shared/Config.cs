using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Persistence.Shared
{
    public sealed class Config
    {
        private static readonly Lazy<Config> lazy = new(() => new Config());
        public static Config Instance => lazy.Value;

        private string _path;
        private readonly string _exe = Assembly.GetExecutingAssembly().GetName().Name!;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        private Config(string? iniPath = null)
        {
            _path = iniPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.ini");
        }

        // Kalau mau override lokasi file INI (mis. dari environment)
        public void SetPath(string iniPath)
        {
            if (string.IsNullOrWhiteSpace(iniPath)) throw new ArgumentNullException(nameof(iniPath));
            _path = iniPath;
        }

        public string? Read(string key, string? section = null, string? defaultValue = null)
        {
            var ret = new StringBuilder(1024);
            GetPrivateProfileString(section ?? _exe, key, defaultValue ?? "", ret, ret.Capacity, _path);
            var s = ret.ToString();
            return string.IsNullOrEmpty(s) ? defaultValue : s;
        }

        public void Write(string key, string? value, string? section = null)
            => WritePrivateProfileString(section ?? _exe, key, value, _path);

        public void DeleteKey(string key, string? section = null)
            => Write(key, null, section ?? _exe);

        public void DeleteSection(string? section = null)
            => Write(null, null, section ?? _exe);

        public bool KeyExists(string key, string? section = null)
            => (Read(key, section)?.Length ?? 0) > 0;

        // ---------- helpers bertipe ----------
        public int ReadInt(string key, string? section = null, int defaultValue = 0)
            => int.TryParse(Read(key, section), out var v) ? v : defaultValue;

        public long ReadLong(string key, string? section = null, long defaultValue = 0L)
            => long.TryParse(Read(key, section), out var v) ? v : defaultValue;

        public bool ReadBool(string key, string? section = null, bool defaultValue = false)
        {
            var s = Read(key, section);
            if (string.IsNullOrWhiteSpace(s)) return defaultValue;
            if (bool.TryParse(s, out var b)) return b;
            // dukung 0/1, yes/no
            return s switch
            {
                "1" => true,
                "0" => false,
                _ when s.Equals("yes", StringComparison.OrdinalIgnoreCase) => true,
                _ when s.Equals("no", StringComparison.OrdinalIgnoreCase) => false,
                _ => defaultValue
            };
        }

        public TimeSpan ReadTimeSpan(string key, string? section = null, TimeSpan? defaultValue = null)
        {
            var s = Read(key, section);
            return TimeSpan.TryParse(s, out var ts) ? ts : (defaultValue ?? TimeSpan.Zero);
        }
    }
}
