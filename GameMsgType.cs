public enum GameMsgType : byte {
    Connect,
    Reconnect,
    Disconnect,
    MsgProgress,
    ClientList,
    AddPlayer,
    RemovePlayer,
    PlayerName,
    PlayerColor,
    InitProgress,
    StartGame,
    StartTurn
}
