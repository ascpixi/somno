using Somno.Evasion;
using Somno.Native;
using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Somno.UI;
using System.Threading;
using System.Reflection;
using System.Linq;
using Somno.Packager;
using System.IO;
using Somno.PortalAgent;
using System.Globalization;
using System.Collections.Generic;
using Somno.Native.WinUSER;
using Somno.Game.Modifications;

namespace Somno
{
    [SupportedOSPlatform("Windows")]
    internal class SomnoMain
    {
        static bool terminating = false;

        public static Portal? MainPortal { get; private set; }
        public static bool ConsoleVisible { get; private set; } = true;

        public static ESPModification? ESP;
        public static TrajectoriesModification? Trajectories;
        public static WaypointsModification? Waypoints;

        static void Main(string[] args)
        {
            //GenuineCheck.VerifyGenuine();
            SafetyCheck.CheckForIncompatibleSoftware();

            Kernel32.SetConsoleCtrlHandler(ConsoleExitHandler, true);
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Exit(false);

            Console.Title = "Somno";

            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.Clear();
            Terminal.Header("Somno Console v1.0.0", ConsoleColor.Black, ConsoleColor.DarkRed);

            SomnoOverlay.Instance = new();
            SomnoOverlay.Instance.Start().ContinueWith(
                GUIExceptionHandler,
                TaskContinuationOptions.OnlyOnFaulted
            );

            USNJournal.Erase(@"\\.\C:");
            USNJournal.Fill(1000);

            ConfigurationGUI.Create();

            ESP = new ESPModification();
            ConfigurationGUI.Instance!.Modules.Add(ESP);
            SomnoOverlay.Instance.Modules.Add(ESP);

            Trajectories = new TrajectoriesModification();
            ConfigurationGUI.Instance!.Modules.Add(Trajectories);
            SomnoOverlay.Instance.Modules.Add(Trajectories);

            Waypoints = new WaypointsModification();
            ConfigurationGUI.Instance!.Modules.Add(Waypoints);
            SomnoOverlay.Instance.Modules.Add(Waypoints);

            Terminal.LogInfo("Somno was successfully initialized.");

            HideConsole();
            MessagePump();

            //Portal.EstablishConnection("ConsoleApp22");
            //uint secret = Portal.ReadProcessMemory<uint>(0x7FFB8F10D380);

            //string? stdinBuffer;
            //ulong targetAddr;

            //do {
            //    Console.Write("Address to read: 0x");
            //    stdinBuffer = Console.ReadLine();
            //} while (
            //    stdinBuffer == null ||
            //    !ulong.TryParse(stdinBuffer, NumberStyles.HexNumber, null, out targetAddr)
            //);

            //string targetProcess;

            //while (true) {
            //    Console.Write("Target process name: ");
            //    stdinBuffer = Console.ReadLine();
            //    if (stdinBuffer == null)
            //        continue;

            //    targetProcess = stdinBuffer;
            //    break;
            //}

            //mainPortal = new Portal(targetProcess);
            //byte secret = mainPortal.ReadProcessMemory<byte>(targetAddr);
            //Console.WriteLine($"Done. Value = {secret} (0x{secret:X2})");

            //Console.WriteLine("Press any key to try again...");
            //Console.ReadLine();

            //byte secret2 = mainPortal.ReadProcessMemory<byte>(targetAddr);
            //Console.WriteLine($"Done. Value = {secret2} (0x{secret2:X2})");

            //Console.WriteLine("Press any key to close...");
            //Console.ReadLine();
            //mainPortal.Close();
            //Console.WriteLine("Closed.");
            //Console.ReadLine();

            //while (true) {
            //    Terminal.LogInfo(mainPortal.ToString());
            //    Thread.Sleep(500);
            //}
        }

        public static void HideConsole()
        {
            User32.ShowWindow(User32.GetConsoleWindow(), ShowWindowCommand.Hide);
            ConsoleVisible = false;
        }

        public static void ShowConsole()
        {
            User32.ShowWindow(User32.GetConsoleWindow(), ShowWindowCommand.Show);
            ConsoleVisible = true;
        }

        static void MessagePump()
        {
            while (!terminating) {
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
            if(terminating) return;
            terminating = true;

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
            } catch (Exception ex) {
                Terminal.LogError("Could not close the portal agent.");
                Terminal.LogError($"Exception: {ex.GetType().Name}: {ex.Message}");

                if (ex.StackTrace != null)
                    Terminal.LogError(ex.StackTrace);

                Console.ReadLine();
            }

            try {
                USNJournal.Erase(@"\\.\C:");
                USNJournal.Fill(1000);
            } catch (Exception ex) {
                Terminal.LogError("Could not clear the USN journal.");
                Terminal.LogError($"Exception: {ex.GetType().Name}: {ex.Message}");

                if (ex.StackTrace != null)
                    Terminal.LogError(ex.StackTrace);
            }

            Terminal.LogInfo(@"See you later! \(^ω^)");

            Thread.Sleep(2000);

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
}