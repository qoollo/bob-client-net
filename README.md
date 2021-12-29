# .NET client library for Bob
[![NuGet](https://img.shields.io/nuget/v/Qoollo.BobClient.svg)](https://www.nuget.org/packages/Qoollo.BobClient/) [![build](https://github.com/qoollo/bob-client-net/actions/workflows/build.yaml/badge.svg)](https://github.com/qoollo/bob-client-net/actions/workflows/build.yaml) [![test](https://github.com/qoollo/bob-client-net/actions/workflows/test.yaml/badge.svg)](https://github.com/qoollo/bob-client-net/actions/workflows/test.yaml)

Bob is a distributed BLOB storage: https://github.com/qoollo/bob

# Usage example for single node client

```C#
using (var client = new BobNodeClient<ulong>("Address = 10.5.5.131:20000; OperationTimeout = 00:02:00"))
{
    client.Open();

    client.Put(1, new byte[] { 1, 2, 3 });
    var result = client.Get(1);

    client.Close();
}
```


# Usage example for cluster

```C#
using (var client = new BobClusterBuilder<ulong>()
    .WithAdditionalNode("Address = 10.5.5.131; OperationTimeout = 00:02:00")
    .WithAdditionalNode("Address = 10.5.5.132; OperationTimeout = 00:02:00")
    .WithSequentialNodeSelectionPolicy()
    .Build())
{
    client.Open();

    client.Put(1, new byte[] { 1, 2, 3 });
    var result = client.Get(1);

    client.Close();
}
```