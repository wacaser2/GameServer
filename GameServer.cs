using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class GameServer {
    private static CancellationTokenSource _cancellationTokenSource;
    private static CancellationToken _token;
    private static TcpListener _socket;
    private static Task _listenTask;
    private static ClientList _clients;
    private static int _clientIdx;

    private static Dictionary<GameMsgType, List<Action<GameClient, BinaryReader>>> _handlers;

    public static ClientList Clients => _clients;

    public static bool started => _listenTask != null;
    public static int clientCount => _clients.Count;

    static GameServer() {
        _clients = new ClientList();
        _handlers = new Dictionary<GameMsgType, List<Action<GameClient, BinaryReader>>>();
        RegisterInitialHandlers();
    }

    private static void RegisterInitialHandlers() {
        RegisterMsgHandler(GameMsgType.Connect, ConnectHandler);
        RegisterMsgHandler(GameMsgType.Reconnect, ReconnectHandler);
        RegisterMsgHandler(GameMsgType.Disconnect, DisconnectHandler);
    }

    private static void ConnectHandler(GameClient client, BinaryReader br) {
        //send client id
        client.Send(new GameMsg(GameMsgType.Connect, client.id));
    }

    private static void ReconnectHandler(GameClient client, BinaryReader br) {
        int reconID = br.ReadInt32();
        if(_clients.ContainsKey(reconID)) {
            _clients[reconID].Disconnect();
            _clients.Remove(reconID);
        }
        //give client back old id
        _clients[client] = reconID;
        client.id = reconID;
        client.Send(new GameMsg(GameMsgType.Reconnect, client.id));
    }

    private static void DisconnectHandler(GameClient client, BinaryReader br) {
        client.Close();
        _clients.Remove(client);
    }

    public static void Start(int port = 44444) {
        if(started) {
            Debug.Log("Server already started");
            return;
        }
        _cancellationTokenSource = new CancellationTokenSource();
        _token = _cancellationTokenSource.Token;

        _socket = new TcpListener(GetLocalIPAddress(), port);
        _socket.Start();
        _listenTask = CheckForClientsAsync(_token);
        _listenTask.ConfigureAwait(false);
        Debug.Log("Server started");
    }

    private static async Task CheckForClientsAsync(CancellationToken token) {
        while(!token.IsCancellationRequested) {
            if(_socket.Pending()) {
                GameClient client = new RemoteClient(await _socket.AcceptTcpClientAsync().ConfigureAwait(false), _handlers);
                AddClient(client);
            } else {
                await Task.Delay(10).ConfigureAwait(false);
            }
        }
        Debug.Log("Stopped listening");
    }

    public static void Stop() {
        if(started) {
            _cancellationTokenSource.Cancel();
            try {
                if(!Task.WaitAll(new Task[] { _listenTask }, 1000)) {
                    Debug.Log("Timed out");
                } else {
                    //completed: same as task incomplete exception
                }
            }
            catch(AggregateException ae) {
                foreach(var e in ae.InnerExceptions)
                    Debug.Log(e.ToString());
            }
            _socket.Stop();
            _listenTask = null;
            Debug.Log("Server stopped");
        }
        foreach(var client in _clients) {
            client.Key.Disconnect();
        }
        ClearHandlers();
        _clients.Clear();
        _clientIdx = 0;
    }

    public static void SendAll(GameMsg msg) {
        foreach(var client in _clients) {
            client.Key.Send(msg);
        }
    }

    public static IPAddress GetLocalIPAddress() {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach(var ip in host.AddressList) {
            if(ip.AddressFamily == AddressFamily.InterNetwork) {
                return ip;
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }

    private static void AddClient(GameClient client) {
        client.id = _clientIdx;
        _clients[client] = _clientIdx++;
    }

    public static GameClient GetLocalClient() {
        LocalClient serverClient = new LocalClient(_handlers);
        GameClient client = new LocalClient(serverClient);
        AddClient(serverClient);
        return client;
    }

    public static void RegisterMsgHandler(GameMsgType type, Action<GameClient, BinaryReader> handler) {
        if(!_handlers.ContainsKey(type)) {
            _handlers[type] = new List<Action<GameClient, BinaryReader>> { handler };
        } else {
            _handlers[type].Add(handler);
        }
    }

    public static void ClearHandlers() {
        _handlers.Clear();
        RegisterInitialHandlers();
    }
}
