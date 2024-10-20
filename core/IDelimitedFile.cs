using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.core;

public interface IDelimitedFile : IDisposable
{

    public void Reset();

    public void ReadAll();

    public List<int> GetRowLengths();

    public Dictionary<int, string> GetComments();

    public void AddColumns(params string[] colNames);

    public void AddColumns(Dictionary<string, string> defaultValuesDict);

    public void RemoveLines(params int[] lines);

    public void SetField(int row, string colName, string field);

    public List<string> SetEntry(int index, Dictionary<string, string> fields);

    public void AddSubstitution(string colName, string term, string substitution);

    public void SubstituteAll();

    public void Substitute(string colName, string term, string substitution);

    public Dictionary<string, string> GetEntry(int index);

    public int Add(Dictionary<string, string> fields);

    public int AddComment(string comment);

    public int AddColNamesLine(params string[] colNames);

    public void WriteAll();

    public int GetLastLine();

    public bool HasLineErrors();

    public void FixLineErrors();

}
