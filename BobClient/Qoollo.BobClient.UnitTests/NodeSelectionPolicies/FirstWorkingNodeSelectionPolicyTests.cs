using Qoollo.BobClient.NodeSelectionPolicies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests.NodeSelectionPolicies
{
    public class FirstWorkingNodeSelectionPolicyTests
    {
        [Fact]
        public void SimpleFirstNodeSelectionTest()
        {
            var nodes = new BobNodeClientStatusMock[]
            {
                new BobNodeClientStatusMock(),
                new BobNodeClientStatusMock(),
                new BobNodeClientStatusMock(),
                new BobNodeClientStatusMock()
            };

            var policy = FirstWorkingNodeSelectionPolicy.Factory.Create(nodes);

            int initial = policy.SelectNodeIndex(BobOperationKind.Get, default(BobKey));
            Assert.Equal(0, initial);
            for (int i = 0; i < 100; i++)
            {
                int cur = policy.SelectNodeIndex(BobOperationKind.Get, default(BobKey));
                Assert.Equal(initial, cur);
            }
        }

        [Fact]
        public void SkipsNonWorkingNodesTest()
        {
            var nodes = new BobNodeClientStatusMock[]
            {
                new BobNodeClientStatusMock() { State = BobNodeClientState.TransientFailure, SequentialErrorCount = 100 },
                new BobNodeClientStatusMock() { State = BobNodeClientState.TransientFailure, SequentialErrorCount = 100 },
                new BobNodeClientStatusMock(),
                new BobNodeClientStatusMock()
            };

            var policy = FirstWorkingNodeSelectionPolicy.Factory.Create(nodes);

            int initial = policy.SelectNodeIndex(BobOperationKind.Get, default(BobKey));
            Assert.Equal(2, initial);
            for (int i = 0; i < 100; i++)
            {
                int cur = policy.SelectNodeIndex(BobOperationKind.Get, default(BobKey));
                Assert.Equal(initial, cur);
            }
        }

        [Fact]
        public void FallBackToSequentialScanWhenAllNodesDeadTest()
        {
            var nodes = new BobNodeClientStatusMock[]
            {
                new BobNodeClientStatusMock() { State = BobNodeClientState.TransientFailure, SequentialErrorCount = 100 },
                new BobNodeClientStatusMock() { State = BobNodeClientState.TransientFailure, SequentialErrorCount = 100 },
            };

            var policy = FirstWorkingNodeSelectionPolicy.Factory.Create(nodes);

            int prev = policy.SelectNodeIndex(BobOperationKind.Get, default(BobKey));
            for (int i = 0; i < 100; i++)
            {
                int expected = (prev + 1) % nodes.Length;

                int cur = policy.SelectNodeIndex(BobOperationKind.Get, default(BobKey));
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

            var policy = FirstWorkingNodeSelectionPolicy.Factory.Create(nodes);

            int firstAttempt = policy.SelectNodeIndex(BobOperationKind.Get, default(BobKey));
            Assert.Equal(1, firstAttempt);

            int secondAttempt = policy.SelectNodeIndexOnRetry(firstAttempt, BobOperationKind.Get, default(BobKey));
            Assert.Equal(4, secondAttempt);
        }


        [Fact]
        public void NoRetryWhenAllNodesDeadTest()
        {
            var nodes = new BobNodeClientStatusMock[]
            {
                new BobNodeClientStatusMock() { State = BobNodeClientState.TransientFailure, SequentialErrorCount = 100 },
                new BobNodeClientStatusMock() { State = BobNodeClientState.TransientFailure, SequentialErrorCount = 100 },
            };

            var policy = FirstWorkingNodeSelectionPolicy.Factory.Create(nodes);

            int initial = policy.SelectNodeIndex(BobOperationKind.Get, default(BobKey));
            int retry = policy.SelectNodeIndexOnRetry(initial, BobOperationKind.Get, default(BobKey));

            Assert.True(retry < 0);
        }
    }
}
