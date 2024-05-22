using dev.gmeister.unsighted.randomeister.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementActionsFile : IndexedMovementDataFile<MovementAction>
{

    public MovementActionsFile(string path) : base(path, MovementAction.FIELDS)
    {
        if (this.Exists())
        {
            this.ReadColNames();

            List<string> missingFieldNames = new(this.fieldNames);
            foreach (string colName in this.colNames) if (missingFieldNames.Contains(colName)) missingFieldNames.Remove(colName);

            if (missingFieldNames != null && missingFieldNames.Count > 0) throw new IOException("File does not contain MovementAction column names");
        }
        else this.Create(this.fieldNames.ToArray());
    }

    public override void Read()
    {
        base.Read();
        List<int> linesToRemove = new();

        foreach (int i in this.rows.Keys)
        {
            Dictionary<string, string> row = this.GetEntry(i);

            try
            {
                this.parsedData[i] = new(row);
            }
            catch
            {
                linesToRemove.Add(i);
            }
        }

        
        if (linesToRemove.Count > 0) this.RemoveLines(linesToRemove.ToArray());

        if (this.modified) this.WriteAll();
    }

}
