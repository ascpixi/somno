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