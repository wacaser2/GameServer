using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class BroadcastServer {
    private static readonly int _port = 40404;
    private static UdpClient _client;
    private static Task _sendTask;
    private static Task _listenTask;
    private static CancellationTokenSource _cts;
    private static CancellationToken _token;

    public static Dictionary<IPAddress, string> broadcastMessages = new Dictionary<IPAddress, string>();

    public static bool started => _sendTask != null || _listenTask != null;

    static BroadcastServer() {
    }

    private static void Init() {
        if(started)
            return;
        _client = new UdpClient(_port) {
            EnableBroadcast = true
        };
        _cts = new CancellationTokenSource();
        _token = _cts.Token;
    }

    public static void StartHost() {
        Init();
        IPEndPoint _ipe = new IPEndPoint(IPAddress.Broadcast, _port);
        byte[] buf = Encoding.ASCII.GetBytes("New Game");
        _sendTask = Task.Run(async () => {
            while(!_token.IsCancellationRequested) {
                try {
                    _client.Send(buf, buf.Length, _ipe);
                    Debug.Log("Sent broadcast");
                    await Task.Delay(1000);
                }
                catch(Exception e) {
                    Debug.Log(e.ToString());
                    _client.Close();
                    _client = new UdpClient(_port) {
                        EnableBroadcast = true
                    };
                }
            }
        });
    }

    public static void StartClient() {
        Init();
        _listenTask = Task.Run(async () => {
            while(!_token.IsCancellationRequested) {
                try {
                    var response = await _client.ReceiveAsync().WithCancellation(_token);
                    IPAddress host = response.RemoteEndPoint.Address;
                    string msg = Encoding.ASCII.GetString(response.Buffer);
                    if(!broadcastMessages.ContainsKey(host) || broadcastMessages[host] == msg) {
                        broadcastMessages[host] = msg;
                        SyncContext.RunOnUnityThread(() => {
                            RM.ui.menuUI.startMenu.connUI.RefreshConns();
                        });
                    }
                    Debug.Log(Encoding.ASCII.GetString(response.Buffer) + " : " + response.RemoteEndPoint.ToString());
                }
                catch(Exception e) {
                    Debug.Log(e.ToString());
                    _client.Close();
                    _client = new UdpClient(_port) {
                        EnableBroadcast = true
                    };
                }
            }
        });
    }

    public static void Stop() {
        if(started) {
            _cts.Cancel();
            try {
                if(_listenTask != null)
                    _listenTask.Wait(1000);
                if(_sendTask != null)
                    _sendTask.Wait(1000);
            }
            catch(Exception e) {
                Debug.Log(e.ToString());
            }
        }
        _sendTask = null;
        _listenTask = null;
        if(_client != null) {
            _client.Close();
        }
        _client = null;
        broadcastMessages.Clear();
    }
}
