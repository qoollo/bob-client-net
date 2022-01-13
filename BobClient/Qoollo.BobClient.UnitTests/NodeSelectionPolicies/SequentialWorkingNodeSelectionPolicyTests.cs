using Qoollo.BobClient.NodeSelectionPolicies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests.NodeSelectionPolicies
{
    public class SequentialWorkingNodeSelectionPolicyTests : BobTestsBaseClass
    {
        public SequentialWorkingNodeSelectionPolicyTests(Xunit.Abstractions.ITestOutputHelper output) : base(output) { }

        [Fact]
        public void SimpleSequenceTest()
        {
            var nodes = new BobNodeClientStatusMock[]
            {
                new BobNodeClientStatusMock(),
                new BobNodeClientStatusMock(),
                new BobNodeClientStatusMock(),
                new BobNodeClientStatusMock()
            };

            var factory = SequentialWorkingNodeSelectionPolicy.CreateFactory(1, 1000);
            var policy = factory.Create(nodes);

            int initial = policy.SelectNodeIndex(BobOperationKind.Get, default(BobKey));
            for (int i = 0; i < 1000; i++)
            {
                int cur = policy.SelectNodeIndex(BobOperationKind.Get, default(BobKey));
                Assert.Equal((initial + i + 1) % nodes.Length, cur);
            }
        }

        [Fact]
        public void SkipNotWorkingNodesTest()
        {
            var nodes = new BobNodeClientStatusMock[]
            {
                new BobNodeClientStatusMock() { State = BobNodeClientState.TransientFailure, SequentialErrorCount = 100 },
                new BobNodeClientStatusMock(),
                new BobNodeClientStatusMock() { State = BobNodeClientState.TransientFailure, SequentialErrorCount = 100 },
                new BobNodeClientStatusMock() { State = BobNodeClientState.TransientFailure, SequentialErrorCount = 100 },
                new BobNodeClientStatusMock()
            };

            var factory = SequentialWorkingNodeSelectionPolicy.CreateFactory(1, 1000);
            var policy = factory.Create(nodes);

            int prev = policy.SelectNodeIndex(BobOperationKind.Get, default(BobKey));
            for (int i = 0; i < 1000; i++)
            {
                int expected = (prev + 1) % nodes.Length;
                while (nodes[expected].State != BobNodeClientState.Ready)
                    expected = (expected + 1) % nodes.Length;

                int cur = policy.SelectNodeIndex(BobOperationKind.Get, default(BobKey));
                Assert.Equal(BobNodeClientState.Ready, nodes[cur].State);
                Assert.Equal(expected, cur);

                prev = cur;
            }
        }

        [Fact]
        public void RetrySkipsNotWorkingTest()
        {
            var nodes = new BobNodeClientStatusMock[]
            {
                new BobNodeClientStatusMock() { State = BobNodeClientState.TransientFailure, SequentialErrorCount = 100 },
                new BobNodeClientStatusMock(),
                new BobNodeClientStatusMock() { State = BobNodeClientState.TransientFailure, SequentialErrorCount = 100 },
                new BobNodeClientStatusMock() { State = BobNodeClientState.TransientFailure, SequentialErrorCount = 100 },
                new BobNodeClientStatusMock()
            };

            var factory = SequentialWorkingNodeSelectionPolicy.CreateFactory(1, 1000);
            var policy = factory.Create(nodes);

            int prev = policy.SelectNodeIndex(BobOperationKind.Get, default(BobKey));
            for (int i = 0; i < 1000; i++)
            {
                int expected = (prev + 1) % nodes.Length;
                while (nodes[expected].State != BobNodeClientState.Ready)
                    expected = (expected + 1) % nodes.Length;

                int cur = policy.SelectNodeIndexOnRetry(prev, BobOperationKind.Get, default(BobKey));
                Assert.Equal(BobNodeClientState.Ready, nodes[cur].State);
                Assert.Equal(expected, cur);

                prev = cur;
            }
        }


        [Fact]
        public void NoRetryWhenAllNodesDeadTest()
        {
            var nodes = new BobNodeClientStatusMock[]
            {
                new BobNodeClientStatusMock() { State = BobNodeClientState.TransientFailure, SequentialErrorCount = 100 },
                new BobNodeClientStatusMock() { State = BobNodeClientState.TransientFailure, SequentialErrorCount = 100 },
            };

            var factory = SequentialWorkingNodeSelectionPolicy.CreateFactory(1, 1000);
            var policy = factory.Create(nodes);

            int initial = policy.SelectNodeIndex(BobOperationKind.Get, default(BobKey));
            int retry = policy.SelectNodeIndexOnRetry(initial, BobOperationKind.Get, default(BobKey));

            Assert.True(retry < 0);
        }


        [Fact]
        public void FallBackToSequentialScanWhenAllNodesDeadTest()
        {
            var nodes = new BobNodeClientStatusMock[]
            {
                new BobNodeClientStatusMock() { State = BobNodeClientState.TransientFailure, SequentialErrorCount = 100 },
                new BobNodeClientStatusMock() { State = BobNodeClientState.TransientFailure, SequentialErrorCount = 100 },
            };

            var factory = SequentialWorkingNodeSelectionPolicy.CreateFactory(1, 1000);
            var policy = factory.Create(nodes);

            int prev = policy.SelectNodeIndex(BobOperationKind.Get, default(BobKey));
            for (int i = 0; i < 100; i++)
            {
                int expected = (prev + 1) % nodes.Length;

                int cur = policy.SelectNodeIndex(BobOperationKind.Get, default(BobKey));
                Assert.Equal(expected, cur);

                prev = cur;
            }
        }
    }
}
