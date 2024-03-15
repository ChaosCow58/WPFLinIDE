using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Reflection;

using Newtonsoft.Json;


namespace WPFLinIDE01.Core
{
    internal static class MetaData
    {
        public static string ProjectName = string.Empty;
    }

    public static class MetaDataFile
    {
        private static string filePath = string.Empty;
        private static string projectName = string.Empty;
        private static string fullPath = string.Empty;

        public static void CreateMetaFile(string filePath, string projectName)
        {
            string fullPath = @$"{Path.Combine(filePath, projectName)}.linproj";
            if (!File.Exists(fullPath))
            {
                Assembly assembly = typeof(MainWindow).Assembly;

                using (Stream stream = assembly.GetManifestResourceStream($"WPFLinIDE01.Templates.Temp.linproj"))
                {
                    if (stream == null)
                    {
                        MessageBox.Show("Unable to load Project File.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        if (reader == null)
                        {
                            MessageBox.Show("Unable to read Project File.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        using (StreamWriter sm = new StreamWriter(@$"{fullPath}"))
                        {
                            sm.WriteLine(reader.ReadToEnd());
                            sm.Close();
                        }

                        reader.Close();
                    }
                    stream.Close();
                }
            }

            string localDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EduPartners");
            string loaclFilePath = Path.Combine(localDataPath, "global.linproj");

            using (StreamWriter sm = new StreamWriter(loaclFilePath))
            { 
                // TODO: Add Global meta file
            }

            MetaDataFile.filePath = filePath;
            MetaDataFile.projectName = projectName;
            MetaDataFile.fullPath = fullPath;
        }

        public static void SetMetaValue(string key, string value, bool toGlobal = false)
        {
            if (!toGlobal)
            {
                dynamic data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(fullPath));
          
                if (key.Contains("."))
                {
                    string[] keys = key.Split('.');
                    string nestedKey = keys[keys.Length - 1];

                    if (data[keys[keys.Length - 2]][nestedKey] == null)
                    {
                        throw new ArgumentException($"'{key}' does not exist in \"{fullPath}\".");
                    }

                    data["EditorSettings"][nestedKey] = value;
                    File.WriteAllText(fullPath, JsonConvert.SerializeObject(data, Formatting.Indented));
                }
                else
                {
                    if (data[key] == null)
                    {
                        throw new ArgumentException($"'{key}' does not exist in \"{fullPath}\".");
                    }

                    data[key] = value;
                    File.WriteAllText(fullPath, JsonConvert.SerializeObject(data, Formatting.Indented));
                }
            }
            else
            {
                if (key == "all")
                {

                }
                else
                { 
                
                }
            }
        }

        public static string GetMetaValue(string key, bool fromGlobal = false) 
        {
            string result = string.Empty;

            if (!fromGlobal)
            {
                dynamic data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(fullPath));

                if (key.Contains("."))
                {
                    string[] keys = key.Split('.');

                    string nestedKey = keys[keys.Length - 1];

                    if (data[keys[keys.Length - 2]][nestedKey] == null)
                    {
                        throw new ArgumentException($"'{key}' does not exist in \"{fullPath}\".");
                    }

                    result = data["EditorSettings"][nestedKey];
                }
                else
                {
                    if (data[key] == null)
                    {
                        throw new ArgumentException($"'{key}' does not exist in \"{fullPath}\".");
                    }

                    result = data[key];
                }
            }
            else
            { 
            
            }

            return result;
        }
    }
}
