using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SyncContext : MonoBehaviour {
    static public Queue<Action> runInUpdate= new Queue<Action>();

    public static void RunOnUnityThread(Action action) {
        // is this right?
        lock(runInUpdate) {
            runInUpdate.Enqueue(action);
        }
    }

    private void Update() {
        while(runInUpdate.Count > 0) {
            Action action = null;
            lock(runInUpdate) {
                if(runInUpdate.Count > 0)
                    action = runInUpdate.Dequeue();
            }
            action?.Invoke();
        }

    }
}