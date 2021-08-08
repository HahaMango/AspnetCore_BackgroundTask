/*--------------------------------------------------------------------------
//
//  Copyright 2021 Chiva Chen
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//
/*--------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TM.Core.TaskQueue.Abstractions;

namespace TM.Core.TaskQueue
{
    /// <summary>
    /// 后台作业队列接口实现
    /// </summary>
#if CodeTest
    public
#else
    internal
#endif
        class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<InvokePackage> _queue;

        private static IBackgroundTaskQueue _singletonObject;

        private static object _lock = new object();

        public static int Capacity { get; set; } = 10;

        private BackgroundTaskQueue()
        {
            // Capacity should be set based on the expected application load and
            // number of concurrent threads accessing the queue.            
            // BoundedChannelFullMode.Wait will cause calls to WriteAsync() to return a task,
            // which completes only when space became available. This leads to backpressure,
            // in case too many publishers/calls start accumulating.
            if (Capacity <= 0)
                throw new ArgumentException($"{nameof(Capacity)}：不能小于等于0");

            var options = new BoundedChannelOptions(Capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<InvokePackage>(options);
        }

        public static IBackgroundTaskQueue Instance()
        {
#if !CodeTest
            lock (_lock)
            {
                if(_singletonObject == null)
                {
                    _singletonObject = new BackgroundTaskQueue();
                }
                return _singletonObject;
            }
#else
            return new BackgroundTaskQueue();
#endif
        }

        public async ValueTask<InvokePackage> DequeueAsync(CancellationToken cancellationToken)
        {
            var workItem = await _queue.Reader.ReadAsync(cancellationToken);

            return workItem;
        }

        public async ValueTask QueueBackgroundWorkItemAsync(InvokePackage workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            await _queue.Writer.WriteAsync(workItem);
        }

        public async ValueTask QueueBackgroundWorkItemAsync(IBackgroundTaskRequest request, Type handlerType, CancellationToken token)
        {
            if(request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await _queue.Writer.WriteAsync(new InvokePackage(request, handlerType, token));
        }

        public async ValueTask QueueBackgroundWorkItemAsync(IBackgroundTaskRequest request, Type handlerType, CancellationToken token, bool enableRetry)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await _queue.Writer.WriteAsync(new InvokePackage(request, handlerType, token, enableRetry));
        }
    }
}
