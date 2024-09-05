using NUnit.Framework;
using System;
using Arteranos.Core;
using UnityEngine.TestTools;
using System.Threading.Tasks;
using System.Collections;
using System.Diagnostics;

namespace Arteranos.Test.Structs
{
    public class AsyncLazy
    {
        async Task<string> CostlyStringFunction()
        {
            await Task.Delay(5000);
            return "Test";
        }

        [UnityTest]
        public IEnumerator T001_Regular()
        {
            AsyncLazy<string> data = new(CostlyStringFunction);

            yield return data.WaitFor();
            Assert.AreEqual("Test", (string) data);
        }

        [UnityTest]
        public IEnumerator T002_NeverUsed() 
        {
            Stopwatch sw = Stopwatch.StartNew();

            AsyncLazy<string> data = new(CostlyStringFunction);

            Assert.IsTrue(sw.ElapsedMilliseconds < 2);
            yield return null;
        }

        [UnityTest]
        public IEnumerator T003_Expedited() 
        {
            AsyncLazy<string> data = new(CostlyStringFunction);

            // Missing WaitUntil(), the value has to be done yet.
            Assert.Throws<InvalidOperationException>(() =>
            {
                Assert.AreEqual("Test", (string) data);
            });

            yield return null;
        }

        [UnityTest]
        public IEnumerator T004_CoroutineAsync()
        {
            AsyncLazy<string> data = new(CostlyStringFunction);

            // Missing WaitUntil(), the value has to be done yet.
            Assert.Throws<InvalidOperationException>(() =>
            {
                Assert.AreEqual("Test", (string)data);
            });

            yield return data.WaitFor();

            Assert.AreEqual("Test", (string)data);
        }

        [UnityTest]
        public IEnumerator T005_Timings()
        {
            Stopwatch sw = Stopwatch.StartNew();

            // Setup
            AsyncLazy<string> data = new(CostlyStringFunction);
            Assert.IsTrue(sw.ElapsedMilliseconds < 1);

            // First access (and instantiation)
            sw.Restart();
            yield return data.WaitFor();
            Assert.AreEqual("Test", (string)data);
            Assert.IsTrue(sw.ElapsedMilliseconds > 4500);

            // Subsequent accesses
            sw.Restart();
            Assert.AreEqual("Test", (string)data);
            Assert.IsTrue(sw.ElapsedMilliseconds < 2);
        }
    }
}