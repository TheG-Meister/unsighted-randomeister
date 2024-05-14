using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public interface IMovementData
{

    public Dictionary<string, string> ToDictionary();

}
