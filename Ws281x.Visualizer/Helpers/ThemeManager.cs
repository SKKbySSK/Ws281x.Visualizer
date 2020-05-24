using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ws281x.Visualizer.Models;

namespace Ws281x.Visualizer.Helpers
{
    public class ThemeManager
    {
        public ThemeManager(string parentDirectory)
        {
            ParentDirectory = parentDirectory;
        }

        public void Load(string searchPattern = "*.json", Action<string, Exception> onFailed = null)
        {
            if (!Directory.Exists(ParentDirectory))
            {
                Directory.CreateDirectory(ParentDirectory);
            }

            _themes.Clear();
            foreach (var themeFile in Directory.EnumerateFiles(ParentDirectory, searchPattern))
            {
                try
                {
                    using (var sr = new StreamReader(themeFile))
                    {
                        var theme = JsonConvert.DeserializeObject<Theme>(sr.ReadToEnd());
                        _themes[themeFile] = theme;
                    }
                }
                catch (Exception ex)
                {
                    onFailed?.Invoke(themeFile, ex);
                }
            }
        }

        public Theme Find(string name, bool themeName = true, bool themeFileName = true)
        {
            name = name.ToLower();

            if (themeName)
            {
                foreach (var pair in _themes)
                {
                    if (pair.Value.Name.ToLower() == name)
                    {
                        return pair.Value;
                    }
                }
            }

            if (themeFileName)
            {
                foreach (var pair in _themes)
                {
                    if (Path.GetFileName(pair.Key).ToLower() == name)
                    {
                        return pair.Value;
                    }
                    else if (Path.GetFileNameWithoutExtension(pair.Key).ToLower() == name)
                    {
                        return pair.Value;
                    }
                }
            }

            return null;
        }

        public string ParentDirectory { get; }

        private Dictionary<string, Theme> _themes = new Dictionary<string, Theme>();

        public IReadOnlyDictionary<string, Theme> Themes => _themes;
    }
}
