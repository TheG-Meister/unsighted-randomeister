using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.core;

public class DelimitedFiles
{

    public static List<List<string>> Read(string path, char delim, out List<string> headers)
    {
        List<List<string>> output = new();
        headers = null;

        if (File.Exists(path))
        {
            List<string> lines = new(File.ReadAllLines(path));

            foreach (string line in lines)
            {
                if (!string.IsNullOrEmpty(line) && !line.StartsWith("#"))
                {
                    string[] split = line.Split(delim);

                    if (headers == null)
                    {
                        headers = new(split);
                    }
                    else
                    {
                        List<string> fields = new(split);
                        while (fields.Count < headers.Count) fields.Add(null);
                        output.Add(fields);
                    }
                }
            }
        }

        return output;
    }

}
