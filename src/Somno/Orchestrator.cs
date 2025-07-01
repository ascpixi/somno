using Somno.Game;
using System;
using System.Threading;

namespace Somno;

/// <summary>
/// Coordinates the logical state of the Somno game modification engine.
/// </summary>
internal static class Orchestrator
{
    readonly static AutoResetEvent orchestratorNotify = new(false);

    public static bool Initialized { get; private set; } = false;
    public static Thread MainThread { get; private set; } = null!;
    public static EngineState State { get; private set; } = EngineState.NotInitialized;

    public static bool IsTerminating => State >= EngineState.Terminating;

    public static void Start()
    {
        MainThread = new Thread(OrchestratorThread);
        MainThread.Start();
    }

    public static void ChangeState(EngineState newState)
    {
        State = newState;
        orchestratorNotify.Set();
    }

    static void OrchestratorThread()
    {
        State = EngineState.WaitingForProcess;

        while (!IsTerminating) {
            switch (State) {
                case EngineState.WaitingForProcess:
                    Terminal.LogInfo($"Waiting for process '{SomnoMain.TargetProcessName}'...");
                    int pid;
                    do {
                        Thread.Sleep(500);
                    } while (!ProcessQuery.TryGetPIDByName(SomnoMain.TargetProcessName, out pid));
                    
                    if(SomnoMain.MainPortal == null)
                        throw new NullReferenceException("The main portal agent reference was null.");

                    GameManager.ChangeProcessPID(pid);
                    SomnoMain.MainPortal.TargetProcessPID = pid;
                    MemoryManager.ReinitializeModules(SomnoMain.MainPortal);

                    Terminal.LogInfo($"Process acquired, PID = {pid}.");
                    State = EngineState.Running;
                    break;
                case EngineState.Running:
                    orchestratorNotify.WaitOne();
                    break;
            }
        }
    }
}
