using Somno.Evasion;
using Somno.Native;
using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Somno.UI;
using System.Threading;

namespace Somno
{
    [SupportedOSPlatform("Windows")]
    internal class Program
    {

        static void Main(string[] args)
        {
            SafetyCheck.VerifyGenuine();
            Kernel32.SetConsoleCtrlHandler(ConsoleExitHandler, true);
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Exit(false);

            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.Clear();
            Terminal.Header("Somno Console v1.0.0", ConsoleColor.Black, ConsoleColor.DarkRed);

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

            while(true) {
                Thread.Sleep(500);
                Console.WriteLine("I'm still running!");
            }

            //HandleHijack.FindMemoryHandle("");

            //Console.WriteLine("Aimbot is on!");
            //Portal.Inject(20112);
            ////Terminal.LogInfo("Exited from Inject.");
            //var r = Portal.GetProcessHandlePID(0x32);
            //Terminal.LogInfo($"Got {r} for a non-existed handle, but still a response!");
        }

        static bool terminating = false;
        static void Exit(bool shouldManuallyTerminate)
        {
            if(terminating) return;
            terminating = true;

            Portal.Close();
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