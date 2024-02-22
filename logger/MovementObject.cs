using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementObject
{

    public string type;
    public string scene;
    public string name;

    public MovementObject()
    {
    }

    public MovementObject(string type, string scene, string name)
    {
        this.type = type;
        this.scene = scene;
        this.name = name;
    }
}
