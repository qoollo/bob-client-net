using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.BobClient.RestApi
{
    internal class BobRestApiClient : IDisposable
    {
        private readonly HttpClient _client;

        public BobRestApiClient(Uri address, TimeSpan timeout)
        {
            _client = new HttpClient()
            {
                BaseAddress = address,
                Timeout = timeout
            };
        }
        public BobRestApiClient(Uri address)
            : this(address, TimeSpan.FromSeconds(60))
        {
        }

        public Uri Address { get { return _client.BaseAddress; } }
        public TimeSpan Timeout { get { return _client.Timeout; } }


        public async Task GetKeyDistributionFunction(CancellationToken token = default(CancellationToken))
        {
            using (var response = await _client.GetAsync("metadata/distrfunc", token))
            {
                // {"func":"Mod"}
                //return await ParseResponse(response, async resp => parse(await resp.Content.ReadAsStringAsync()));
            }
        }

        public async Task GetNodes(CancellationToken token = default(CancellationToken))
        {
            using (var response = await _client.GetAsync("nodes", token))
            {
                /*
 [
  {
    "name": "local_node_1",
    "address": "10.5.7.195:20000",
    "vdisks": [
      {
        "id": 0,
        "replicas": [
          {
            "node": "local_node_1",
            "disk": "disk1",
            "path": "/opt/disk1"
          }
        ]
      },
      {
        "id": 1,
        "replicas": [
          {
            "node": "local_node_1",
            "disk": "disk2",
            "path": "/opt/disk2"
          }
        ]
      },
      {
        "id": 3,
        "replicas": [
          {
            "node": "local_node_1",
            "disk": "disk4",
            "path": "/opt/disk4"
          }
        ]
      },
      {
        "id": 2,
        "replicas": [
          {
            "node": "local_node_1",
            "disk": "disk3",
            "path": "/opt/disk3"
          }
        ]
      }
    ]
  },
  {
    "name": "local_node_3",
    "address": "10.5.7.217:20000",
    "vdisks": []
  },
  {
    "name": "local_node_2",
    "address": "10.5.7.241:20000",
    "vdisks": [
      {
        "id": 0,
        "replicas": [
          {
            "node": "local_node_2",
            "disk": "disk1",
            "path": "/opt/disk1"
          }
        ]
      },
      {
        "id": 1,
        "replicas": [
          {
            "node": "local_node_2",
            "disk": "disk2",
            "path": "/opt/disk2"
          }
        ]
      },
      {
        "id": 3,
        "replicas": [
          {
            "node": "local_node_2",
            "disk": "disk4",
            "path": "/opt/disk4"
          }
        ]
      },
      {
        "id": 2,
        "replicas": [
          {
            "node": "local_node_2",
            "disk": "disk3",
            "path": "/opt/disk3"
          }
        ]
      }
    ]
  },
  {
    "name": "local_node_4",
    "address": "10.5.7.212:20000",
    "vdisks": []
  }
]
                */

                //return await ParseResponse(response, async resp => parse(await resp.Content.ReadAsStringAsync()));
            }
        }


        protected virtual void Dispose(bool isUserCall)
        {
            if (isUserCall)
                _client.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
