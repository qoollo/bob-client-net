using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.BobClient
{
    public class BobClusterClient: IBobApi, IDisposable
    {
        private readonly BobNodeClient[] _clients;
        private readonly BobNodeSelectionPolicy _selectionPolicy;

        public BobClusterClient(IEnumerable<BobNodeClient> clients, BobNodeSelectionPolicy selectionPolicy)
        {
            if (clients == null)
                throw new ArgumentNullException(nameof(clients));

            _clients = clients.ToArray();
            _selectionPolicy = selectionPolicy;

            if (selectionPolicy == null)
            {
                if (_clients.Length == 1)
                    _selectionPolicy = FirstNodeSelectionPolicy.Instance;
                else
                    _selectionPolicy = new SequentialNodeSelectionPolicy();
            }

            if (_clients.Length == 0)
                throw new ArgumentException("Clients list cannot be empty", nameof(clients));
            if (_clients.Any(o => o == null))
                throw new ArgumentException("Client inside clients array cannot be null", nameof(clients));
        }
        public BobClusterClient(IEnumerable<BobNodeClient> clients)
            : this(clients, new SequentialNodeSelectionPolicy())
        {
        }
        public BobClusterClient(IEnumerable<string> nodeAddress, TimeSpan operationTimeout, BobNodeSelectionPolicy selectionPolicy)
            : this(nodeAddress.Select(o => new BobNodeClient(o, operationTimeout)).ToList(), selectionPolicy)
        {
        }
        public BobClusterClient(IEnumerable<string> nodeAddress)
            : this(nodeAddress, Timeout.InfiniteTimeSpan, new SequentialNodeSelectionPolicy())
        {
        }


        public Task OpenAsync(TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            Task[] openTasks = new Task[_clients.Length];
            for (int i = 0; i < _clients.Length; i++)
                openTasks[i] = _clients[i].OpenAsync(timeout);

            return Task.WhenAll(openTasks);
        }
        public Task OpenAsync()
        {
            return OpenAsync(Timeout.InfiniteTimeSpan);
        }
        public void Open(TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            List<Exception> exs = new List<Exception>();
            for (int i = 0; i < _clients.Length; i++)
            {
                try
                {
                    _clients[i].Open(timeout);
                }
                catch (Exception e)
                {
                    exs.Add(e);
                }
            }

            if (exs.Count > 0)
                throw new AggregateException(exs);
        }
        public void Open()
        {
            Open(Timeout.InfiniteTimeSpan);
        }


        public Task CloseAsync()
        {
            Task[] openTasks = new Task[_clients.Length];
            for (int i = 0; i < _clients.Length; i++)
                openTasks[i] = _clients[i].CloseAsync();

            return Task.WhenAll(openTasks);
        }
        public void Close()
        {
            List<Exception> exs = new List<Exception>();
            for (int i = 0; i < _clients.Length; i++)
            {
                try
                {
                    _clients[i].Close();
                }
                catch (Exception e)
                {
                    exs.Add(e);
                }
            }

            if (exs.Count > 0)
                throw new AggregateException(exs);
        }


        public BobResult Put(ulong key, byte[] data, CancellationToken token)
        {
            var client = _selectionPolicy.Select(_clients);
            return client.Put(key, data, token);
        }

        public BobResult Put(ulong key, byte[] data)
        {
            var client = _selectionPolicy.Select(_clients);
            return client.Put(key, data);
        }

        public Task<BobResult> PutAsync(ulong key, byte[] data, CancellationToken token)
        {
            var client = _selectionPolicy.Select(_clients);
            return client.PutAsync(key, data, token);
        }

        public Task<BobResult> PutAsync(ulong key, byte[] data)
        {
            var client = _selectionPolicy.Select(_clients);
            return client.PutAsync(key, data);
        }


        public BobGetResult Get(ulong key, bool fullGet, CancellationToken token)
        {
            var client = _selectionPolicy.Select(_clients);
            return client.Get(key, fullGet, token);
        }

        public BobGetResult Get(ulong key, CancellationToken token)
        {
            var client = _selectionPolicy.Select(_clients);
            return client.Get(key, token);
        }

        public BobGetResult Get(ulong key, bool fullGet)
        {
            var client = _selectionPolicy.Select(_clients);
            return client.Get(key, fullGet);
        }

        public BobGetResult Get(ulong key)
        {
            var client = _selectionPolicy.Select(_clients);
            return client.Get(key);
        }

        public Task<BobGetResult> GetAsync(ulong key, bool fullGet, CancellationToken token)
        {
            var client = _selectionPolicy.Select(_clients);
            return client.GetAsync(key, fullGet, token);
        }

        public Task<BobGetResult> GetAsync(ulong key, CancellationToken token)
        {
            var client = _selectionPolicy.Select(_clients);
            return client.GetAsync(key, token);
        }

        public Task<BobGetResult> GetAsync(ulong key, bool fullGet)
        {
            var client = _selectionPolicy.Select(_clients);
            return client.GetAsync(key, fullGet);
        }

        public Task<BobGetResult> GetAsync(ulong key)
        {
            var client = _selectionPolicy.Select(_clients);
            return client.GetAsync(key);
        }



        protected virtual void Dispose(bool isUserCall)
        {
            try
            {
                this.Close();
            }
            catch { }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
