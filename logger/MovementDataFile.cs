using dev.gmeister.unsighted.randomeister.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementDataFile<T> : DelimitedFile, IMovementDataFile where T : class, IMovementData
{

    public Dictionary<string, string> header;
    public Dictionary<int, T> parsedData;
    public List<MovementDataFileVersion<T>> versions;
    public MovementDataFileVersion<T> version;
    public Func<Dictionary<string, string>, T> factory;

    public MovementDataFile(string path, Func<Dictionary<string, string>, T> factory, List<MovementDataFileVersion<T>> versions) : base(path, '\t')
    {
        this.factory = factory;
        this.versions = versions;
    }

    public MovementDataFile(Stream stream, Func<Dictionary<string, string>, T> factory, List<MovementDataFileVersion<T>> versions) : base(stream, '\t')
    {
        this.factory = factory;
        this.versions = versions;
    }

    public override void ReadAll()
    {
        base.ReadAll();

        List<string> headerLines = new();
        foreach (KeyValuePair<int, string> kvp in this.unusedLines)
        {
            if (kvp.Key >= this.colNamesLine) break;

            string line = kvp.Value;
            if (line.StartsWith(COMMENT_CHAR.ToString()))
            {
                headerLines.Add(line.Substring(line.IndexOf(COMMENT_CHAR.ToString()) + 1));
            }
        }

        this.header = MovementDataFileVersion<T>.ParseHeader(headerLines);
    }

    public virtual bool Contains(T obj)
    {
        return this.Find(obj) != null;
    }

    public virtual T Find(T obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));

        foreach (T other in this.parsedData.Values)
        {
            if (obj.Equals(other)) return other;
        }

        return null;
    }

    /**
     * <returns>the line number of the added object, or -1 if the object was not added</returns>
     */
    public virtual int Add(T obj)
    {
        int index = this.Add(obj.ToDictionary());
        this.parsedData[index] = obj;
        return index;
    }

    public void FindVersion()
    {
        if (this.header == null) throw new ApplicationException("file has not been read");
        if (this.header.Count < 1) throw new IOException("file header is empty");

        if (!this.header.TryGetValue(this.versions[0].GetTypeKey(), out string type)) throw new IOException("file header does not contain a type key");
        if (type != typeof(T).FullName) throw new IOException("file has the wrong type of data");

        if (!this.header.TryGetValue(this.versions[0].GetVersionKey(), out string versionString)) throw new IOException("file header does not contain a version key");
        MovementDataFileVersion<T> version = this.versions.Find(v => v.Version == versionString);
        if (version == null) throw new IOException("could not find version data for this file's version string");

        version.VerifyColNames(this.colNames);
        this.version = version;
    }

    public virtual void Create()
    {
        base.Reset();
        this.version = this.versions[this.versions.Count - 1];
        this.header = version.ToDictionary();
        List<string> headerLines = version.ToHeader();
        foreach (string line in headerLines) this.AddComment(line);
        this.AddColNamesLine(version.ColNames.ToArray());

        this.parsedData = new();
    }

    public virtual Dictionary<int, Exception> Parse()
    {
        this.parsedData = new();
        Dictionary<int, Exception> result = new();
        foreach (int key in this.rows.Keys)
        {
            Dictionary<string, string> entry = this.GetEntry(key);
            try
            {
                this.parsedData[key] = this.factory.Invoke(entry);
                result[key] = null;
            }
            catch (Exception e)
            {
                result[key] = e;
            }
        }

        return result;
    }

}
