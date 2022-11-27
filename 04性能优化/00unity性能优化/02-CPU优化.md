## CPU

### 性能分析方法

流程：90%分析-》10%解决-》下一个问题，再分析，解决

分析前：

1.去干扰

内部干扰：profiler，Vertical Sync（显卡绘制相关，显示器和显卡处理同步），Log output，避免游戏别的问题的影响

外部干扰：CPU，内存，IO，系统别的进程、读写，网络的影响等

2.使用工具

-   Unity Profiler
-   Custom Profiler
-   Timer & Log

    Unity Profiler

1.  先deep Profiler分析出问题大概范围UnityEngine.Profiling.Profiler.BeginSample("CustomProfile");UnityEngine.Profiling.Profiler.endSample("CustomProfile");
2.  再使用自定义profiler输出分析出具体问题

Log

继承Dispose的计时器类 初始化时初始化Stopwatch 在Dispose时利用Stopwatch的Stopwatch.ElapsedMillisecodes属性算出代码段消耗的时间;

### 

### CPU优化要点

#### 性能慢的原因

这个从官方Unity性能优化拓展。

主要两个大原因：

1.CPU执行了**慢指令**。指令执行也有快慢之分，比如计算平方根。

2.CPU执行了**过多的指令**。有些源代码编译出太多的指令，或者执行了不必要的指令。

从这两个大方向出发，Unity代码执行慢的情况有：

1.代码结构不良造成浪费。重复调用同一函数，而函数其实只需调用一次。

2.代码虽然简短或结构良好，但却调用开销大的引擎API（慢指令）。开销大的unity API。

3.代码虽然是高效的，但却在不需要时调用了。比如移除视线的物体更新。

4.代码的算法要求高。比如需要大量模拟计算的AI，优化方向是并行、分时分帧或者提前计算和模拟数据缓存（寻路，可以提前计算好路径，或者使用模拟路径）。

### 1.Untiy脚本最佳做法

**1.1Component的获取和缓存**
```
GetComponent\<CompTest\>() 最快 (CompTest)GetComponent("TestComponent") 最慢 (CompTest)GetComponet(typeof(CompTest)) 比最快慢一丟丟 ------- 
private Renderer myRenderer;
void Start() { 
    myRenderer = GetComponent\<Renderer\>(); 
} 
void Update() { 
    ExampleFunction(myRenderer);
}
```
**1.2移除空声明**

Update，Start等MonoBehavior钩子函数，Unity在执行这些生命周期函数时会进行一些安全检查，以确保GameObject处于有效状态、未被销毁等

**1.3避免Find和SendMessage、BroadcastMessage**

最快是使用引用的方式获得脚本组件的对象

SendMessage在native层调unity逻辑可以使用，其他情况一般不使用，太慢了

消息的发送可以直接使用静态类，或者单例组件，或自定义消息系统**Events**或**Delegates**。

SendMessage、BroadcastMessage适用原型设计功能，是使用反射实现的，在运行时检查并解析调用，多出了很多cpu指令，尽量不要使用。

**1.4仅在运行时运行需要的代码**

**a. 禁用未使用的游戏脚本及对象**

生成周期：生命周期结束及时distroy

可见性：不可见的不执行更新，disable，unactive

距离：不在距离内的不更新

**b.尽量将代码移除循环，尤其在Update函数中**
```
void Update() { 
    for(int i = 0; i \< myArray.Length; i++) {
        if(exampleBool) { 
            ExampleFunction(myArray[i]); 
        } 
    } 
} 
// 改为以下，当需要时才执行迭代： 
void Update() { 
    if(exampleBool) { 
        for(int i = 0; i \< myArray.Length; i++) {  
            ExampleFunction(myArray[i]); 
        } 
    } 
}
```
**c.仅在事情发生变化时才执行代码**
```
private int score; 
public void IncrementScore(int incrementBy) { 
    score += incrementBy; 
} 
void Update() { 
    DisplayScore(score); 
} 
//改为以下： 
private int score; 
public void IncrementScore(int incrementBy) { 
    score += incrementBy; 
    DisplayScore(score); 
}
```
**1.5对象池**

避免频繁的对象创建和销毁，降低内存碎片化

**1.6分时分帧**

有些计算不必每帧计算，允许延后计算——1s执行一次。
```
void Update() { 
    ExampleExpensiveFunction(); AnotherExampleExpensiveFunction(); 
}
//改为以下，将压力分散到不同帧中，避免尖峰： 
private int interval = 3; 
void Update() { 
    if(Time.frameCount % interval == 0) { 
        ExampleExpensiveFunction(); 
    } else if(Time.frameCount % 1 == 1) { 
        AnotherExampleExpensiveFunction(); 
    } 
}
```
**1.7尽量减少GC**

单独一节讲解。

**1.8开销大的API或者访问器**

托管代码和引擎代码通讯开销较大。

有些getter实现是每访问一次计算一次。

Unity 耗时大的API有：

**a.Transition**

Transform.position获取位置总会重新计算一次，应该缓存起来或者使用localPosition。

Transform位置和旋转的变换会触发*OnTransformChanged*事件，假如对位置等进行update操作，进来用一个Vector3变量存储起来，对这个变量进行Update，完毕后在赋值回给Transition.position。

**b.Vector2和Vector3**

平方根是的指令很慢，所以尽量避免使用Vector2.magnitude和Vector3.magnitude，Vector2.Distance和Vector3.Distance的幕后也是用来magnitude，应该使用Vector2.sqrMagnitude和Vector3.sqrMagnitude替换。

**c.Camera.main**

遍历所有摄像机找出tag为main的摄像机，应该缓存起来或手动管理相机的引用。

其他开销大的unity API参考官方优化教程。

### 2.数据结构优化

只有清楚各种数据结构的优缺点，才可能找出其适用场景。

-   Array、List、Dictionary

List和Dictionary底层实现是Array，易操作，缺点是动态扩容和复制旧数据耗性能

Array需要自己维护数据，内存连续检索快，修改好操作但插入和删除不好操作；定长，过长造成浪费，过短会有溢出。

-   Stack
-   Queue
-   ArrayList，允许插入不同类型元素，当object类型，非类型安全，存在装箱拆箱操作消耗大

详细请参考微软官方指导[集合和数据结构](https://learn.microsoft.com/en-us/dotnet/standard/collections/#choose-a-collection) 和 [选择集合类型](https://learn.microsoft.com/en-us/dotnet/standard/collections/selecting-a-collection-class)。


### 3.算法优化

-   时间复杂度
-   密集计算分布式（多线程，分时分帧）
-   缓存（空间换时间）


### 性能分析注意事项

1.  分析第一，优化第二
2.  分析要透彻，直达底层原理，避免一知半解盲目优化
3.  避免为性能分析增加过多临时代码
4.  增加的日志在优化完成及时清除
5.  尽量通过Debug发现问题