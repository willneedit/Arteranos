using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Threading.Tasks;
using System.Collections;
using System.Threading;
using System;
using Random = UnityEngine.Random;

namespace Arteranos.PlayTest
{
    [TestFixture]
    public class TaskScheduler
    {
        GameObject SchedulerInstance = null;
        [UnitySetUp] public IEnumerator SetUp() 
        {
            SchedulerInstance = new GameObject();
            SchedulerInstance.AddComponent<Core.TaskScheduler>();

            yield return null;
        }

        [TearDown] public void TearDown() 
        {
            UnityEngine.Object.DestroyImmediate(SchedulerInstance);
        }

        [UnityTest]
        public IEnumerator SchedulerTest()
        {
            int numfinished = 0;

            async Task TestTask(int number, int duration)
            {
                Debug.Log($"Stsrting task {number}");
                await Task.Delay(duration);
                Debug.Log($"Ending task {number} after {duration} ms delay time");

                Interlocked.Increment(ref numfinished);
            }

            Func<Task> NewMethod(int number)
            {
                int duration = Random.Range(200, 1000);
                Task a() => TestTask(number, duration);
                return a;
            }

            Assert.IsNotNull(Core.TaskScheduler.Instance);

            for(int number = 0; number < 100; number++)
            {
                Core.TaskScheduler.Schedule(NewMethod(number));
            }

            Debug.Log("All tasks deployed, waiting for finished.");

            yield return new WaitUntil(() => numfinished == 100);

            Debug.Log("All tasks accounted for.");

        }
    }
}