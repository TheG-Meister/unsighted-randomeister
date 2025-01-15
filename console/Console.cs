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

    public class ConsoleMessageData
    {
        public ConsoleMessage message;
        public float height;

        public ConsoleMessageData(ConsoleMessage message, float height)
        {
            this.message = message;
            this.height = height;
        }
    }

    public Font font;
    private Vector2 scroll;
    private string command;
    private Rect window;
    public int limit;
    public List<ConsoleMessageData> messages;
    private float lastMessageLabelsHeight;
    private float totalRemovedMessagesHeight;
    private bool autoScroll;

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
        this.autoScroll = true;

        Logger.Listeners.Add(this);
    }

    public void OnGUI()
    {
        if (this.messages.Count > this.limit)
        {
            List<ConsoleMessageData> removedMessages = new(this.messages.GetRange(0, this.messages.Count - this.limit));
            if (!this.autoScroll) foreach (ConsoleMessageData data in removedMessages) this.scroll.y -= data.height;
            this.messages.RemoveRange(0, this.messages.Count - this.limit);
        }

        if (this.enable.Value) this.window = GUILayout.Window(0, this.window, CreateWindow, "Console");
    }

    private void CreateWindow(int id)
    {
        GUIStyle style = new(GUI.skin.label)
        {
            font = this.font,
            fontStyle = FontStyle.Bold,
            fontSize = 20,
        };
        GUILayout.BeginVertical();

        this.scroll = GUILayout.BeginScrollView(this.scroll);

        bool repaint = true;

        float messageLabelsHeight = Math.Min(style.margin.top, style.margin.bottom);
        int maxMargin = Math.Max(style.margin.top, style.margin.bottom);

        List<ConsoleMessageData> messagesClone = new(this.messages);
        foreach (ConsoleMessageData data in messagesClone)
        {
            GUIStyle labelStyle = new(style);
            labelStyle.normal.textColor = data.message.color;

            GUILayout.Label(data.message.message, labelStyle);

            if (Event.current.type == EventType.Repaint)
            {
                data.height = GUILayoutUtility.GetLastRect().height + maxMargin;

                if (repaint) messageLabelsHeight += data.height;
                else repaint = false;
            }
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
            {
                this.scroll.y = Math.Max(0, messageLabelsHeight - scrollPaneHeight);
                this.autoScroll = true;
            }
            else this.autoScroll = false;
            this.lastMessageLabelsHeight = messageLabelsHeight;
        }
    }

    public void AddMessage(ConsoleMessage message)
    {
        this.messages.Add(new(message, 0));
    }

    public void LogEvent(object sender, LogEventArgs eventArgs)
    {
        Color color;
        switch (eventArgs.Level)
        {
            case LogLevel.Fatal:
                color = new(0.66f, 0f, 0f);
                break;
            case LogLevel.Error:
                color = Color.red;
                break;
            case LogLevel.Warning:
                color = Color.yellow;
                break;
            case LogLevel.Debug:
                color = Color.green;
                break;
            case LogLevel.Info:
                color = new(0.66f, 0.66f, 0.66f);
                break;
            default:
                color = Color.white;
                break;
        }

        this.AddMessage(new(eventArgs.ToString(), color, sender, eventArgs.Level));
    }

    public void Dispose()
    {
    }

}
