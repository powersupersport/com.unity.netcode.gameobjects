using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    [TestFixture(1)]
    [TestFixture(2)]
    public class TestCoordinatorTests : BaseMultiprocessTests
    {
        private int m_WorkerCount;
        protected override int WorkerCount => m_WorkerCount;

        protected override bool IsPerformanceTest => false;

        public TestCoordinatorTests(int workerCount)
        {
            m_WorkerCount = workerCount;
        }

        static private float s_ValueToValidateAgainst;
        private static void ValidateSimpleCoordinatorTestValue(float resultReceived)
        {
            Assert.AreEqual(s_ValueToValidateAgainst, resultReceived);
        }

        private static void ExecuteSimpleCoordinatorTest()
        {
            s_ValueToValidateAgainst = float.PositiveInfinity;
            TestCoordinator.Instance.WriteTestResultsServerRpc(s_ValueToValidateAgainst);
        }

        private static void ExecuteWithArgs(byte[] args)
        {
            s_ValueToValidateAgainst = args[0];
            TestCoordinator.Instance.WriteTestResultsServerRpc(s_ValueToValidateAgainst);
        }

        [UnityTest]
        public IEnumerator CheckTestCoordinator()
        {
            // Sanity check for TestCoordinator
            // Call the method
            MultiprocessLogger.Log("CheckTestCoordinator test in TestCoordinatorTests about to call InvokeFromMethodActionRpc");
            TestCoordinator.Instance.InvokeFromMethodActionRpc(ExecuteSimpleCoordinatorTest);

            var nbResults = 0;
            MultiprocessLogger.Log($"WorkerCount is {WorkerCount}");
            for (int i = 0; i < WorkerCount; i++) // wait and test for the two clients
            {
                MultiprocessLogger.Log("Waiting for result to be set on TestCoordinator");
                yield return new WaitUntil(TestCoordinator.ResultIsSet());
                MultiprocessLogger.Log("Returning from wait");
                var (clientId, result) = TestCoordinator.ConsumeCurrentResult().Take(1).Single();
                MultiprocessLogger.Log($"Check if {result} is greater than 0");
                Assert.Greater(result, 0f);
                nbResults++;
            }
            MultiprocessLogger.Log($"Check that {nbResults} is equal to {WorkerCount}");
            Assert.That(nbResults, Is.EqualTo(WorkerCount));
        }

        [UnityTest]
        public IEnumerator CheckTestCoordinatorWithArgs()
        {
            TestCoordinator.Instance.InvokeFromMethodActionRpc(ExecuteWithArgs, 99);
            var nbResults = 0;

            for (int i = 0; i < WorkerCount; i++) // wait and test for the two clients
            {
                yield return new WaitUntil(TestCoordinator.ResultIsSet());

                var (clientId, result) = TestCoordinator.ConsumeCurrentResult().Take(1).Single();
                Assert.That(result, Is.EqualTo(99));
                nbResults++;
            }
            Assert.That(nbResults, Is.EqualTo(WorkerCount));
        }
    }
}
