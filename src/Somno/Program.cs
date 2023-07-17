using Somno.Evasion;
using Somno.Native;
using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Somno.UI;
using System.Threading;
using Lunar;
using System.Linq;
using Somno.Packager;
using System.IO;
using Somno.IPC;

namespace Somno
{
    [SupportedOSPlatform("Windows")]
    internal class Program
    {
        static Portal? mainPortal;
        static void Main(string[] args)
        {
            SafetyCheck.VerifyGenuine();
            Kernel32.SetConsoleCtrlHandler(ConsoleExitHandler, true);
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Exit(false);

            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.Clear();
            Terminal.Header("Somno Console v1.0.0", ConsoleColor.Black, ConsoleColor.DarkRed);

            //var gui = new ConfigurationGUI();
            //_ = gui.Start().ContinueWith(
            //    x => {
            //        Terminal.LogError($"A {x.Exception!.GetType().Name} has been thrown in the GUI task.");
            //        foreach (var line in x.Exception.Message.Split("\n")) {
            //            Terminal.LogError($"\t{line}");
            //        }
            //    },
            //    TaskContinuationOptions.OnlyOnFaulted
            //);


            //Portal.EstablishConnection("ConsoleApp22");
            //uint secret = Portal.ReadProcessMemory<uint>(0x7FFB8F10D380);
            mainPortal = new Portal("notepad");
            //uint secret = portal.ReadProcessMemory<uint>(0x7FFDFBEAD380);
            //Terminal.LogInfo($"The secret is {secret}!");

            while (true) {
                Thread.Sleep(500);
                Console.WriteLine("I'm still running!");
            }
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