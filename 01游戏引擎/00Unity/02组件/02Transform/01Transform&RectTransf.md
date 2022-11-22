# Transform&RectTransform

每个Object都默认有transform组件，定义在scene中的几何物体的相关信息

Rect transform表示UI元素中的矩形，可以控制UI元素相对其父元素的位置和大小。

- posX、posY、posZ表示相对其锚点的坐标
- width、height表示宽高。假如子元素相对父元素拉伸，宽高不能设置。
- Anchors min和max表示锚点的四个手柄，值为百分比，范围为父元素宽高
- Pivot表示当前元素跟锚点对比的定位点，posX和posY作用于这个点，值为百分比，范围是当前元素的宽高。
- 锚点的四个手柄的分开可以锚定子元素相对父元素的大小和位置，可以影响left、top、right、bottom，width，height属性

*四个手柄两两聚合或者都聚合为一个点，那么可以控制posX和posY定位子元素

详细参考：[https://blog.csdn.net/vv_017/article/details/79107900](https://blog.csdn.net/vv_017/article/details/79107900)

[https://www.jianshu.com/p/dbefa746e50d](https://www.jianshu.com/p/dbefa746e50d)



[ 坐标轴](%C2%A0%E5%9D%90%E6%A0%87%E8%BD%B4/%C2%A0%E5%9D%90%E6%A0%87%E8%BD%B4.md)

