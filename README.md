# .NET client library for Bob

Bob is a distributed BLOB storage: https://github.com/qoollo/bob

# Usage example

```C#
using (var client = new BobNodeClient("10.5.5.131:20000"))
{
    client.Open();

    client.Put(1, new byte[] { 1, 2, 3 });
    var result = client.Get(1);

    client.Close();
}
```