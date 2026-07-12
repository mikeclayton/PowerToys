// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

using Microsoft.PowerToys.Telemetry;
using MouseWithoutBorders.Class;
using MouseWithoutBorders.Machines;

// <summary>
//     Machine setup/switching implementation.
// </summary>
// <history>
//     2008 created by Truong Do (ductdo).
//     2009-... modified by Truong Do (TruongDo).
//     2023- Included in PowerToys.
// </history>
namespace MouseWithoutBorders.Core;

internal static class MachineStuff
{
    internal const byte MAX_MACHINE = 4;
    private const byte MAX_SOCKET = MAX_MACHINE * 2;
    internal const long HEARTBEAT_TIMEOUT = 1500000; // 30 Mins

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
    internal static ID desMachineID;
#pragma warning restore SA1307
#pragma warning disable SA1306 // Field should begin with a lower-case letter
    internal static string DesMachineName = string.Empty;
#pragma warning restore SA1306
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
    internal static ID newDesMachineID;
    internal static ID newDesMachineIdEx;
    internal static ID dropMachineID;
    internal static long lastJump = Common.GetTick();
    internal static MyRectangle desktopBounds = new();
    internal static MyRectangle primaryScreenBounds = new();
#pragma warning restore SA1307
    private static MachineMatrix _liveMatrix;

    internal static MachineMatrix MachineMatrix
    {
        get
        {
            _liveMatrix ??= new MachineMatrix(1, 4);
            return _liveMatrix;
        }
    }

    internal static MyRectangle PrimaryScreenBounds => MachineStuff.primaryScreenBounds;

#pragma warning disable SA1306 // Field should begin with a lower-case letter
    internal static MouseLocation SwitchLocation = new();
#pragma warning restore SA1306

    internal static ID NewDesMachineID
    {
        get => MachineStuff.newDesMachineID;
        set => MachineStuff.newDesMachineID = value;
    }

    internal static MyRectangle DesktopBounds => MachineStuff.desktopBounds;

    internal static bool RemoveDeadMachines(ID ip)
    {
        bool rv = false;

        // Here we are removing a dead machine by IP.
        foreach (MachineEntry entry in MachineStuff.MachineMatrix.GetAllEntries())
        {
            if (entry.Id == ip)
            {
                if (MachineService.SetMachineDisconnected(MachineStuff.MachineMatrix, entry.Hostname, Common.GetTick(), MachineStuff.HEARTBEAT_TIMEOUT))
                {
                    rv = true;
                }

                Logger.LogDebug("<><><><><>>><><><<><><><><><><><><><><>><><><><><><><><><><><" + entry.Hostname);
            }
        }

        return rv;
    }

    internal static void RemoveDeadMachines()
    {
        // list of live/dead machines is now automatically up-to-date
        // if it changed we need to update the UI.
        // for now assume it changed.
        // Common.MachinePool.RemoveIdsFromEntries();
        // DoSomethingInUIThread(UpdateMenu);
        MachineStuff.UpdateMachinePoolStringSetting();

        // Make sure MachinePool still holds this machine.
        if (MachineStuff.MachineMatrix.TryAddMachine(Common.MachineName, out _))
        {
            _ = MachineStuff.MachineMatrix.TryUpdateMachineID(Common.MachineName, Common.MachineID, false, Common.GetTick());
        }
    }

    internal static string AddToMachinePool(DATA package)
    {
        // Log("********** AddToMachinePool called: " + package.src.ToString(CultureInfo.InvariantCulture));

        // There should be no duplicates in machine pool.
        string name = package.MachineName;

        // a few things happening here:
        // 1) find a matching machine (by name)
        // 2) update its ID and time
        // 3) logging
        // 4) updating some variables - desMachineID/newDesMachineID
        // 5) return the matched name (trimmed) - only in the event of a match
        if (MachineStuff.MachineMatrix.TryGetEntryByHostname(name, out var machineEntry))
        {
            _ = MachineStuff.MachineMatrix.TryUpdateMachineID(machineEntry!.Hostname, machineEntry.Id, true, Common.GetTick());

            _ = MachineStuff.MachineMatrix.TryUpdateMachineID(machineEntry.Hostname, package.Src, true, Common.GetTick());

            if (machineEntry.Hostname.Equals(DesMachineName, StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogDebug("AddToMachinePool: Des ID updated: " + Common.DesMachineID.ToString() + "/" + package.Src.ToString());
                newDesMachineID = desMachineID = package.Src;
            }

            return machineEntry.Hostname;
        }
        else
        {
            if (MachineStuff.MachineMatrix.TryAddMachine(name, out _))
            {
                _ = MachineStuff.MachineMatrix.TryUpdateMachineID(name, package.Src, true, Common.GetTick());
            }
            else
            {
                Logger.LogDebug("AddToMachinePool: could not add a new machine: " + name);
                return "The 5th machine";
            }
        }

        // if (machineCount != saved)
        {
            // DoSomethingInUIThread(UpdateMenu);
            MachineStuff.UpdateMachinePoolStringSetting();
        }

        // NOTE(yuyoyuppe): automatically active "bidirectional" control between the machines.
        string[] st = new string[MachineStuff.MAX_MACHINE];
        Array.Fill(st, string.Empty);
        var machines = MachineStuff.MachineMatrix.GetAllEntries();
        for (int i = 0; i < machines.Count; ++i)
        {
            if (machines[i].Id != ID.NONE && machines[i].Id != ID.ALL)
            {
                st[i] = machines[i].Hostname;
            }
        }

        MachineStuff.MachineMatrix.Initialize(st);
        MachineStuff.SaveMachineMatrixToSettings();
        Common.ReopenSockets(true);
        MachineStuff.SendMachineMatrix();

        Logger.LogDebug("Machine added: " + name + "/" + package.Src.ToString());
        UpdateClientSockets("AddToMachinePool");
        return name;
    }

    internal static void UpdateClientSockets(string logHeader)
    {
        Logger.LogDebug("UpdateClientSockets: " + logHeader);
        Common.Sk?.UpdateTCPClients();
    }

    private static SettingsForm settings;

    internal static SettingsForm Settings
    {
        get => MachineStuff.settings;
        set => MachineStuff.settings = value;
    }

    internal static void ShowSetupForm(bool reopenSockets = false)
    {
        Logger.LogDebug("========== BEGIN THE SETUP EXPERIENCE ==========", true);
        Setting.Values.MyKey = Encryption.MyKey = Encryption.CreateRandomKey();
        Encryption.GeneratedKey = true;

        if (Process.GetCurrentProcess().SessionId != NativeMethods.WTSGetActiveConsoleSessionId())
        {
            Logger.Log("Not physical console session.");
            _ = MessageBox.Show(
                "Please run the program in the physical console session.\r\nThe program does not work in a remote desktop or virtual machine session.",
                Application.ProductName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Stop);
            return;
        }

        if (settings == null)
        {
            settings = new SettingsForm();
            settings.Show();
        }
        else
        {
            settings.Close();
            Common.MMSleep(0.3);
            settings = new SettingsForm();
            settings.Show();
        }

        if (reopenSockets)
        {
            Common.ReopenSockets(true);
        }
    }

    internal static void CloseSetupForm()
    {
        if (settings != null)
        {
            settings.Close();
            settings = null;
        }
    }

    internal static void ShowMachineMatrix()
    {
        if (!Setting.Values.ShowOriginalUI)
        {
            return;
        }

        if (Process.GetCurrentProcess().SessionId != NativeMethods.WTSGetActiveConsoleSessionId())
        {
            Common.ShowToolTip(Application.ProductName + " cannot be used in a remote desktop or virtual machine session.", 5000);
        }

#if NEW_SETTINGS_FORM
        Common.ShowSetupForm();
#else
        if (Setting.Values.FirstRun && !Common.AtLeastOneSocketConnected())
        {
            MachineStuff.ShowSetupForm();
        }
        else
        {
            PowerToysTelemetry.Log.WriteEvent(new MouseWithoutBorders.Telemetry.MouseWithoutBordersOldUIOpenedEvent());

            if (Common.MatrixForm == null)
            {
                Common.MatrixForm = new FrmMatrix();
                Common.MatrixForm.Show();

                if (Common.MainForm != null)
                {
                    Common.MainForm.NotifyIcon.Visible = false;
                    Common.MainForm.NotifyIcon.Visible = Setting.Values.ShowOriginalUI;
                }
            }
            else
            {
                Common.MatrixForm.WindowState = FormWindowState.Normal;
                Common.MatrixForm.Activate();
            }
        }
#endif
    }

    internal static void SaveMachineMatrixToSettings()
    {
        Setting.Values.MachineMatrixString = string.Join(
            ",",
            Enumerable.Range(0, MAX_MACHINE).Select(i => MachineMatrix.GetHostname(i)));

        Common.DoSomethingInUIThread(() =>
        {
            Common.MainForm.ChangeIcon(-1);
            Common.MainForm.UpdateNotifyIcon();
        });
    }

    internal static void ReloadMachineMatrixFromSettings()
    {
        string s = Setting.Values.MachineMatrixString;
        if (!string.IsNullOrEmpty(s))
        {
            string[] parts = s.Split(',');
            if (parts.Length == MAX_MACHINE)
            {
                MachineMatrix.Initialize(parts);
                return;
            }
        }

        MachineMatrix.Initialize(Array.Empty<string>());
    }

    internal static void UpdateMachinePoolStringSetting()
    {
        Setting.Values.MachinePoolString = MachineService.SerializeMachinePoolSetting(MachineStuff.MachineMatrix);
    }

    internal static void SendMachineMatrix()
    {
        DATA package = new();

        for (int i = 0; i < MAX_MACHINE; i++)
        {
            package.MachineName = MachineMatrix.GetHostname(i);

            package.Type = PackageType.Matrix
                | (Setting.Values.MatrixCircle ? PackageType.MatrixSwapFlag : 0)
                | (Setting.Values.MatrixOneRow ? 0 : PackageType.MatrixTwoRowFlag);

            package.Src = (ID)(i + 1);
            package.Des = ID.ALL;

            Common.SkSend(package, null, false);

            Logger.LogDebug($"matrixIncludedMachine sent: [{i + 1}]:[{MachineMatrix.GetHostname(i)}]");
        }
    }

    internal static void UpdateMachineMatrix(DATA package)
    {
        uint i = (uint)package.Src;
        string matrixIncludedMachine = package.MachineName;

        if (i is > 0 and <= MAX_MACHINE)
        {
            Logger.LogDebug($"matrixIncludedMachine: [{i}]:[{matrixIncludedMachine}]");

            MachineMatrix.SetSlotHostname((int)(i - 1), matrixIncludedMachine);

            if (i == MAX_MACHINE)
            {
                Setting.Values.MatrixCircle = (package.Type & PackageType.MatrixSwapFlag) == PackageType.MatrixSwapFlag;
                Setting.Values.MatrixOneRow = !((package.Type & PackageType.MatrixTwoRowFlag) == PackageType.MatrixTwoRowFlag);
                SaveMachineMatrixToSettings();

                InitAndCleanup.ReopenSocketDueToReadError = true;

                UpdateClientSockets("UpdateMachineMatrix");

                Setting.Values.Changed = true;
            }
        }
        else
        {
            Logger.LogDebug("Invalid machine Matrix package!");
        }
    }

    internal static void SwitchToMachine(string name)
    {
        if (MachineStuff.MachineMatrix.TryGetEntryByHostname(name, out var switchEntry) && switchEntry!.Id != ID.NONE)
        {
            // Ask current machine to hide the Mouse cursor
            if (desMachineID != Common.MachineID)
            {
                Common.SendPackage(desMachineID, PackageType.HideMouse);
            }

            NewDesMachineID = Common.DesMachineID = switchEntry.Id;
            SwitchLocation.X = Event.XY_BY_PIXEL + primaryScreenBounds.Left + ((primaryScreenBounds.Right - primaryScreenBounds.Left) / 2);
            SwitchLocation.Y = Event.XY_BY_PIXEL + primaryScreenBounds.Top + ((primaryScreenBounds.Bottom - primaryScreenBounds.Top) / 2);
            SwitchLocation.ResetCount();
            Common.UpdateMultipleModeIconAndMenu();
            Common.HideMouseCursor(false);
            _ = Common.EvSwitch.Set();
        }
    }

    internal static void SwitchToMultipleMode(bool multipleMode, bool centerScreen)
    {
        if (multipleMode)
        {
            PowerToysTelemetry.Log.WriteEvent(new MouseWithoutBorders.Telemetry.MouseWithoutBordersMultipleModeEvent());
            NewDesMachineID = Common.DesMachineID = ID.ALL;
        }
        else
        {
            NewDesMachineID = Common.DesMachineID = Common.MachineID;
        }

        if (centerScreen)
        {
            Common.MoveMouseToCenter();
        }

        InitAndCleanup.ReleaseAllKeys();

        Common.UpdateMultipleModeIconAndMenu();
    }

    internal static bool CheckSecondInstance(bool sendMessage = false)
    {
        int h;

        if ((h = NativeMethods.FindWindow(null, Setting.Values.MyID)) > 0)
        {
            return true;
        }

        return false;
    }

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
    internal static EventWaitHandle oneInstanceCheck;
#pragma warning restore SA1307

    internal static void AssertOneInstancePerDesktopSession()
    {
        string eventName = $"Global\\{Application.ProductName}-{FrmAbout.AssemblyVersion}-{WinAPI.GetMyDesktop()}-{Common.CurrentProcess.SessionId}";
        oneInstanceCheck = new EventWaitHandle(false, EventResetMode.ManualReset, eventName, out bool created);

        if (!created)
        {
            Logger.TelemetryLogTrace($"Second instance found: {eventName}.", SeverityLevel.Warning, true);
            Common.CurrentProcess.KillProcess(true);
        }
    }

    internal static ID IdFromName(string name)
    {
        return MachineStuff.MachineMatrix.TryGetEntryByHostname(name, out var entry) ? entry!.Id : ID.NONE;
    }

    internal static string NameFromID(ID id)
    {
        MachineEntry entry = MachineStuff.MachineMatrix.GetEntryById(id);
        return string.IsNullOrEmpty(entry?.Hostname) ? null : entry.Hostname;
    }

    internal static void ClearComputerMatrix()
    {
        MachineStuff.MachineMatrix.Initialize(new string[] { Common.MachineName.Trim() });
        MachineStuff.SaveMachineMatrixToSettings();
        MachineStuff.UpdateMachinePoolStringSetting();
    }
}
