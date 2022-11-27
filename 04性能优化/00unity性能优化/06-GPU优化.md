## GPU

### 渲染流水线

### 工具

* Unity Profiler
* Frame Debug
* PU Profiler
* MeshBaker减低drawcall

### Gpu优化要点

* drawcall
* batching
* 图集
* 移动设备优化

**drawcall**

相当于一次render state的改变

输入参数material、shader、texture、mesh

渲染对象关系，理清楚关系才能着手优化drawcall

shader-》多个Material-》多个MeshRenderer

GPU优化的产出：资源制作规范、规格、资源的做法

**batching**

Dynamic Batching

-   所有Mesh实例具有相同的材质引用
-   Vertex Attribute总数必须小于900（坐标、uv、法线都是一个个属性）
-   所有实例必须采用Uniform Scale或者所有Mesh都采用，Nonuniform Scale,不用混合使用
-   必须引用相同的Lightmap
-   材质Shader不能使用Multiple passes
-   Mesh实例不能接受实时阴影
-   每个Batch最大300Mesh
-   最多32000Mesh可以Batch

Static Batching

-   所有Mesh实例具有相同的材质引用
-   所有Mesh必须标记为static
-   额外的内存占用（会生成一块新的大网格）
-   静态对象无法通过原始的Transform移动
-   任何一点可见，全部渲染

**移动设备优化**

-   最小的Drawcall
-   最小的材质数量
-   最小的纹理尺寸
-   方形 & POT纹理
-   shader中使用尽可能低的数据类型
-   避免Alpha测试