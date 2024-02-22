using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.core;

public class DelimitedFileReader
{

    public static IEnumerator<List<string>> ReadDelimitedFile(string path, char delim, string[] headers)
    {
        if (File.Exists(path))
        {
            List<string> lines = new(File.ReadAllLines(path));

            List<string> fileHeaders = null;
            foreach (string line in lines)
            {
                if (!string.IsNullOrEmpty(line) && !line.StartsWith("#"))
                {
                    string[] split = line.Split(delim);

                    if (fileHeaders == null)
                    {
                        foreach (string header in headers) if (!split.Contains(header)) throw new IOException("A header was missing from the passed file");
                        fileHeaders = new(split);
                    }
                    else
                    {
                        List<string> output = new();
                        foreach (string header in headers)
                        {
                            if (split.Length <= fileHeaders.IndexOf(header)) throw new IOException("A line did not have enough fields");
                            output.Add(split[fileHeaders.IndexOf(header)]);
                        }
                        yield return output;
                    }
                }
            }
        }
    }

}
