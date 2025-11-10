/*
This Source Code Form is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/.

Copyright (c) 2025 ClearCode Inc.
*/
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyDialogAutoFiller
{
    internal class Section
    {
        public string Name;
        public string UserName;
        public string Password;

        public Section(string name)
        {
            Name = name;
        }
    }

    internal class Config
    {
        public List<Section> SectionList = new List<Section>();
    }

    internal static class ConfigLoader
    {
        internal static Config LoadConfig()
        {
            var ruleFilePath = GetRulefilePath();
            if (string.IsNullOrEmpty(ruleFilePath))
            {
                Console.Error.WriteLine("Rulefile path is not set in the registry.");
                return new Config();
            }
            using (var fileStream = new FileStream(ruleFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, 1024, true))
            {
                string data = streamReader.ReadToEnd();
                return ParseConf(data);
            }
        }

        internal static string GetRulefilePath()
        {
            const string registryPath = @"SOFTWARE\ProxyDialogAutoFiller";
            const string valueName = "Rulefile";
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath))
                {
                    if (key == null)
                    {
                        Console.Error.WriteLine($"cannot read {registryPath}: key not found");
                        return null;
                    }
                    object value = key.GetValue(valueName);
                    if (value is string rulefile)
                    {
                        return rulefile;
                    }
                    else
                    {
                        Console.Error.WriteLine($"cannot read {registryPath}: 'Rulefile' not found or not string");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"cannot read {registryPath}: {ex.Message}");
                return null;
            }
        }

        internal static Config ParseConf(string data)
        {
            var conf = new Config();
            Section section = null;

            var lines = data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                switch (line[0])
                {
                    case ';':
                    case '#':
                        // コメント行
                        break;
                    case '[':
                        // 新しいセクション追加
                        section = new Section(line.Trim(new char[]{ '[', ']' }));
                        conf.SectionList.Add(section);
                        break;
                    default:
                        if (section != null)
                        {
                            if (line.StartsWith("Password="))
                            {
                                section.Password = line.Substring("Password=".Length);
                            }
                            else if (line.StartsWith("UserName="))
                            {
                                section.UserName = line.Substring("UserName=".Length);
                            }
                        }
                        break;
                }
            }
            return conf;
        }
    }
}
