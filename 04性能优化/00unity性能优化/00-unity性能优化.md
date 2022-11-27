# unity性能优化

## 游戏优化目标

1.性能（目的是能容纳更多的用户）

2.品质（在目标平台上能支持多好的品质）

**三大方向:**

1.  内存占用
2.  大小/容量
3.  运行效率

## 硬件层面

从硬件层面可以分为三大部分：

1.CUP，执行时间平稳、减低峰值、分片执行，避免频繁GC

2.内存，资源管理（大小、加卸载）、避免内存碎片、内存泄漏

3.GPU

## 功能模块

从功能模块可以分：

1.渲染模块：半透明渲染耗时和Draw Call、不透明渲染耗时和Draw Call、Triangle，SRP使用

2.逻辑代码：GC调用频率（1000帧/次）、堆内存分配、增量GC功能（可以减低GC的CPU耗时）

3.UI模块：UGUI（Canvas.SendWillRenderCanvases）、NGUI（UIPanel.LateUpdate）,FGUI、Draw Call、重建、OverDraw、主线程阻塞

4.动画模块：Animator.Update、AnimationClipPlayable

5.物理模块：phsics.Processing

6.粒子模块：ParticleSystem.Update、粒子数量、设备分档

## 内存

内存占用方面统计：

1.总体内存：4G内存设备为分界线，4G以下内存占用在800\~900MB之间，4G以上在950\~1.1GB之间。

2.堆内存：100M已经是很高了，重要原因是配置文件的序列化库使用不当，比如不需要启动时加载全部配置。

3.纹理资源内存占用：纹理Mipmap使用情况、渲染利用率、ASTC/ETC格式（ASTC格式取代ETC已成为必然趋势，但海外设备低端机较多的国家仍需考虑使用ETC格式）

4.网格资源：G内存以下设备网格内存在30MB左右，4G内存以上的设备网格内存在40\~50MB之间。

5.动画资源内存：平均峰值内存长期控制在30MB之内。

6.shader资源：Unity 2019.4.21以后Standard Shader在大家的项目中被经常误引入进来。

7.RT资源：对象池重复利用，及时回收。

8.粒子系统资源：平均保持在18\~30MB之间，其最有效的优化方法还是降低粒子系统的使用数量，可以考虑在高端设备上使用GPU Particle新功能。

参考链接：

*   [Unity手游性能优化蓝皮书](https://mp.weixin.qq.com/s/oJnOA8Fmik4nb3nmFZWPDQ "Unity手游性能优化蓝皮书")

*   [Unity移动端游戏性能优化简谱\_UWA学堂 (uwa4d.com)](https://edu.uwa4d.com/course-intro/0/430 "Unity移动端游戏性能优化简谱_UWA学堂 (uwa4d.com)")

*   [基于移动设备的性能优化标准](https://blog.uwa4d.com/archives/UWA_PAnormalvalue.html "基于移动设备的性能优化标准")

*   [性能优化的一些技巧](https://www.jianshu.com/p/8dc6c7af8f33 "性能优化的一些技巧")

*   [Lua性能优化](https://www.cnblogs.com/YYRise/p/7082637.html "Lua性能优化")

*   [安卓使用Unity Profiler真机调试函数耗时瓶颈案例](https://gameinstitute.qq.com/community/detail/128433 "安卓使用Unity Profiler真机调试函数耗时瓶颈案例")

*   [官方Unity优化教程](https://learn.unity.com/tutorial/fixing-performance-problems-2019-3-1#5e85ad9dedbc2a0021cb81aa "官方Unity优化教程")

*   [官方Unity UI优化教程](https://learn.unity.com/tutorial/optimizing-unity-ui#5c7f8528edbc2a002053b5a1 "官方Unity UI优化教程")

*   使用[Profiler诊断性能问题](https://learn.unity.com/tutorial/diagnosing-performance-problems-2019-3#604578bdedbc2a08f8930484 "Profiler诊断性能问题")

*   [优化移动游戏性能：来自Unity顶级工程师的性能分析、内存与代码架构tips](https://blog.unity.com/cn/technology/optimize-your-mobile-game-performance-tips-on-profiling-memory-and-code-architecture "优化移动游戏性能：来自Unity顶级工程师的性能分析、内存与代码架构tips")

*   [移动游戏优化指南](https://learn.u3d.cn/tutorial/mobile-game-optimization?chapterId=63562b28edca72001f21d125#61164663feec0d00200df1da "移动游戏优化指南")中文版
