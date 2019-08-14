namespace GameServer {
	public enum MsgType : byte {
		Connect = 0,
		Reconnect,
		Disconnect,
		MsgProgress,
		Highest
	}
}
