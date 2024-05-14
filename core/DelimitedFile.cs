using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.core;

public class DelimitedFile
{
    public const string MISSING_COL_NAME = "";
    public const string BLANK_LINE = "";
    public const string BLANK_FIELD = "";

    public readonly string path;
    public char delim;

    public int colNamesLine;
    public List<string> colNames;
    public Dictionary<int, string> unusedLines;
    public Dictionary<int, List<string>> rows;
    public int lastLine;

    public StreamWriter writer;

    public DelimitedFile(string path, char delim)
    {
        this.path = path;
        this.delim = delim;
    }

    public bool Exists() => File.Exists(path);

    public void ReadColNames()
    {
        IEnumerable<string> lines = File.ReadLines(path);
        foreach (string line in lines)
        {
            if (!string.IsNullOrEmpty(line) && !line.StartsWith("#"))
            {
                this.colNames = new(line.Split(delim));
                break;
            }
        }
    }

    public void AddColumns(params string[] colNames)
    {
        Dictionary<string, string> defaultValuesDict = new();
        foreach (string colName in colNames) defaultValuesDict[colName] = BLANK_FIELD;

        this.AddColumns(defaultValuesDict);
    }

    public virtual void AddColumns(Dictionary<string, string> defaultValuesDict)
    {
        this.colNames.AddRange(colNames);
        foreach (List<string> fields in this.rows.Values) foreach (string header in defaultValuesDict.Keys) fields.Add(defaultValuesDict[header]);
    }

    public virtual void Read()
    {
        List<string> lines = new(File.ReadAllLines(path));
        bool colNamesFound = false;

        this.unusedLines = new();
        this.rows = new();

        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            this.lastLine = i;

            if (line == null) this.unusedLines[i] = BLANK_LINE;
            else if (string.IsNullOrEmpty(line) || line.StartsWith("#")) this.unusedLines[i] = line;
            else
            {
                List<string> row = new(line.Split(delim));
                if (!colNamesFound)
                {
                    this.colNames = row;
                    this.colNamesLine = i;
                    colNamesFound = true;
                }
                else
                {
                    while (this.colNames.Count < row.Count) this.colNames.Add(MISSING_COL_NAME);
                    while (row.Count < this.colNames.Count) row.Add(BLANK_FIELD);
                    this.rows[i] = row;
                }
            }
        }

        //Normalise row lengths
        foreach (List<string> row in this.rows.Values)
        {
            if (row.Count > this.colNames.Count) throw new ApplicationException("row has more fields than columns");
            else if (row.Count == this.colNames.Count) break;

            while (row.Count < this.colNames.Count) row.Add(BLANK_FIELD);
        }
    }

    public Dictionary<string, string> GetEntry(int index)
    {
        if (!this.rows.ContainsKey(index)) throw new ArgumentException($"{nameof(index)} {index} does not exist.");

        Dictionary<string, string> output = new();
        List<string> row = this.rows[index];

        for (int i = 0; i < this.colNames.Count; i++) output[this.colNames[i]] = row[i];

        return output;
    }

    public int Add(Dictionary<string, string> fields)
    {
        List<string> fieldsList = new();

        foreach (string col in this.colNames)
        {
            if (fields.ContainsKey(col)) fieldsList.Add(fields[col]);
            else fieldsList.Add("");
        }

        this.lastLine++;
        this.rows[this.lastLine] = fieldsList;
        this.WriteLine(string.Join(this.delim.ToString(), fieldsList));

        return this.lastLine;
    }

    public int AddComment(string comment)
    {
        string line = "# " + comment;

        this.lastLine++;
        this.unusedLines[this.lastLine] = line;
        this.WriteLine(line);

        return this.lastLine;
    }

    private void WriteLine(string line)
    {
        this.writer ??= new StreamWriter(this.path, true);
        this.writer.WriteLine(line);
        this.writer.Flush();
    }

    public void WriteAll()
    {
        if (this.HasLineErrors()) throw new ApplicationException("Finish line error code");

        this.writer = new StreamWriter(this.path, false);

        int max = Math.Max(this.unusedLines.Keys.Max(), this.rows.Keys.Max());
        max = Math.Max(max, this.colNamesLine);
        max++;

        for (int i = 0; i < max; i++)
        {
            if (this.colNamesLine == i) this.writer.WriteLine(string.Join(this.delim.ToString(), this.colNames));
            else if (this.rows.ContainsKey(i)) this.writer.WriteLine(string.Join(this.delim.ToString(), this.rows[i]));
            else if (this.unusedLines.ContainsKey(i)) this.writer.WriteLine(this.unusedLines[i]);
            else
            {
                this.writer.WriteLine(BLANK_LINE);
                this.unusedLines.Add(i, BLANK_LINE);
            }
        }

        this.writer.Flush();
    }

    public bool HasLineErrors()
    {
        //Line error for if the header line is below the first split line
        //Also line error for if the largest line number is off
        //if ()
        if (this.unusedLines.ContainsKey(this.colNamesLine)) return true;
        else if (this.rows.ContainsKey(this.colNamesLine)) return true;
        else
        {
            int min = Math.Min(this.unusedLines.Keys.Max(), this.rows.Keys.Max());
            for (int i = 0; i < min; i++) if (this.unusedLines.ContainsKey(i) && this.rows.ContainsKey(i)) return true;
        }
        return false;
    }

    public void FixLineErrors()
    {
        int max = Math.Max(this.unusedLines.Keys.Max(), this.rows.Keys.Max());
        max = Math.Max(max, this.colNamesLine);
        max++;

        bool changedHeaderLine = false;
        int line = 0;
        for (int i = 0; i < max; i++)
        {
            bool header = i == this.colNamesLine;
            bool unused = this.unusedLines.ContainsKey(i);
            bool row = this.rows.ContainsKey(i);


        }
    }

}
