# Shader实现UI遮罩

unity提供的mask组件，在UGUI上做遮罩会出现明显的锯齿，所以有了shader实现UI遮罩方案。

实现原理是遮罩使用显示区域不透明其他部分透明的图片，利用显示图片的uv和遮罩的uv做alpha相乘来得出输出的颜色值。透明部分相乘结果是透明的所以实现遮罩效果。

UI图片有两种单图和图集。

图集的uv信息要正确处理，需要保证shader处理的显示图片uv是显示sprite的uv。可以利用C#脚本获取sprite的uv，然后传给shader使用。

参考：[UGUI基于图集的shader遮罩](https://lianbai.github.io/2021/04/20/Unity%E6%9D%82%E6%96%87/Unity%E6%9D%82%E6%96%87%E2%80%94%E2%80%94UGUI%E5%9F%BA%E4%BA%8E%E5%9B%BE%E9%9B%86%E7%9A%84shader%E9%81%AE%E7%BD%A9/ "UGUI基于图集的shader遮罩")

我们的项目使用的方案原理也一样，只不过看起来有点复杂，应该是兼容各种情况：

[ui\_mask.shader](file/ui_mask_5Nj9YgcVas.shader)

[UGUIUIMaskBlendComponent.cs](file/UGUIUIMaskBlendComponent_bfSV4aQdYI.cs)
