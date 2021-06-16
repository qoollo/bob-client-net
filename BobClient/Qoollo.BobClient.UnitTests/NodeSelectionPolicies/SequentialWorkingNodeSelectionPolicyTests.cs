using Qoollo.BobClient.NodeSelectionPolicies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests.NodeSelectionPolicies
{
    public class SequentialWorkingNodeSelectionPolicyTests
    {
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

            int initial = policy.SelectNextNodeIndex();
            for (int i = 0; i < 1000; i++)
            {
                int cur = policy.SelectNextNodeIndex();
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

            int prev = policy.SelectNextNodeIndex();
            for (int i = 0; i < 1000; i++)
            {
                int expected = (prev + 1) % nodes.Length;
                while (nodes[expected].State != BobNodeClientState.Ready)
                    expected = (expected + 1) % nodes.Length;

                int cur = policy.SelectNextNodeIndex();
                Assert.Equal(BobNodeClientState.Ready, nodes[cur].State);
                Assert.Equal(expected, cur);

                prev = cur;
            }
        }
    }
}
