using System;
using System.IO;
using System.Web.Script.Serialization;

namespace CountdownApp
{
    public class UiSettings
    {
        public bool DarkTheme { get; set; }

        public UiSettings()
        {
            DarkTheme = true;
        }
    }

    public class UiSettingsStore
    {
        private readonly JavaScriptSerializer _serializer;
        private readonly string _path;

        public UiSettingsStore()
        {
            _serializer = new JavaScriptSerializer();
            _path = ResolvePath();
        }

        public UiSettings Load()
        {
            if (!File.Exists(_path)) return new UiSettings();

            try
            {
                string json = File.ReadAllText(_path);
                UiSettings settings = _serializer.Deserialize<UiSettings>(json);
                return settings == null ? new UiSettings() : settings;
            }
            catch
            {
                return new UiSettings();
            }
        }

        public void Save(UiSettings settings)
        {
            if (settings == null) throw new ArgumentNullException("settings");

            string directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            string temp = _path + ".tmp";
            File.WriteAllText(temp, _serializer.Serialize(settings));
            if (File.Exists(_path)) File.Delete(_path);
            File.Move(temp, _path);
        }

        private static string ResolvePath()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string preferred = Path.Combine(baseDirectory, "data", "ui.json");

            try
            {
                string directory = Path.GetDirectoryName(preferred);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                    string probe = Path.Combine(directory, ".ui-write-probe");
                    File.WriteAllText(probe, "ok");
                    File.Delete(probe);
                }
                return preferred;
            }
            catch
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CountdownApp",
                    "data",
                    "ui.json");
            }
        }
    }
}
