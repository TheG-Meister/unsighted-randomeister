using dev.gmeister.unsighted.randomeister.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementState
{

    public int id;
    public string name;
    public string scene;

    public MovementState(int id, string name, string scene)
    {
        this.id = id;
        this.name = name;
        this.scene = scene;
    }

    public MovementState(string id, string name, string scene)
    {

    }

    public string GetStringID()
    {
        return string.IsNullOrEmpty(scene) ? this.name : string.Join(Constants.ID_SEPARATOR.ToString(), this.scene, this.name);
    }

}
