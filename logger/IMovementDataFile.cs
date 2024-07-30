using dev.gmeister.unsighted.randomeister.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public interface IMovementDataFile<out T> : IDelimitedFile where T : IMovementData
{

    //public void Add(T obj);

    public string GetVersionString();

    //public void CreateAndWriteHeader(IMovementDataFileVersion<T> version);

}
