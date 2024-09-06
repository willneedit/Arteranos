/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace Arteranos.Core
{

    /*
     * Example:
     * 
     * static AsyncLazy<string> m_data = new AsyncLazy<string>(async delegate
     * {
     *      WebClient client = new WebClient();
     *      return (await client.DownloadStringTaskAsync(someUrl)).ToUpper();
     * });
     */

    /// <summary>
    /// AsyncLazy, Unity flavored
    /// </summary>
    /// <typeparam name="T">The underlying type</typeparam>
    public class AsyncLazy<T> : Lazy<Task<T>>
    {
        public AsyncLazy(Func<T> valueFactory) :
            base(() => Task.Factory.StartNew(valueFactory))
        { }
        public AsyncLazy(Func<Task<T>> taskFactory) :
            base(() => Task.Factory.StartNew(() => taskFactory()).Unwrap())
        { }

        /// <summary>
        /// Allows use of 'await lazyvalue'
        /// </summary>
        /// <returns>The TaskAwaiter itself</returns>
        public TaskAwaiter<T> GetAwaiter() => Value.GetAwaiter();

        /// <summary>
        /// Allows use of 'yield return lazyvalue.WaitFor();' in a Coroutine
        /// </summary>
        /// <returns>The Enumerator to wait the value to be created</returns>
        public IEnumerator WaitFor()
        {
            yield return new WaitUntil(() => GetAwaiter().IsCompleted);
        }

        /// <summary>
        /// For the two-stage value retrieval in Coroutine
        /// </summary>
        /// <param name="lazy">The variable itself</param>
        public static implicit operator T(AsyncLazy<T> lazy)
        {
            if (!lazy.GetAwaiter().IsCompleted)
                throw new InvalidOperationException("Would block");

            return lazy.Value.Result;
        }

        /// <summary>
        /// For the two-stage value retrieval in Coroutine and implicit conversion fails
        /// </summary>
        /// <returns>The variable itself</returns>
        /// <exception cref="InvalidOperationException">Access would block</exception>
        public T Result
        {
            get
            {
                if (!Value.GetAwaiter().IsCompleted)
                    throw new InvalidOperationException("Would block");

                return Value.Result;
            }
        }
    }
}