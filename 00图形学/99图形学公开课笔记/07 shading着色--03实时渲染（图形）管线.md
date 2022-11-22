实时渲染管线

![clipboard.png](media/933cc06a7a3b45e74cd83dc480d82324.png)

-   顶点处理：将3D中的顶点投影到屏幕空间
-   三角形处理：找出顶点组成的三角形
-   光栅化：采样出被每个三角形覆盖的像素部分（片段）
-   片段（像素）处理：对片段进行着色
-   framebuffer操作：显示图像

变换发生阶段

![clipboard.png](media/64b65e13cd6e263fb8d3da854f209a14.png)

采样三角形覆盖阶段

![clipboard.png](media/c0a2e91b888c3e58b779c6045412ae85.png)

z-buffer深度测试

![clipboard.png](media/5533e6fa1fd56b6be94d69df28e13aad.png)

shading阶段

![clipboard.png](media/3b15481636c25d37e1515768227dc8cf.png)

考虑到不同的着色频率，shading可以反生在上述两个阶段。

shader编程

-   发生在顶点和片段处理阶段
-   描述每个顶点或者片段的处理方式

![clipboard.png](media/cef8a20f8a710ea7be769a63e6e564b5.png)

shader作品的网站：<https://www.shadertoy.com/>

纹理映射（Texture Mapping）

我们需要用来定义一个点的不同属性，比如颜色。

![clipboard.png](media/8c08c23e6e5210b9688d15e3f81eb15f.png)

纹理：物体的表面其实是2D的，展开来就是一张图。

![clipboard.png](media/9be689994d495c7e90a500ea7f1b2608.png)

-   空间上的一个三角形映射到纹理上的一个块
-   换句话说是三角形的顶点映射到纹理上的坐标（u，v）

![clipboard.png](media/b2e409f9ceb365bc5e46c2233cd5bd88.png)

-   u，v的大小都是0到1
-   纹理可以被重复使用多次（瓦块）

纹理与着色的区别：纹理是着色时候定义的每个点的不同的属性。

着色与材质：不同材质就是不同的着色方法。
