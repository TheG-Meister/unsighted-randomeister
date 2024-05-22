using dev.gmeister.unsighted.randomeister.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementState : IndexedMovementData
{

    public static readonly List<string> FIELDS = new() { "id", "scene", "name" };

    public string scene;
    public string name;

    public MovementState(string name, string scene) : base(id)
    {
        this.name = name;
        this.scene = scene;
    }

    public string GetStringID()
    {
        return string.IsNullOrEmpty(scene) ? this.name : string.Join(Constants.ID_SEPARATOR.ToString(), this.scene, this.name);
    }

    public override Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            { nameof(id), this.id.ToString() },
            { nameof(scene), this.scene },
            { nameof(name), this.name },
        };
    }
}
