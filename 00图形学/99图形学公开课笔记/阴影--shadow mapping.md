## shadow mapping：解决在光栅化化上画出阴影

![clipboard.png](media/c63e8439cb6066cbd70f774df359ade6.png)

属于一个图像空间算法，不需要知道几何信息，但存在走样问题

关键点：

-   一个点不在阴影内，那肯定会被光源和摄像机看见

## 步骤

①以光源位置假设有摄像机，生成深度图

![clipboard.png](media/372e985b0cc67d871d2c48889d5e861c.png)

②A在真实摄像机上生成带深度的标准图

![clipboard.png](media/8d63216499d95224b90fe341c531c2b2.png)

②B将标准图的点投影回光源生成的深度图上，对比深度，相近（浮点数相等问题）的则不在阴影内，不同则在。

![clipboard.png](media/0595c448b1b5f111f9c925cc0bac86a2.png)

## 例子

![clipboard.png](media/306724528be06fe49f2229a9eb73968a.png)

①光源处生成的深度图

![clipboard.png](media/45f27ef6f9a33e799f4efc22443b1f07.png)

②摄像机处从生成像素上投影到深度图上的点，对比深度，生成shadow mapping

![clipboard.png](media/aed74f4d4895f86779baf19ee86fd0cb.png)

-   绿色上的点表示光源看到的点
-   非绿色点表示阴影

## 问题

![clipboard.png](media/affa95c36d597586a1c607764fed38b4.png)

-   属于硬阴影，仅支持点光源
-   质量依赖shadow mapping分辨率
-   浮点数比较问题

## 软阴影和硬阴影的对比

![clipboard.png](media/b6cc5eb77fef9f4f6453112ebcf0a1d1.png)
