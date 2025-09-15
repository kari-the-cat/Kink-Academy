using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BP_testing
{
    using OpenQA.Selenium.Chrome;
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text.Json;

    public class KASettings
    {
        public string hostname = "ws://localhost:12345/buttplug";

        public KASettings() { }
        public KASettings(KASettings other)
        {
            hostname = other.hostname;
            // Copy other fields here
        }

        private static string ConfigFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".kinkacademy");
        private static string ConfigFile => Path.Combine(ConfigFolder, "settings.json");

        // Load settings (or create default if not exist)
        public static KASettings Load()
        {
            if (!File.Exists(ConfigFile))
            {
                var s = new KASettings();
                s.Save(); // create default
                return s;
            }

            string json = File.ReadAllText(ConfigFile);
            return JsonSerializer.Deserialize<KASettings>(json) ?? new KASettings();
        }

        // Save settings
        public void Save()
        {
            Directory.CreateDirectory(ConfigFolder); // ensure folder exists
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFile, json);
        }
    }

    public class SettingsTUI
    {
        // In Setting view, no connection is active so we can block execution
        // 

        public required KASettings settings { get; set; }

        public static bool getInt(string val, out int res, int low = 0, int high = int.MaxValue)
        {
            try
            {
                res = int.Parse(val);
                return !(res < low || res > high);
            }
            catch
            {
                res = 0;
                return false;
            }
        }


        public static bool getBoolean(string val, out bool res)
        {
            val = val.Trim().ToLower();
            if (val == "y" || val == "yes" || val == "true" || val == "1")
            {
                res = true;
                return true;
            }
            else if (val == "n" || val == "no" || val == "false" || val == "0")
            {
                res = false;
                return true;
            }
            else
            {
                res = false;
                return false;
            }
        }

        static void ClearLine()
        {
            int col = Console.CursorLeft;
            Console.Write( "\r" + new string(' ', col+5) + "\r");
        }
        public static string promptInput(string field_name, string value)
        {
            while (true)
            {
                ClearLine();
                Console.Write(field_name + ": ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(value);
                Console.ResetColor();
                ConsoleKeyInfo key = Console.ReadKey(false);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return value;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (value.Length > 0)
                        value = value.Substring(0, value.Length - 1);
                }
                else
                {
                    value += key.KeyChar;
                }
            }
            

        }

        

        private string ReadString(FieldInfo field)
        {

            string input = promptInput(field.Name, (string)field.GetValue(settings)!);
            return string.IsNullOrEmpty(input) ? (string)field.GetValue(settings)! : input;
        }

        private int ReadInt(FieldInfo field)
        {
            int res;
            string input = promptInput(field.Name, field.GetValue(settings)!.ToString()!);
            while (!getInt(input, out res))
            {
                input = promptInput(field.Name + " (invalid input)", field.GetValue(settings)!.ToString()!);
            }
            return res;
        }
        private bool ReadBool(FieldInfo field)
        {
            bool res;
            string input = promptInput(field.Name + " (y/n)", ((bool)field.GetValue(settings)!).ToString());
            while (!getBoolean(input, out res))
            {
                input = promptInput(field.Name + " (invalid input, y/n)", ((bool)field.GetValue(settings)!).ToString());
            }
            return res;
        }



        public void Show()
        {
            var backup = (new KASettings(settings));
            var t = settings.GetType();
            foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.FieldType == typeof(string))
                    field.SetValue(settings, ReadString(field));
                else if (field.FieldType == typeof(int))
                    field.SetValue(settings, ReadInt(field));
                else if (field.FieldType == typeof(bool))
                    field.SetValue(settings, ReadBool(field));
            }
            bool confirm = promptInput("Confirm changes? (y/n)", "").Trim().ToLower() == "y";
            if (!confirm)
            {
                settings = backup;
            }
            settings.Save();

        }

    }

}
