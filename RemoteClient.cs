using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class RemoteClient : GameClient {
    private IPEndPoint _ipe;
    private TcpClient _client;
    private NetworkStream _stream;
    private CancellationTokenSource _cts;
    private CancellationToken _token;
    private Queue<GameMsg> _sendQueue = new Queue<GameMsg>();
    private Task _reading, _sending;
    private bool _recon;

    public RemoteClient(IPAddress ip, int port = 44444, Dictionary<GameMsgType, List<Action<GameClient, BinaryReader>>> handlers = null) : base(handlers) {
        _ipe = new IPEndPoint(ip, port);
        _client = new TcpClient();
        _client.Connect(_ipe);
        RegisterInitialHandlers();
        Init();
        _recon = true;
    }

    public RemoteClient(TcpClient client, Dictionary<GameMsgType, List<Action<GameClient, BinaryReader>>> handlers = null) : base(handlers) {
        _ipe = (IPEndPoint)client.Client.RemoteEndPoint;
        _client = client;
        Init();
        _recon = false;
    }

    private void Init() {
        _stream = _client.GetStream();
        _cts = new CancellationTokenSource();
        _token = _cts.Token;
        _token.Register(Close);
        _reading = ReadingAsync();
        _sending = SendingAsync();
        _reading.ConfigureAwait(false);
        _sending.ConfigureAwait(false);
    }

    private async Task SendingAsync() {
        while(!_token.IsCancellationRequested) {
            try {
                if(_sendQueue.Count > 0) {
                    GameMsg msg;
                    lock(_sendQueue)
                        msg = _sendQueue.Dequeue();
                    byte[] msgLen = BitConverter.GetBytes(msg.message.Length);
                    await _stream.WriteAsync(msgLen, 0, 4, _token).ConfigureAwait(false);
                    await _stream.WriteAsync(msg.message, 0, msg.message.Length, _token).ConfigureAwait(false);
                    await _stream.FlushAsync(_token).ConfigureAwait(false);
                    Debug.Log("Message sent");
                } else {
                    await Task.Delay(100, _token).ConfigureAwait(false);
                }
            }
            catch(TaskCanceledException e) {
                Debug.Log("Send cancelled: " + e.Source);
            }
            catch(Exception e) {
                Debug.Log("Problem sending: " + e.ToString());
                _cts.Cancel();
            }
        }
    }

    private async Task ReadingAsync() {
        while(!_token.IsCancellationRequested) {
            try {
                if(_stream.DataAvailable) {
                    byte[] msgLenBuf = new byte[4];
                    int bytesRead = 0, msgLen;
                    while(bytesRead < 4)
                        bytesRead += await _stream.ReadAsync(msgLenBuf, bytesRead, 4 - bytesRead, _token).ConfigureAwait(false);
                    msgLen = BitConverter.ToInt32(msgLenBuf, 0);
                    bytesRead = 0;
                    byte[] msg = new byte[msgLen];
                    while(bytesRead < msgLen) {
                        bytesRead += await _stream.ReadAsync(msg, bytesRead, msgLen - bytesRead, _token).ConfigureAwait(false);
                        if(msgLen > 1024) {
                            SyncContext.RunOnUnityThread(() => {
                                Receive(new GameMsg(GameMsgType.MsgProgress, bytesRead, msgLen));
                            });
                        }
                    }
                    SyncContext.RunOnUnityThread(() => {
                        Receive(new GameMsg(msg));
                    });
                    Debug.Log("Message received");
                } else {
                    await Task.Delay(100, _token).ConfigureAwait(false);
                }
            }
            catch(TaskCanceledException e) {
                Debug.Log("Read cancelled " + e.Source);
            }
            catch(Exception e) {
                Debug.Log("Problem reading: " + e.ToString());
                _cts.Cancel();
            }
        }

    }

    internal override void RegisterInitialHandlers() {
        base.RegisterInitialHandlers();
        RegisterMsgHandler(GameMsgType.Disconnect, (c, br) => {
            _recon = false;
            _cts.Cancel();
        });
    }

    public override void Disconnect() {
        lock(_sendQueue)
            _sendQueue.Clear();
        Send(new GameMsg(GameMsgType.Disconnect));
        _recon = false;
        Task sentDisconnect = Task.Run(async () => {
            while(_sendQueue.Count != 0)
                await Task.Delay(10).ConfigureAwait(false);
        });
        sentDisconnect.ConfigureAwait(false);
        try {
            if(sentDisconnect.Wait(1000)) {
                Debug.Log("Client sent disconnect");
            } else {
                Debug.Log("Sending disconnect timed out");
            }
        }
        catch(Exception e) {
            Debug.Log(e.ToString());
        }

        _cts.Cancel();
    }

    public override void Close() {
        if(!_cts.IsCancellationRequested) {
            _cts.Cancel();
            return;
        }
        try {
            if(Task.WaitAll(new Task[] { _sending, _reading }, 1000)) {
                Debug.Log("Client successfully ended send and read");
            } else {
                Debug.Log("Client close timed out");
            }
        }
        catch(Exception e) {
            Debug.Log(e.ToString());
        }
        lock(_sendQueue)
            _sendQueue.Clear();
        _stream.Close();
        _client.Close();
        if(_recon)
            Reconnect();
    }

    public override void Send(GameMsg msg) {
        lock(_sendQueue)
            _sendQueue.Enqueue(msg);
    }

    public void Reconnect() {
        try {
            _client = new TcpClient();
            _client.Connect(_ipe);
            Init();
            Send(new GameMsg(GameMsgType.Reconnect, id));
        }
        catch(Exception e) {
            Debug.Log(e.ToString());
        }
    }
}