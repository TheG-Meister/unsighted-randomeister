using BepInEx.Configuration;
using BepInEx.Logging;
using dev.gmeister.unsighted.randomeister.core;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Logger = BepInEx.Logging.Logger;

namespace dev.gmeister.unsighted.randomeister.console;

public class Console : ILogListener
{

    public Font font;
    private Vector2 scroll;
    private string command;
    private Rect window;
    public int limit;
    public List<ConsoleMessage> messages;
    private float lastMessageLabelsHeight;

    private ConfigEntry<bool> enable;

    public Console(ConfigFile config)
    {
        this.enable = config.Bind<bool>("Developer", "Console", false, "Opens the developer console");

        this.font = Font.CreateDynamicFontFromOSFont("Courier New", 15);
        this.scroll = default;
        this.command = "";
        this.limit = 500;
        this.messages = new();
        this.window = new Rect(20, 20, 1880, 1040);
        this.lastMessageLabelsHeight = 0;

        Logger.Listeners.Add(this);
    }

    public void OnGUI()
    {
        if (this.enable.Value) this.window = GUILayout.Window(0, this.window, CreateWindow, "Console");
    }

    private void CreateWindow(int id)
    {
        GUIStyle style = new(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            margin = new RectOffset(),
            padding = new RectOffset(),
            font = this.font,
            fontStyle = FontStyle.Bold,
            fontSize = 20,
        };
        GUILayout.BeginVertical();

        this.scroll = GUILayout.BeginScrollView(this.scroll);

        bool repaint = true;

        float messageLabelsHeight = 0;

        List<ConsoleMessage> removedMessages = new();
        if (this.messages.Count > this.limit)
        {
            removedMessages.AddRange(this.messages.GetRange(0, this.messages.Count - this.limit));
            this.messages.RemoveRange(0, this.messages.Count - this.limit);
        }

        List<ConsoleMessage> messagesClone = new(this.messages);
        foreach (ConsoleMessage message in messagesClone)
        {
            GUILayout.Label(message.message, style);
            if (repaint && Event.current.type == EventType.Repaint)
            {
                messageLabelsHeight += GUILayoutUtility.GetLastRect().height;
            }
            else repaint = false;
        }
        GUILayout.EndScrollView();
        float scrollPaneHeight = 0f;
        if (Event.current.type == EventType.Repaint) scrollPaneHeight = GUILayoutUtility.GetLastRect().height;
        else repaint = false;

        this.command = GUILayout.TextArea(this.command);
        GUILayout.EndVertical();
        GUI.DragWindow();

        if (repaint)
        {
            //enable auto-scrolling if the panel is scolled to the bottom
            if (Input.mouseScrollDelta.y <= 0 && this.scroll.y > this.lastMessageLabelsHeight - scrollPaneHeight - 1)
                this.scroll.y = Math.Max(0, messageLabelsHeight - scrollPaneHeight + 500);
            this.lastMessageLabelsHeight = messageLabelsHeight;
        }
    }

    public void AddMessage(ConsoleMessage message)
    {
        this.messages.Add(message);
    }

    public void LogEvent(object sender, LogEventArgs eventArgs)
    {
        this.AddMessage(new(eventArgs.ToString(), Color.white, sender, eventArgs.Level));
    }

    public void Dispose()
    {
    }

}
