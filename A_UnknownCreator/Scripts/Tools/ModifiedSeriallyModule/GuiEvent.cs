﻿using UnityEngine;

namespace UnknownCreator.Modules
{
    public struct GuiEvent
    {
        public EventType Type { get; set; }
        public Vector2 MousePosition { get; }
        public int ClickCount { get; }
        public int Button { get; }

        public GuiEvent(
            EventType type,
            Vector2 mousePosition,
            int clickCount,
            int button
        )
        {
            Type = type;
            MousePosition = mousePosition;
            ClickCount = clickCount;
            Button = button;
        }

        public static GuiEvent FromCurrentUnityEvent
        {
            get
            {
                var evt = Event.current;
                return new GuiEvent(evt.type, evt.mousePosition, evt.clickCount, evt.button);
            }
        }
    }
}
