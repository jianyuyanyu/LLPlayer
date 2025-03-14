﻿using System.Diagnostics;
using System.Windows.Forms;

namespace FlyleafLib.MediaPlayer;

public class Activity : NotifyPropertyChanged
{
    /* Player Activity Mode ( Idle / Active / FullActive )
     *
     * Required Engine's Thread (UIRefresh)
     *
     * TODO: Static?
     */

    public ActivityMode Mode
        {
        get => mode;
        set {

            if (value == mode)
                return;

            mode = value;

            if (value == ActivityMode.Idle)
            {
                swKeyboard.Reset();
                swMouse.Reset();
            }
            else if (value == ActivityMode.Active)
                swKeyboard.Restart();
            else
                swMouse.Restart();

            Utils.UI(() => SetMode());
            }
        }
    internal ActivityMode _Mode = ActivityMode.FullActive, mode = ActivityMode.FullActive;

    /// <summary>
    /// Should use Timeout to Enable/Disable it. Use this only for temporary disable.
    /// </summary>
    public bool IsEnabled           { get => _IsEnabled;
        set {

            if (value && _Timeout <= 0)
            {
                if (_IsEnabled)
                {
                    _IsEnabled = false;
                    RaiseUI(nameof(IsEnabled));
                }
                else
                    _IsEnabled = false;

            }
            else
            {
                if (_IsEnabled == value)
                    return;

                if (value)
                {
                    swKeyboard.Restart();
                    swMouse.Restart();
                }

                _IsEnabled = value;
                RaiseUI(nameof(IsEnabled));
            }
            }
        }
    bool _IsEnabled;

    public int  Timeout             { get => _Timeout; set { _Timeout = value; IsEnabled = value > 0; } }
    int _Timeout;

    Player player;
    Stopwatch swKeyboard = new();
    Stopwatch swMouse = new();

    public Activity(Player player) => this.player = player;

    /// <summary>
    /// Updates Mode UI value and shows/hides mouse cursor if required
    /// Must be called from a UI Thread
    /// </summary>
    internal void SetMode()
    {
        _Mode = mode;
        Raise(nameof(Mode));
        player.Log.Trace(mode.ToString());

        if (player.Activity.Mode == ActivityMode.Idle && player.Host != null /*&& player.Host.Player_GetFullScreen() */&& player.Host.Player_CanHideCursor())
        {
            lock (cursorLocker)
            {
                while (Utils.NativeMethods.ShowCursor(false) >= 0) { }
                isCursorHidden = true;
            }

        }
        else if (isCursorHidden && player.Activity.Mode == ActivityMode.FullActive)
        {
            lock (cursorLocker)
            {
                while (Utils.NativeMethods.ShowCursor(true) < 0) { }
                isCursorHidden = false;
            }
        }
    }

    /// <summary>
    /// Refreshes mode value based on current timestamps
    /// </summary>
    internal void RefreshMode()
    {
        if (!IsEnabled)
            mode = ActivityMode.FullActive;
        else mode = swMouse.IsRunning && swMouse.ElapsedMilliseconds < Timeout
            ? ActivityMode.FullActive
            : swKeyboard.IsRunning && swKeyboard.ElapsedMilliseconds < Timeout ? ActivityMode.Active : ActivityMode.Idle;
    }

    /// <summary>
    /// Sets Mode to Idle
    /// </summary>
    public void ForceIdle()
    {
        if (Timeout > 0)
            Mode = ActivityMode.Idle;
    }
    /// <summary>
    /// Sets Mode to Active
    /// </summary>
    public void ForceActive()       => Mode = ActivityMode.Active;
    /// <summary>
    /// Sets Mode to Full Active
    /// </summary>
    public void ForceFullActive()   => Mode = ActivityMode.FullActive;

    /// <summary>
    /// Updates Active Timestamp
    /// </summary>
    public void RefreshActive()     => swKeyboard.Restart();

    /// <summary>
    /// Updates Full Active Timestamp
    /// </summary>
    public void RefreshFullActive() => swMouse.Restart();

    #region Ensures we catch the mouse move even when the Cursor is hidden
    static bool isCursorHidden;
    static object cursorLocker = new();
    public class GlobalMouseHandler : IMessageFilter
    {
        public bool PreFilterMessage(ref Message m)
        {
            if (isCursorHidden && m.Msg == 0x0200)
            {
                try
                {
                    lock (cursorLocker)
                    {
                        while (Utils.NativeMethods.ShowCursor(true) < 0) { }
                        isCursorHidden = false;
                        foreach(var player in Engine.Players)
                            player.Activity.RefreshFullActive();
                    }

                } catch { }
            }

            return false;
        }
    }
    static Activity()
    {
        GlobalMouseHandler gmh = new();
        Application.AddMessageFilter(gmh);
    }
    #endregion
}

public enum ActivityMode
{
    Idle,
    Active,     // Keyboard only
    FullActive  // Mouse
}
