using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class LocalClient : GameClient {
    private LocalClient _pair;

    public LocalClient(LocalClient pair) {
        _pair = pair;
        pair._pair = this;
    }

    public LocalClient(Dictionary<GameMsgType, List<Action<GameClient, BinaryReader>>> handlers = null) : base(handlers) {
    }

    public override void Close() {
        _pair = null;
    }

    public override void Send(GameMsg msg) {
        SyncContext.RunOnUnityThread(() => {
            if(_pair != null)
                _pair.Receive(msg);
        });
    }
}