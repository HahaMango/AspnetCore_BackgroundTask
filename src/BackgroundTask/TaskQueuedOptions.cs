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

namespace TM.Core.TaskQueue
{
    /// <summary>
    /// 后台任务options
    /// </summary>
    public class TaskQueuedOptions
    {
        /// <summary>
        /// 是否开启失败重试
        /// </summary>
        public bool EnableFalidRetry { get; set; } = false;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetryTime { get; set; } = 3;

        /// <summary>
        /// 任务队列长度
        /// </summary>
        public int Capacity { get; set; } = 10;
    }
}
