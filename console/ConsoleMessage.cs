using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace dev.gmeister.unsighted.randomeister.console;

public class ConsoleMessage
{

    public string message;
    public Color color;
    public object sender;
    public LogLevel level;

    public ConsoleMessage(string message)
    {
    }

    public ConsoleMessage(string message, Color color, object sender, LogLevel level)
    {
        this.message = message;
        this.color = color;
        this.sender = sender;
        this.level = level;
    }
}
