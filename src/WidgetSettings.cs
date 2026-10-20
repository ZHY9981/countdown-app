using System;
using System.IO;
using System.Web.Script.Serialization;

namespace CountdownApp
{
    public class WidgetSettings
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool HasPosition { get; set; }
        public bool Visible { get; set; }

        public WidgetSettings()
        {
            X = 0;
            Y = 0;
            HasPosition = false;
            Visible = true;
        }
    }

    public class WidgetSettingsStore
    {
        private readonly JavaScriptSerializer _serializer;
        private readonly string _path;

        public WidgetSettingsStore()
        {
            _serializer = new JavaScriptSerializer();
            _path = ResolvePath();
        }

        public WidgetSettings Load()
        {
            if (!File.Exists(_path)) return new WidgetSettings();

            try
            {
                string json = File.ReadAllText(_path);
                WidgetSettings settings =
                    _serializer.Deserialize<WidgetSettings>(json);
                return settings == null ? new WidgetSettings() : settings;
            }
            catch
            {
                return new WidgetSettings();
            }
        }

        public void Save(WidgetSettings settings)
        {
            if (settings == null) throw new ArgumentNullException("settings");

            string directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            string temp = _path + ".tmp";
            File.WriteAllText(temp, _serializer.Serialize(settings));
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
            File.Move(temp, _path);
        }

        private static string ResolvePath()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string preferred = Path.Combine(baseDirectory, "data", "widget.json");

            try
            {
                string directory = Path.GetDirectoryName(preferred);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                    string probe = Path.Combine(directory, ".widget-write-probe");
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
                    "widget.json");
            }
        }
    }
}
