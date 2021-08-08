# Mango.BackgroundTask
用于AspnetCore的基于IHostedService的后台任务库，该包主要解决了在HTTP请求执行比较长时间的任务时在后台新建线程执行而HTTP马上返回时依赖注入的服务被销毁的问题。

解放双手不再需要做复杂的服务生命周期管理，不再需要自己新建`Scope`

## 引用

搜索并引用包：

> Mango.BackgroundTask

## Startup.cs

```CSharp
service.AddBackgroundTaskQueue();
```

## 开始编写任务

该包模仿`MediatR`的交互方式，首先创建任务请求对象实现`IBackgroundTaskRequest`接口：

```CSharp
public class MyTaskRequest : IBackgroundTaskRequest
{
    //...
}
```

然后创建任务处理类实现`IBackgroundTaskHandler`接口：

```CSharp
public class MyTaskHandler : IBackgroundTaskHandler<MyTaskRequest>
{
    private readonly ISomeService _someService;

    public MyTaskHandler(ISomeService someService)
    {
        //在处理程序中可以像其他普通的服务一样使用依赖注入
        _someService = someService;
    }

    public Task Handler(MyTaskRequest request, CancellationToken token = default)
    {
        //编写处理程序
    }
}
```

然后在`Startup.cs`中注册`MyTaskHandler`：

```CSharp
service.AddScoped<MyTaskHandler>();
```

## 发布任务

在任意需要发布任务的地方利用依赖注入`IBackgroundTaskProvider`接口，像后台线程发布任务。

```CSharp
public class SendTaskService
{
    private readonly IBackgroundTaskProvider _provider;

    public SendTaskService(IBackgroundTaskProvider provider)
    {
        //依赖注入IBackgroundTaskProvider接口
        _provider = provider;
    }

    public async Task SomeMethodAsync()
    {
        //todo...
        //发布任务
        await _provider.Send(new MyTaskRequest());
        //todo...
    }
}
```

## 最后

该包和`MediatR`有什么区别？直接用`MediatR`不能实现吗？

首先因为生命周期的原因在一个http请求结束后依赖注入的服务就有可能会被销毁，所以当需要执行时间长的任务而要求http能马上返回时，就会出现引用了被销毁对象而异常的情况。而像`MediatR`这种中介者模式的库并不会对这种超过生命周期的服务做任何处理，所以大概率任然会发生异常，除非自己手动处理。

该包只是在交互方式上模仿`MediatR`，本身并没有引用到`MediatR`。

也希望大家都可以来维护或者贡献这个小小又实用的包。