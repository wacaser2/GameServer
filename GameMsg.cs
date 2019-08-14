using System.IO;

using General;

namespace GameServer {
	public class GameMsg {
		public byte[] message;

		public GameMsg(byte[] msg) => message = msg;

		public GameMsg(byte type, params object[] objects) {
			using(MemoryStream m = new MemoryStream()) {
				using(BinaryWriter bw = new BinaryWriter(m)) {
					bw.Write(type);
					foreach(object o in objects) {
						if(o is ISendable s)
							s.ToSend(bw);
						else if(o is bool b)
							bw.Write(b);
						else if(o is byte by)
							bw.Write(by);
						else if(o is ushort us)
							bw.Write(us);
						else if(o is int i)
							bw.Write(i);
						else if(o is float f)
							bw.Write(f);
						else if(o is string ss)
							bw.Write(ss);
						else if(o is BinaryReader br) {
							bw.Write(br.ReadAllBytes());
						} else if(o is System.Action<BinaryWriter> sf)
							sf(bw);
					}
					bw.Flush();
					message = m.ToArray();
				}
			}
		}
	}
}
