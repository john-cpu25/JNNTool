using System;
using System.Collections.Generic;
using System.IO;

namespace JNNTool.Tools.CSIxRevit.Parser
{
    public class E2KReader
    {
        public Dictionary<string, List<string>> Read(string filePath)
        {
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            string[] lines = File.ReadAllLines(filePath);
            
            string currentTable = "HEADER";
            result[currentTable] = new List<string>();

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";")) continue;

                if (line.StartsWith("$"))
                {
                    currentTable = line.Substring(1).Trim().Trim('"').ToUpper();
                    if (!result.ContainsKey(currentTable))
                        result[currentTable] = new List<string>();
                    continue;
                }
                else if (line.StartsWith("TABLE:", StringComparison.OrdinalIgnoreCase))
                {
                    currentTable = line.Substring(6).Trim().Trim('"').ToUpper();
                    if (!result.ContainsKey(currentTable))
                        result[currentTable] = new List<string>();
                    continue;
                }

                result[currentTable].Add(line);
            }

            return result;
        }
    }
}
