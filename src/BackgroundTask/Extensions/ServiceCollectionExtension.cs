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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using TM.Core.TaskQueue.Abstractions;

namespace TM.Core.TaskQueue.Extensions
{
    public static class ServiceCollectionExtension
    {
        /// <summary>
        /// 添加后台任务队列（注入IBackgroundTaskQueue队列对象）
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddBackgroundTaskQueue(this IServiceCollection services)
        {
            services.AddHostedService<TaskQueuedHostedService>();
            services.AddScoped<IBackgroundTaskProvider, BackgroundTaskProvider>();
            BackgroundTaskQueue.Capacity = 10;
            return services;
        }

        /// <summary>
        /// 添加指定长度后台任务队列（注入IBackgroundTaskQueue队列对象）
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddBackgroundTaskQueue(this IServiceCollection services, int capacity)
        {
            if(capacity <= 0)
            {
                throw new ArgumentException("设置的队列容量不能小于等于0");
            }
            services.AddHostedService<TaskQueuedHostedService>();
            services.AddScoped<IBackgroundTaskProvider, BackgroundTaskProvider>();
            BackgroundTaskQueue.Capacity = capacity;
            return services;
        }
    }
}
