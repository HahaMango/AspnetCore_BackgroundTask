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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TM.Core.TaskQueue.Abstractions;

namespace TM.Core.TaskQueue
{
    /// <summary>
    /// 后台服务（负责执行所有被添加到后台队列的任务）
    /// </summary>
    public class TaskQueuedHostedService : BackgroundService
    {
        private readonly ILogger<TaskQueuedHostedService> _logger;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IServiceProvider _serviceProvider;
        private readonly TaskQueuedOptions _options;

        private readonly ConcurrentDictionary<InvokePackage, int> _taskFalidCountDic;

        internal IReadOnlyDictionary<InvokePackage, int> TaskFalidCountDic => _taskFalidCountDic;

        public TaskQueuedHostedService(
            ILogger<TaskQueuedHostedService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _taskQueue = BackgroundTaskQueue.Instance();
            _serviceProvider = serviceProvider;
        }

        public TaskQueuedHostedService(
            ILogger<TaskQueuedHostedService> logger,
            IServiceProvider serviceProvider,
            TaskQueuedOptions options)
        {
            _logger = logger;
            _taskQueue = BackgroundTaskQueue.Instance();
            _serviceProvider = serviceProvider;
            _options = options;
            //开启失败重试
            if (_options.EnableFalidRetry)
            {
                _taskFalidCountDic = new ConcurrentDictionary<InvokePackage, int>();
            }
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("后台任务队列服务启动...");
            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem =
                    await _taskQueue.DequeueAsync(stoppingToken);

                try
                {
                    var has = _taskQueue.GetHashCode();
                    _ = Task.Run(async () =>
                    {
                        var ip = workItem;
                        using var scope = _serviceProvider.CreateScope();
                        try
                        {
                            var handler = scope.ServiceProvider.GetRequiredService(ip.HandlerType);
                            var method = handler.GetType().GetMethod("Handler", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                            await (Task)method.Invoke(handler, new object[] { ip.Request, ip.CancelToken });
                            //await handler.Handler(ip.Request, ip.cancelToken);
                        }
                        catch(Exception ex)
                        {
                            var logger = scope.ServiceProvider.GetRequiredService<ILogger<TaskQueuedHostedService>>();
                            logger.LogError($"执行{ip.HandlerType.FullName}异常：{ex}");
                            if (_options != null && _options.EnableFalidRetry && ip.EnableFalidRetry)
                            {
                                await BackToQueued(ip);
                            }
                            throw;
                        }
                    });

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "后台任务队列服务异常", nameof(workItem));
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("后台任务队列服务停止...");

            await base.StopAsync(stoppingToken);
        }

        private async Task BackToQueued(InvokePackage invokePackage)
        {
            var falidCount = IncFalidCount(invokePackage);
            if (falidCount <= _options.MaxRetryTime)
            {
                //小于最大重试次数放回任务队列
                await _taskQueue.QueueBackgroundWorkItemAsync(invokePackage);
            }
            else
            {
                //否则清除计数
                _taskFalidCountDic.TryRemove(invokePackage, out falidCount);
            }
        }

        private int IncFalidCount(InvokePackage invokePackage)
        {
            var falidCount = 0;
            if (_taskFalidCountDic.ContainsKey(invokePackage))
            {
                //键存在则加一
                falidCount = _taskFalidCountDic[invokePackage] + 1;
                _taskFalidCountDic[invokePackage] = falidCount;
            }
            else
            {
                //键不存在则添加默认1
                _taskFalidCountDic[invokePackage] = 1;
                falidCount = 1;
            }
            return falidCount;
        }
    }
}
