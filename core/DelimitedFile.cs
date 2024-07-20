using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.core;

public class DelimitedFile
{
    public const char COMMENT_CHAR = '#';
    public const string MISSING_COL_NAME = null;
    public const string BLANK_LINE = "";
    public const string MISSING_FIELD = null;

    public readonly string path;
    public char delim;

    public StreamWriter writer;
    public bool modified;

    public int colNamesLine;
    public List<string> colNames;
    public Dictionary<int, string> unusedLines;
    public Dictionary<int, List<string>> rows;
    public int lastLine;

    public Dictionary<string, Dictionary<string, string>> substitutions;

    public DelimitedFile(string path, char delim)
    {
        this.path = path;
        this.delim = delim;

        this.writer = null;
        this.modified = false;

        this.colNamesLine = -1;
        this.colNames = null;
        this.unusedLines = new();
        this.rows = new();
        this.lastLine = -1;

        this.substitutions = new();
    }

    public bool Exists() => File.Exists(path);

    public void Create()
    {
        this.writer = new StreamWriter(this.path, false);
        this.lastLine = 0;
    }

    public virtual void ReadAll()
    {
        List<string> lines = new(File.ReadAllLines(path));
        bool colNamesFound = false;

        this.unusedLines = new();
        this.rows = new();

        for (int lineNum = 0; lineNum < lines.Count; lineNum++)
        {
            string line = lines[lineNum];

            if (line == null) this.unusedLines[lineNum] = BLANK_LINE;
            else if (string.IsNullOrEmpty(line) || line.StartsWith(COMMENT_CHAR.ToString())) this.unusedLines[lineNum] = line;
            else
            {
                List<string> row = new(line.Split(delim));
                if (!colNamesFound)
                {
                    this.colNames = row;
                    this.colNamesLine = lineNum;
                    colNamesFound = true;
                }
                else this.rows[lineNum] = row;
            }
        }

        this.lastLine = lines.Count - 1;
    }

    public List<int> GetRowLengths()
    {
        return this.rows.Values.Select(r => rows.Count).Distinct().ToList();
    }

    public Dictionary<int, string> GetComments()
    {
        Dictionary<int, string> result = new();

        foreach (int key in this.unusedLines.Keys)
        {
            if (this.unusedLines[key].StartsWith(COMMENT_CHAR.ToString())) result[key] = this.unusedLines[key].Substring(1);
        }

        return result;
    }

    public void AddColumns(params string[] colNames)
    {
        Dictionary<string, string> defaultValuesDict = new();
        foreach (string colName in colNames) defaultValuesDict[colName] = MISSING_FIELD;

        this.AddColumns(defaultValuesDict);
    }

    public virtual void AddColumns(Dictionary<string, string> defaultValuesDict)
    {
        this.colNames.AddRange(colNames);
        foreach (List<string> fields in this.rows.Values) foreach (string header in defaultValuesDict.Keys) fields.Add(defaultValuesDict[header]);
    }

    public void RemoveLines(params int[] lines)
    {
        foreach (int line in lines)
        {
            if (this.unusedLines.ContainsKey(line)) this.unusedLines.Remove(line);
            if (this.rows.ContainsKey(line)) this.rows.Remove(line);
        }

        this.modified = true;
        this.FixLineErrors();
    }

    public void SetField(int row, string colName, string field)
    {
        if (colName == this.colNames[0] && field.StartsWith(COMMENT_CHAR.ToString())) throw new ArgumentException($"{nameof(field)} would comment out the line");
        if (field.Contains(this.delim)) throw new ArgumentException($"{nameof(field)} contains the file delimiter.");

        this.rows[row][this.colNames.IndexOf(colName)] = field;
        this.modified = true;
    }

    public List<string> SetEntry(int index, Dictionary<string, string> fields)
    {
        List<string> fieldsList = new();

        foreach (string col in this.colNames)
        {
            if (fields.ContainsKey(col)) fieldsList.Add(fields[col]);
            else fieldsList.Add("");
        }

        this.rows[index] = fieldsList;
        this.modified = true;

        return fieldsList;
    }

    public void AddSubstitution(string colName, string term, string substitution)
    {
        if (colName == this.colNames[0] && substitution.StartsWith(COMMENT_CHAR.ToString())) throw new ArgumentException($"{nameof(substitution)} would comment out the line");
        if (substitution.Contains(this.delim)) throw new ArgumentException($"{nameof(substitution)} contains the file delimiter.");

        if (!this.substitutions.ContainsKey(colName)) this.substitutions[colName] = new();
        this.substitutions[colName].Add(term, substitution);

        this.Substitute(colName, term, substitution);
    }

    public void SubstituteAll()
    {
        foreach (string colName in this.substitutions.Keys)
        {
            int colIndex = this.colNames.IndexOf(colName);
            foreach (List<string> row in this.rows.Values)
            {
                string term = row[colIndex];
                if (this.substitutions[colName].ContainsKey(term))
                {
                    row[colIndex] = this.substitutions[colName][term];
                    this.modified = true;
                }
            }
        }
    }

    public void Substitute(string colName, string term, string substitution)
    {
        if (colName == this.colNames[0] && substitution.StartsWith(COMMENT_CHAR.ToString())) throw new ArgumentException($"{nameof(substitution)} would comment out the line");
        if (substitution.Contains(this.delim)) throw new ArgumentException($"{nameof(substitution)} contains the file delimiter.");

        int colIndex = this.colNames.IndexOf(colName);
        foreach (List<string> row in this.rows.Values) if (row[colIndex] == term)
            {
                row[colIndex] = substitution;
                this.modified = true;
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
        if (fields.ContainsKey(this.colNames[0]) && fields[colNames[0]].StartsWith(COMMENT_CHAR.ToString())) throw new ArgumentException($"The field for {this.colNames[0]} would comment out the line");

        List<string> fieldsList = new();

        foreach (string col in this.colNames)
        {
            if (fields.ContainsKey(col))
            {
                string field = fields[col];

                if (this.substitutions.ContainsKey(col) && this.substitutions[col].ContainsKey(field)) field = this.substitutions[col][field];

                if (field.Contains(this.delim)) throw new ArgumentException($"The field for {col} contains the file delimiter.");
                fieldsList.Add(field);
            }
            else fieldsList.Add("");
        }

        this.lastLine++;
        this.rows[this.lastLine] = fieldsList;
        this.WriteLine(string.Join(this.delim.ToString(), fieldsList));

        return this.lastLine;
    }

    public int AddComment(string comment)
    {
        string line = "#" + comment;

        this.lastLine++;
        this.unusedLines[this.lastLine] = line;
        this.WriteLine(line);

        return this.lastLine;
    }

    public int AddColNamesLine(params string[] colNames)
    {
        this.lastLine++;
        this.colNamesLine = lastLine;
        this.colNames = new(colNames);
        this.WriteLine(string.Join(this.delim.ToString(), this.colNames));

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
        if (this.HasLineErrors()) this.FixLineErrors();

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
        this.modified = false;
    }

    public int GetLastLine()
    {
        int max = Math.Max(this.unusedLines.Keys.Max(), this.rows.Keys.Max());
        max = Math.Max(max, this.colNamesLine);
        max++;
        return max;
    }

    public bool HasLineErrors()
    {
        if (this.rows.Keys.Min() >= this.colNamesLine) return true;
        else if (Math.Max(this.unusedLines.Keys.Max(), this.rows.Keys.Max()) != this.lastLine) return true;
        else if (this.unusedLines.ContainsKey(this.colNamesLine)) return true;
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
        Dictionary<int, string> unusedLines = new();
        Dictionary<int, List<string>> rows = new();

        bool setHeaderLine = false;
        int line = 0;
        for (int i = 0; this.unusedLines.Count > 0 || this.rows.Count > 0; i++)
        {
            bool unused = this.unusedLines.ContainsKey(i);
            bool row = this.rows.ContainsKey(i);

            if (unused)
            {
                unusedLines[line] = this.unusedLines[i];
                this.unusedLines.Remove(i);
                line++;
                this.lastLine = line;
            }

            if (row)
            {
                if (!setHeaderLine)
                {
                    this.colNamesLine = line;
                    line++;
                    setHeaderLine = true;
                }

                rows[line] = this.rows[i];
                this.rows.Remove(i);
                line++;
                this.lastLine = line;
            }
        }

        this.unusedLines = unusedLines;
        this.rows = rows;
        this.modified = true;
    }

}
