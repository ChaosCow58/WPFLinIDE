using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Reflection;

using Newtonsoft.Json;
using System.Diagnostics;
using System.Windows.Media.Animation;


namespace WPFLinIDE01.Core
{
    public static class MetaDataFile
    {
        private static string filePath = string.Empty;
        private static string projectName = string.Empty;
        private static string fullPath = string.Empty;
        private static string globalFilePath = string.Empty;

        public static void CreateMetaFile(string filePath, string projectName)
        {
            string fullPath = @$"{Path.Combine(filePath, projectName)}.linproj";
         
            string globalDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LinIDE");
            string globalFilePath = Path.Combine(globalDataPath, "Global.linproj");

            if (!Directory.Exists(globalDataPath))
            { 
                Directory.CreateDirectory(globalDataPath);
            }

            if (!File.Exists(globalFilePath))
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

                        using (StreamWriter sm = new StreamWriter(@$"{globalFilePath}"))
                        {
                            sm.WriteLine(reader.ReadToEnd());
                            sm.Close();
                        }

                        reader.Close();
                    }
                    stream.Close();
                }
            }

            
            using (StreamReader reader = new StreamReader(globalFilePath))
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
                  
            

            MetaDataFile.filePath = filePath;
            MetaDataFile.projectName = projectName;
            MetaDataFile.fullPath = fullPath;
            MetaDataFile.globalFilePath = globalFilePath;
        }

        public static void SetMetaValue<T>(string key, T value, bool toGlobal = false) where T : IConvertible
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
                if (key == "Settings")
                {
                    dynamic localMeta = JsonConvert.DeserializeObject(File.ReadAllText(fullPath));
                    dynamic globalMeta = JsonConvert.DeserializeObject(File.ReadAllText(globalFilePath));

                    globalMeta["EditorSettings"] = localMeta["EditorSettings"];

                    File.WriteAllText(globalFilePath, JsonConvert.SerializeObject(globalMeta, Formatting.Indented));

                    Debug.WriteLine("EditorSettings updated successfully!");
                }
                else
                {
                    dynamic data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(globalFilePath));

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
            }
        }

        public static T GetMetaValue<T>(string key, bool fromGlobal = false) where T : IConvertible 
        {
            T result = default;

            if (!string.IsNullOrEmpty(fullPath) && !string.IsNullOrEmpty(globalFilePath))
            { 
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
                    dynamic data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(globalFilePath));

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

            }
            return result;
        }
    } // Class
} // Namespace
