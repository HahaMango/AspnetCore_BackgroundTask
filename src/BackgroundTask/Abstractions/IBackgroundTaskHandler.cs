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

namespace TM.Core.TaskQueue.Abstractions
{
    /// <summary>
    /// 后台作业处理接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IBackgroundTaskHandler<T> 
        where T : IBackgroundTaskRequest
    {
        Task Handler(T request, CancellationToken token = default);
    }
}
