using Somno.Evasion;
using Somno.Native;
using System;
using System.Threading.Tasks;
using Somno.UI;
using System.IO;
using Somno.PortalAgent;
using Somno.Native.WinUSER;
using Somno.Game.Modifications;
using Somno.Game;
using System.Runtime.CompilerServices;

namespace Somno;

internal class SomnoMain
{
    public static Portal? MainPortal;
    public static bool ConsoleVisible { get; private set; } = true;

    public const string TargetProcessName = "csgo.exe";

    static void RegisterAllModifications()
    {
        ModEngine.AddModification(new ESPModification());
        ModEngine.AddModification(new WaypointsModification());
        ModEngine.AddModification(new ExtraCrosshairModification());
        ModEngine.AddModification(new RecoilCrosshairModification());
        ModEngine.AddModification(new GameInfoModification());
        ModEngine.AddModification(new RearviewModification());
    }

    static void Main()
    {
        GenuineCheck.VerifyGenuine();
        SafetyCheck.CheckForIncompatibleSoftware();

        Kernel32.SetConsoleCtrlHandler(ConsoleExitHandler, true);
        AppDomain.CurrentDomain.ProcessExit += (s, e) => Exit(false);

        Console.Title = "Somno";
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (!Directory.Exists("config")) {
            var dir = Directory.CreateDirectory("config");
            dir.Attributes |= FileAttributes.NotContentIndexed;
        }

        Console.Clear();
        Terminal.Header("Somno Console v1.0.0", ConsoleColor.Black, ConsoleColor.DarkRed);

        Terminal.LogInfo("Establishing portal connection.");
        MainPortal = new Portal();

        Terminal.LogInfo("Starting overlay...");
        SomnoOverlay.Instance = new();
        SomnoOverlay.Instance.Start().ContinueWith(
            GUIExceptionHandler,
            TaskContinuationOptions.OnlyOnFaulted
        );

#if !DEBUG
        Terminal.LogInfo("Cleaning the USN journal...");
        USNJournal.Erase(@"\\.\C:");
        USNJournal.Fill(1000);
#endif

        ConfigurationGUI.Create();

        Terminal.LogInfo("Somno was successfully initialized.");

        Orchestrator.Start();

        RegisterAllModifications();

        HideConsole();
        MessagePump();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void HideConsole()
    {
        User32.ShowWindow(User32.GetConsoleWindow(), ShowWindowCommand.Hide);
        ConsoleVisible = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ShowConsole()
    {
        User32.ShowWindow(User32.GetConsoleWindow(), ShowWindowCommand.Show);
        ConsoleVisible = true;
    }

    static void MessagePump()
    {
        while (!Orchestrator.IsTerminating) {
            var result = User32.GetMessage(out var message, IntPtr.Zero, 0, 0);
            
            if (result <= 0) {
                break;
            }
            
            User32.TranslateMessage(ref message);
            User32.DispatchMessage(ref message);
        }
    }

    static void GUIExceptionHandler(Task x)
    {
        ShowConsole();

        if (x.Exception == null) {
            return;
        }

        Terminal.LogError($"A {x.Exception.GetType().Name} has been thrown in the GUI task.");
        foreach (var line in x.Exception.Message.Split("\n")) {
            Terminal.LogError($"\t{line}");
        }
    }

    /// <summary>
    /// Exits and cleans up all resources.
    /// </summary>
    public static void Exit() => Exit(true);

    static void Exit(bool shouldManuallyTerminate)
    {
        if(Orchestrator.IsTerminating)
            return;

        Orchestrator.ChangeState(EngineState.Terminating);

        ShowConsole();

        try {
            SomnoOverlay.Instance?.Close();
            SomnoOverlay.Instance?.Dispose();
        }
        catch (Exception ex) {
            Terminal.LogError("Could not dispose GUI resources.");
            Terminal.LogError($"Exception: {ex.GetType().Name}: {ex.Message}");

            if (ex.StackTrace != null)
                Terminal.LogError(ex.StackTrace);

            Console.ReadLine();
        }

        try {
            MainPortal?.Close();
        }
        catch (Exception ex) {
            Terminal.LogError("Could not close the portal agent.");
            Terminal.LogError($"Exception: {ex.GetType().Name}: {ex.Message}");

            if (ex.StackTrace != null)
                Terminal.LogError(ex.StackTrace);

            Console.ReadLine();
        }

#if !DEBUG
        try {
            USNJournal.Erase(@"\\.\C:");
            USNJournal.Fill(1000);
        } catch (Exception ex) {
            Terminal.LogError("Could not clear the USN journal.");
            Terminal.LogError($"Exception: {ex.GetType().Name}: {ex.Message}");

            if (ex.StackTrace != null)
                Terminal.LogError(ex.StackTrace);
        }
#endif

        Terminal.LogInfo(@"See you later! \(^ω^)");

        Orchestrator.ChangeState(EngineState.Terminated);

#if !DEBUG
        Thread.Sleep(1000);
#endif

        if(shouldManuallyTerminate) {
            Environment.Exit(0);
        }
    }
    
    static bool ConsoleExitHandler(Kernel32.CtrlType signal)
    {
        switch (signal) {
            case Kernel32.CtrlType.CtrlBreakEvent:
            case Kernel32.CtrlType.CtrlCEvent:
            case Kernel32.CtrlType.CtrlLogoffEvent:
            case Kernel32.CtrlType.CtrlShutdownEvent:
            case Kernel32.CtrlType.CtrlCloseEvent:
                Exit(true);
                return false;
            default:
                return false;
        }
    }
}