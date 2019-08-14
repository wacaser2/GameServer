using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace GameServer {
	public abstract class GameClient {
		private Dictionary<byte, List<Action<GameClient, BinaryReader>>> _handlers;
		private int _id;

		public int id { get => _id; set => _id = value; }

		public GameClient(Dictionary<byte, List<Action<GameClient, BinaryReader>>> handlers = null) {
			if(handlers == null)
				_handlers = new Dictionary<byte, List<Action<GameClient, BinaryReader>>>();
			else
				_handlers = handlers;
		}

		internal virtual void RegisterInitialHandlers() {
			RegisterMsgHandler((byte)MsgType.Connect, (c, br) => {
				_id = br.ReadInt32();
			});
		}

		public void Connect() {
			Send(new GameMsg((byte)MsgType.Connect, SystemInfo.deviceName));
		}

		public abstract void Send(GameMsg msg);

		public virtual void Disconnect() {
			Send(new GameMsg((byte)MsgType.Disconnect));
			Close();
		}

		public abstract void Close();

		public void RegisterMsgHandler(byte type, Action<GameClient, BinaryReader> handler) {
			if(!_handlers.ContainsKey(type)) {
				_handlers[type] = new List<Action<GameClient, BinaryReader>> { handler };
			} else {
				_handlers[type].Add(handler);
			}
		}

		public void Receive(GameMsg msg) {
			using(MemoryStream m = new MemoryStream(msg.message)) {
				m.Seek(0, SeekOrigin.Begin);
				byte type = (byte)m.ReadByte();
				Debug.Log("Received " + type.ToString() + " message");
				if(!_handlers.ContainsKey(type))
					return;
				foreach(var h in _handlers[type]) {
					m.Seek(1, SeekOrigin.Begin);
					using(BinaryReader br = new BinaryReader(m, System.Text.Encoding.Default, true))
						h(this, br);
				}
			}
		}
	}
}