using NUnit.Framework;
using System;
using Arteranos.Core;
using UnityEngine.TestTools;
using System.Threading.Tasks;
using System.Collections;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Arteranos.Test.Structs
{
    /// <summary>
    /// A simplified example of properly handling of unmanaged resources:
    ///  * objects falling out of scope get their destructors after
    ///    a garbage collection, eventually.
    ///  * objects used by 'using' get their Dispose() called, exactly on fall out of scope,
    ///    and their destructor after a garbage collection.
    ///  * Calling Dispose() in the destructor results to call Dispose() twice, both after
    ///    'using' and the GC.
    /// </summary>
    class TestDisposable : IDisposable
    {
        string where = null;
        bool disposed = false;

        public TestDisposable(string where) 
        {
            this.where = where;
            Debug.Log($"{where} Constructor called");
        }

        ~TestDisposable()
        {
            Dispose();
            Debug.Log($"{where} Destructor called");
        }

        public void Dispose()
        {
            if(disposed)
            {
                Debug.Log($"{where} Dispose already called");
                return;
            }

            disposed = true;

            Debug.Log($"{where} Dispose called");
        }
    }

    public class Disposables
    {
        [Test]
        public void T001_JustScope()
        {
            {
                TestDisposable td = new("T001");

                td = null;
                Assert.IsNull(td);
            }
        }

        [Test]
        public void T002_UsingUsing()
        {
            {
                using TestDisposable td = new("T002");
                Assert.IsNotNull(td);
            }
        }

        [Test]
        public void T003_GarbageCollection()
        {
            {
                TestDisposable td = new("T003");

                td = null;
                Assert.IsNull(td);
            }

            Debug.Log("Collecting garbage...");
            GC.Collect();
        }

    }
}