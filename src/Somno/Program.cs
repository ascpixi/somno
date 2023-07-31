using Somno.Evasion;
using Somno.Native;
using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Somno.UI;
using System.Threading;
using System.Linq;
using Somno.Packager;
using System.IO;
using Somno.PortalAgent;
using System.Globalization;
using Somno.Native.WinUSER;

namespace Somno
{
    [SupportedOSPlatform("Windows")]
    internal class Program
    {
        static Portal? mainPortal;
        static void Main(string[] args)
        {
            //GenuineCheck.VerifyGenuine();
            SafetyCheck.CheckForIncompatibleSoftware();

            Kernel32.SetConsoleCtrlHandler(ConsoleExitHandler, true);
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Exit(false);

            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.Clear();
            Terminal.Header("Somno Console v1.0.0", ConsoleColor.Black, ConsoleColor.DarkRed);


            //Console.ReadLine();
            //host.Dispose();
            //proc.Kill();

            //Console.WriteLine("bye");

            var gui = new ConfigurationGUI();
            _ = gui.Start().ContinueWith(
                x => {
                    Terminal.LogError($"A {x.Exception!.GetType().Name} has been thrown in the GUI task.");
                    foreach (var line in x.Exception.Message.Split("\n")) {
                        Terminal.LogError($"\t{line}");
                    }
                },
                TaskContinuationOptions.OnlyOnFaulted
            );
        }

        static bool terminating = false;
        static void Exit(bool shouldManuallyTerminate)
        {
            if(terminating) return;
            terminating = true;

            mainPortal?.Close();
            Terminal.LogInfo(@"See you later! \(^ω^)");

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