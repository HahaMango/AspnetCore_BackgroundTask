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
using System.Threading.Tasks;
using TM.Core.TaskQueue.Abstractions;

namespace TM.Core.TaskQueue
{
    /// <summary>
    /// 后台任务对象（记录后台委托，调用参数等）
    /// </summary>
#if CodeTest
    public
#else
    internal
#endif
        class InvokePackage
    {
        public InvokePackage(IBackgroundTaskRequest request, Type handlerType , CancellationToken cancelToken)
        {
            Request = request;
            HandlerType = handlerType;
        }

        public InvokePackage(IBackgroundTaskRequest request, Type handlerType, CancellationToken cancelToken, bool enableRetry) : this(request, handlerType, cancelToken)
        {
            EnableFalidRetry = enableRetry;
        }

        public Type HandlerType { get; set; }

        public IBackgroundTaskRequest Request { get; set; }

        /// <summary>
        /// 失败重试
        /// </summary>
        public bool EnableFalidRetry { get; set; } = false;

        public CancellationToken cancelToken { get; set; }
    }
}
