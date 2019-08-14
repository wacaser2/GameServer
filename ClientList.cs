using System.Collections;
using System.Collections.Generic;

namespace GameServer {
	public class ClientList : IEnumerable<KeyValuePair<GameClient, int>> {
		private Dictionary<int, GameClient> _clients;
		private Dictionary<GameClient, int> _clientIdxs;

		public ClientList() {
			_clients = new Dictionary<int, GameClient>();
			_clientIdxs = new Dictionary<GameClient, int>();
		}

		public int Count => _clients.Count;

		public int this[GameClient client] {
			get { return _clientIdxs[client]; }
			set { Add(client, value); }
		}

		public GameClient this[int clientIdx] {
			get { return _clients[clientIdx]; }
			set { Add(value, clientIdx); }
		}

		public bool ContainsKey(GameClient client) {
			return _clientIdxs.ContainsKey(client);
		}

		public bool ContainsKey(int idx) {
			return _clients.ContainsKey(idx);
		}

		public void Add(GameClient client, int idx) {
			_clients[idx] = client;
			_clientIdxs[client] = idx;
		}

		public void Remove(GameClient client) {
			if(!_clientIdxs.ContainsKey(client))
				return;
			else {
				_clients.Remove(_clientIdxs[client]);
				_clientIdxs.Remove(client);
			}
		}

		public void Remove(int idx) {
			if(!_clients.ContainsKey(idx))
				return;
			else {
				_clientIdxs.Remove(_clients[idx]);
				_clients.Remove(idx);
			}
		}

		public void Clear() {
			_clients.Clear();
			_clientIdxs.Clear();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return ((IEnumerable<KeyValuePair<GameClient, int>>)_clientIdxs).GetEnumerator();
		}

		public IEnumerator<KeyValuePair<GameClient, int>> GetEnumerator() {
			return ((IEnumerable<KeyValuePair<GameClient, int>>)_clientIdxs).GetEnumerator();
		}
	}
}