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

using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TM.Core.TaskQueue;
using TM.Core.TaskQueue.Abstractions;
using System.Linq;
using Xunit;
using Microsoft.Extensions.DependencyModel;

namespace FrameworkTest.TM.Core.Test.TaskQueueTest
{
    /// <summary>
    /// 后台队列测试集
    /// </summary>
    public class TaskQueueTest
    {
        #region BackgroundTaskQueue测试
        /// <summary>
        /// BackgroundTaskQueue对象初始化测试
        /// </summary>
        /// <returns></returns>
        [Fact]
        public void BackgroundTaskQueue_InitTest()
        {
            BackgroundTaskQueue.Capacity = 10;
            var queue = BackgroundTaskQueue.Instance();

            Assert.NotNull(queue);
        }

        /// <summary>
        /// BackgroundTaskQueue对象错误初始化测试
        /// </summary>
        /// <param name="cap"></param>
        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(-54)]
        public void BackgroundTaskQueue_InitLessThenZeroTest(int cap)
        {
            Assert.ThrowsAny<System.Exception>(() =>
            {
                BackgroundTaskQueue.Capacity = cap;
                BackgroundTaskQueue.Instance();
            });
        }

        /// <summary>
        /// 入队出队测试
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task BackgroundTaskQueue_WriteReadTest()
        {
            var item = new InvokePackage(new TestRequest(), typeof(TestHandler), default);

            BackgroundTaskQueue.Capacity = 10;
            var queue = BackgroundTaskQueue.Instance();

            await queue.QueueBackgroundWorkItemAsync(item);

            var resultItem = await queue.DequeueAsync(CancellationToken.None);

            Assert.Equal(item.GetHashCode(), resultItem.GetHashCode());
        }

        /// <summary>
        /// 多次入队出队测试
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task BackgroundTaskQueue_MultWriteReadTest()
        {
            var item1 = new InvokePackage(new TestRequest(), typeof(TestHandler),default);
            var item2 = new InvokePackage(new TestRequest(), typeof(TestHandler), default);
            var item3 = new InvokePackage(new TestRequest(), typeof(TestHandler), default);

            BackgroundTaskQueue.Capacity = 10;
            var queue = BackgroundTaskQueue.Instance();

            await queue.QueueBackgroundWorkItemAsync(item1);

            var resultItem = await queue.DequeueAsync(CancellationToken.None);

            Assert.Equal(item1.GetHashCode(), resultItem.GetHashCode());

            await queue.QueueBackgroundWorkItemAsync(item1);
            await queue.QueueBackgroundWorkItemAsync(item2);
            resultItem = await queue.DequeueAsync(CancellationToken.None);

            Assert.Equal(item1.GetHashCode(), resultItem.GetHashCode());

            await queue.QueueBackgroundWorkItemAsync(item3);
            resultItem = await queue.DequeueAsync(CancellationToken.None);

            Assert.Equal(item2.GetHashCode(), resultItem.GetHashCode());

            resultItem = await queue.DequeueAsync(CancellationToken.None);

            Assert.Equal(item3.GetHashCode(), resultItem.GetHashCode());
        }

        /// <summary>
        /// 入队重载方法测试
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task BackgroundTaskQueue_QueueBackgroundWorkItemAsyncTest()
        {
            var request = new TestRequest();

            var handlerType = typeof(TestHandler);

            BackgroundTaskQueue.Capacity = 10;
            var queue = BackgroundTaskQueue.Instance();
            await queue.QueueBackgroundWorkItemAsync(request, handlerType,default);
            var resultItem = await queue.DequeueAsync(CancellationToken.None);

            Assert.Equal(resultItem.Request.GetHashCode(), request.GetHashCode());
            Assert.Equal(resultItem.HandlerType.GetHashCode(), handlerType.GetHashCode());
        }

        /// <summary>
        /// 入队重试次数重载方法测试
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task BackgroundTaskQueue_QueueBackgroundWorkItemEnableRetryTest()
        {
            var request = new TestRequest();

            var handlerType = typeof(TestHandler);

            BackgroundTaskQueue.Capacity = 10;
            var queue = BackgroundTaskQueue.Instance();
            await queue.QueueBackgroundWorkItemAsync(request, handlerType, default, true);
            var resultItem = await queue.DequeueAsync(CancellationToken.None);

            Assert.Equal(resultItem.Request.GetHashCode(), request.GetHashCode());
            Assert.Equal(resultItem.HandlerType.GetHashCode(), handlerType.GetHashCode());
            Assert.True(resultItem.EnableFalidRetry);
        }

        #endregion

        #region TaskQueuedHostedService测试
        private Mock<ILogger<TaskQueuedHostedService>> _loggerMock;
        private IBackgroundTaskQueue _taskQueue;
        private Mock<IServiceProvider> _serviceProviderMock;

        /// <summary>
        /// 运行测试
        /// </summary>
        [Fact]
        public void TaskQueuedHostedService_RunTest()
        {
            _loggerMock = new Mock<ILogger<TaskQueuedHostedService>>();
            BackgroundTaskQueue.Capacity = 10;
            _taskQueue = BackgroundTaskQueue.Instance();
            _serviceProviderMock = new Mock<IServiceProvider>();

            var request = new TestRequest();
            var handler = new TestHandler();

            _taskQueue.QueueBackgroundWorkItemAsync(request, handler.GetType(), default);

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(3000);
            var cancelToken = cancellationTokenSource.Token;

            var service = new TaskQueuedHostedService(_loggerMock.Object, _serviceProviderMock.Object);
            var executeAsyncMethod = service.GetType().GetMethod("ExecuteAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            executeAsyncMethod.Invoke(service, new object[] { cancelToken });


        }

        ///// <summary>
        ///// 测试不开启重试情况下抛出异常
        ///// </summary>
        //[Fact]
        //public void TaskQueuedHostedService_IncFalidCountNotEnableRetryTest()
        //{
        //    _loggerMock = new Mock<ILogger<TaskQueuedHostedService>>();
        //    _taskQueue = new BackgroundTaskQueue(10);
        //    _serviceProviderMock = new Mock<IServiceProvider>();

        //    var testPackage = new InvokePackage((p) =>
        //    {
        //        return Task.CompletedTask;
        //    }, new MyParam());

        //    var service = new TaskQueuedHostedService(_loggerMock.Object, _taskQueue, _serviceProviderMock.Object, new TaskQueuedOptions());
        //    var incFalidCountMethod = service.GetType().GetMethod("IncFalidCount", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        //    Assert.ThrowsAny<Exception>(() => incFalidCountMethod.Invoke(service, new object[] { testPackage }));
        //}

        ///// <summary>
        ///// 测试增加失败计数
        ///// </summary>
        //[Fact]
        //public void TaskQueuedHostedService_IncFalidCountAddTest()
        //{
        //    _loggerMock = new Mock<ILogger<TaskQueuedHostedService>>();
        //    _taskQueue = new BackgroundTaskQueue(10);
        //    _serviceProviderMock = new Mock<IServiceProvider>();

        //    var testPackage = new InvokePackage((p) =>
        //    {
        //        return Task.CompletedTask;
        //    }, new MyParam());

        //    var service = new TaskQueuedHostedService(_loggerMock.Object, _taskQueue, _serviceProviderMock.Object, new TaskQueuedOptions { EnableFalidRetry = true});
        //    var incFalidCountMethod = service.GetType().GetMethod("IncFalidCount", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        //    var count = incFalidCountMethod.Invoke(service, new object[] { testPackage });

        //    Assert.Equal(1, count);
        //}

        ///// <summary>
        ///// 测试增加失败计数2次
        ///// </summary>
        //[Fact]
        //public void TaskQueuedHostedService_IncFalidCountAddTwiceTest()
        //{
        //    _loggerMock = new Mock<ILogger<TaskQueuedHostedService>>();
        //    _taskQueue = new BackgroundTaskQueue(10);
        //    _serviceProviderMock = new Mock<IServiceProvider>();

        //    var testPackage = new InvokePackage((p) =>
        //    {
        //        return Task.CompletedTask;
        //    }, new MyParam());

        //    var service = new TaskQueuedHostedService(_loggerMock.Object, _taskQueue, _serviceProviderMock.Object, new TaskQueuedOptions { EnableFalidRetry = true });
        //    var incFalidCountMethod = service.GetType().GetMethod("IncFalidCount", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        //    var count = incFalidCountMethod.Invoke(service, new object[] { testPackage });
        //    count = incFalidCountMethod.Invoke(service, new object[] { testPackage });

        //    Assert.Equal(2, count);
        //}

        ///// <summary>
        ///// 放回队列测试
        ///// </summary>
        ///// <returns></returns>
        //[Fact]
        //public async Task TaskQueuedHostedService_BackToQueuedTest()
        //{
        //    _loggerMock = new Mock<ILogger<TaskQueuedHostedService>>();
        //    _taskQueue = new BackgroundTaskQueue(10);
        //    _serviceProviderMock = new Mock<IServiceProvider>();

        //    var testPackage = new InvokePackage((p) =>
        //    {
        //        return Task.CompletedTask;
        //    }, new MyParam());

        //    var cancellationTokenSource = new CancellationTokenSource();
        //    cancellationTokenSource.CancelAfter(3000);
        //    var cancelToken = cancellationTokenSource.Token;

        //    var service = new TaskQueuedHostedService(_loggerMock.Object, _taskQueue, _serviceProviderMock.Object, new TaskQueuedOptions { EnableFalidRetry = true });
        //    var backToQueuedMethod = service.GetType().GetMethod("BackToQueued", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        //    await Task.Run(() => (Task)backToQueuedMethod.Invoke(service, new object[] { testPackage }));

        //    var ip = await _taskQueue.DequeueAsync(cancelToken);
        //    Assert.Equal(testPackage, ip);
        //}

        ///// <summary>
        ///// 达到最大重试次数测试
        ///// </summary>
        //[Fact]
        //public async Task TaskQueuedHostedService_BackToQueuedMaxRetryCountTest()
        //{
        //    _loggerMock = new Mock<ILogger<TaskQueuedHostedService>>();
        //    _taskQueue = new BackgroundTaskQueue(10);
        //    _serviceProviderMock = new Mock<IServiceProvider>();

        //    var testPackage = new InvokePackage((p) =>
        //    {
        //        return Task.CompletedTask;
        //    }, new MyParam());

        //    var cancellationTokenSource = new CancellationTokenSource();
        //    cancellationTokenSource.CancelAfter(3000);
        //    var cancelToken = cancellationTokenSource.Token;

        //    var service = new TaskQueuedHostedService(_loggerMock.Object, _taskQueue, _serviceProviderMock.Object, new TaskQueuedOptions { EnableFalidRetry = true });
        //    var incFalidCountMethod = service.GetType().GetMethod("IncFalidCount", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        //    for (var i = 0; i < 4; i++)
        //    {
        //        //假设3次都失败，错误计数达到3次
        //        incFalidCountMethod.Invoke(service, new object[] { testPackage });
        //    }
        //    var backToQueuedMethod = service.GetType().GetMethod("BackToQueued", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        //    await Task.Run(() => (Task)backToQueuedMethod.Invoke(service, new object[] { testPackage }));

        //    await Assert.ThrowsAsync<OperationCanceledException>(async () => await _taskQueue.DequeueAsync(cancelToken));
        //    Assert.False(service.TaskFalidCountDic.TryGetValue(testPackage, out var count));
        //}
        #endregion

        #region BackgroundTaskProvider测试

        /// <summary>
        /// 反射获取处理器测试
        /// </summary>
        [Fact]
        public void BackgroundTaskProvider_GetHandlerTypeTest()
        {
            var request = new TestRequest();
            BackgroundTaskQueue.Capacity = 10;
            var provider = new BackgroundTaskProvider();
            var type = provider.GetType().GetMethod("GetHandlerType", BindingFlags.Instance | BindingFlags.NonPublic);
            var handlerType = (Type)type.Invoke(provider, new object[] { request });

            Assert.Equal(typeof(TestHandler), handlerType);
        }

        /// <summary>
        /// 发布任务为空测试
        /// </summary>
        [Fact]
        public async Task BackgroundTaskProvider_SendNullTest()
        {
            BackgroundTaskQueue.Capacity = 10;
            var provider = new BackgroundTaskProvider();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await provider.Send(null));
        }

        /// <summary>
        /// 发布任务测试
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task BackgroundTaskProvider_SendTest()
        {
            BackgroundTaskQueue.Capacity = 10;
            var queue = BackgroundTaskQueue.Instance();
            var provider = new BackgroundTaskProvider();
            provider._queue = queue;
            var request = new TestRequest();
            await provider.Send(request);

            var e = await queue.DequeueAsync(default);

            Assert.Equal(request.GetHashCode(), e.Request.GetHashCode());
        }

        /// <summary>
        /// 发布重试任务测试
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task BackgroundTaskProvider_SendEnableRetryTest()
        {
            BackgroundTaskQueue.Capacity = 10;
            var queue = BackgroundTaskQueue.Instance();
            var provider = new BackgroundTaskProvider();
            provider._queue = queue;
            var request = new TestRequest();
            await provider.Send(request, true, default);

            var e = await queue.DequeueAsync(default);

            Assert.Equal(request.GetHashCode(), e.Request.GetHashCode());
            Assert.True(e.EnableFalidRetry);
        }

        #endregion

        public class TestRequest: IBackgroundTaskRequest
        {

        }

        public class TestHandler : IBackgroundTaskHandler<TestRequest>
        {
            public Task Handler(TestRequest request, CancellationToken token = default)
            {
                throw new NotImplementedException();
            }
        }
    }
}
