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

using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TM.Core.TaskQueue.Abstractions;

namespace TM.Core.TaskQueue
{
    /// <summary>
    /// 后台作业提供器接口实现
    /// </summary>
    public class BackgroundTaskProvider : IBackgroundTaskProvider
    {
#if !CodeTest
        private readonly
#else
        public
#endif
            IBackgroundTaskQueue _queue;

        public BackgroundTaskProvider()
        {
            _queue = BackgroundTaskQueue.Instance();
        }

        /// <summary>
        /// 发布任务
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task Send(IBackgroundTaskRequest request, CancellationToken token = default(CancellationToken))
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            await _queue.QueueBackgroundWorkItemAsync(request, GetHandlerType(request),token);
        }

        /// <summary>
        /// 发布任务
        /// </summary>
        /// <param name="request"></param>
        /// <param name="enableRetry"></param>
        /// <returns></returns>
        public async Task Send(IBackgroundTaskRequest request, bool enableRetry, CancellationToken token = default(CancellationToken))
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            await _queue.QueueBackgroundWorkItemAsync(request, GetHandlerType(request), token, enableRetry);
        }

        private Type GetHandlerType(IBackgroundTaskRequest request)
        {
            var handlerInterface = typeof(IBackgroundTaskHandler<>);
            handlerInterface = handlerInterface.MakeGenericType(request.GetType());
            var allAssembly = GetCurrentPathAssembly();
            Type firstHandler = null;
            foreach (var ass in allAssembly)
            {
                var tempTypeArray = ass.GetTypes().Where(x => x.IsClass == true && x.IsAbstract == false);
                foreach(var type in tempTypeArray)
                {
                    //找到实现IBackgroundTaskHandler的类
                    var typeInterface = type.GetInterfaces().FirstOrDefault(x=>x == handlerInterface);
                    if(typeInterface != null)
                    {
                        firstHandler = type;
                    }
                }
                if(firstHandler != null)
                    break;
            }
            if (firstHandler == null)
                throw new Exception($"无法找到{nameof(request)}的后台任务处理器");

            return firstHandler;
        }

        private List<Assembly> GetCurrentPathAssembly()
        {
            var dlls = DependencyContext.Default.CompileLibraries
                .Where(x => !x.Name.StartsWith("Microsoft") && !x.Name.StartsWith("System"))
                .ToList();
            var list = new List<Assembly>();
            if (dlls.Any())
            {
                foreach (var dll in dlls)
                {
                    if (dll.Type == "project")
                    {
                        list.Add(Assembly.Load(dll.Name));
                    }
                }
            }
            return list;
        }
    }
}
