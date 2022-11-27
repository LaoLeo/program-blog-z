# 图文混排-支持多个不同emoji来自不同图集和散图

之前针对表情不支持多图集的问题优化过一板，可以回顾下这篇文章：[UIGraphicText组件表情渲染优化-支持表情来之不同图集](./01UGUI%20Text%E7%BB%84%E4%BB%B6%E5%AE%9E%E7%8E%B0%E5%9B%BE%E6%96%87%E6%B7%B7%E6%8E%92%E2%80%94%E2%80%94%E9%A1%B9%E7%9B%AE%E4%BC%98%E5%8C%96.md)

但后来发现图文混排场景不支持多个不同emoji来自不同图集和散图，下面是问题描述。

PC下不打图集下发现，多个不同的emoji标签显示同一个emoji sprite。

![C:\\88534cc8db9f6423fece8da47e598d32](media/4c1118d6cb20ce80f3105b95502455c3.tmp)![C:\\53fba21b5a4b310d26e01fbc92b36a2e](media/7975d77768f66dd5b45d9ef03f066e45.tmp)

## **原因分析**

每个text组件下只分配一个maskableGraphic组件，如其名可遮罩图形，该组件可以根据uv坐标来截取自身mainTexture上的区域，以渲染emoji。

当不打图集，uv坐标就代表整个mainTexture，也就是整张单图，而mainTexture同一时刻只能代表一张单图。

所以在这个例子中，所有的emoji都显示同一张图。

按照这个原理，即使在打图集的情况下，假如一行消息上的emoji并不是都来自同一个图集，那么显示上也会出问题。

## **解决方案**

这个问题的瓶颈在于maskableGraphic组件的数量，可以根据不同的texture来分配maskableGraphic组件来渲染emoji，这样就可以支持同时显示来自不同texture的emoji。

问题分拆：

1.  分组，按sprite的texture的不同来决定分配UIGraphiTextSprites组件（继承maskableGraphic），每个UIGraphiTextSprites组件负责渲染这个texture的emoji。
2.  重构drawSprites方法逻辑以支持分组渲染emoji
3.  处理Update逻辑以支持渲染动图

但实践结果发现，按texture不同来分配maskableGraphic组件的方式并不适用于多个动图（动图即多个sprite切换）的情况，比如不打图集情况下有两个一样的动图emoji，当动图切换sprite texture时候，因为按texture分配组件所以两个一样的动图emoji只分配一个组件，前一个emoji会被后一个emoji抢占组件而不渲染。

![C:\\7a9f240bac4cb3c6c539f8bad0891c12](media/1b068870d1847acdd585381f49f9127c.tmp)![C:\\ad1a89e4b9b73f312118b21ca07bab28](media/b1ea6bccc642c35685b0dd0f5671b303.tmp)

所以最终的方案为：

1.  区分静图和动图的emoji，静图的emoji按texture分配UIGraphiTextSprites组件
2.  动图的emoji分配单独一个UIGraphiTextSprites组件负责渲染
3.  UIGraphicTextSpritesMgr池化UIGraphiTextSprites组件，避免在消息列表界面频繁创建和销毁组件

分配策略代码如下：

展开源码

UIGraphicTextSprites[] m_spriteGraphicArray;

private void DistributeTextSprite(List\<SpriteTagInfor\> listTag)

{

var staticEmojiDict = new Dictionary\<Texture, List\<int\>\>();

for (var i=0;i\<listTag.Count;i++)

{

var tagInfor = listTag[i];

var animTagArray = m_AnimSpriteTag[i];

if (animTagArray.Length\>1)

{

var textSprite = CreateTextSprite();

m_spriteGraphicArray[i] = textSprite;

}

else

{

var sprite = m_nameToSpriteDict[tagInfor.name];

if (!staticEmojiDict.ContainsKey(sprite.texture)) {

staticEmojiDict[sprite.texture] = new List\<int\>(){i};

}

staticEmojiDict[sprite.texture].Add(i);

}

}

// staticEmojiDict：{texture1=\>{1，3}}

foreach (var item in staticEmojiDict)

{

var textSprite = CreateTextSprite();

foreach(var index in item.Value)

{

m_spriteGraphicArray[index] = textSprite;

}

}

}

## **具体实现**

下面以渲染emoji的流程图来讲解讲解改造的过程。

![C:\\97daee51e2a0dadd426d08e8b46ba127](media/ebee4d8b9fab274fef93b95923e20796.tmp)

## **遇到的问题**

1.OnPopulateMesh中控制gameObject的slefActive会导致报错，与onLogCallBack有冲突。

![C:\\c8e64b17c775c1a4b44a6a7a6a6354ce](media/949ad5756a5f0697641e2267e54b6a2a.tmp)

“Trying to add...... while we are already inside a graphic rebuild loop.”这句是UGUI的报错，查看了UGUI的源码发现：

canvas刷新时，会将标记为dirty的UIElement重新构建，graphic类会生成网格时调用OnPopulateMesh。

这个时候Graphic类不能更改，更改会让canvas标记setDirty加入到待构建队列，而且在canvas刷新时不能禁止入队，所以报了这个错。

下面是UGUI的源码：

![C:\\8671ab9a5ec1a5f297a33d24684e119b](media/889d57042c9cc1a988458e23e9617d0d.tmp)![C:\\3b0528cdf5e8756c32cae65ffba1311a](media/1ed8f3c55e63efc72047784eb2d715ec.tmp)![C:\\11e53b162bb563663b032f919afcab4a](media/35ae882a0e5fee85b1b7a102ba20976f.tmp)

即在OnPopulateMesh方法里不能执行setActive方法，可以放在base.SetCVerticesDirty方法里做。

2.重用cell的带sprite的text没有更新文本，只是调用了disable后调了enable，log：

因为是base.SetVertivesDirty方法前提前返回了，没有加入到canvas的待更新队列不会更新图形，所以不能这样做。

下面是引起错误的代码：

![C:\\56a7b0dce321c762d3affc70752c805e](media/83146e7e3d81e80b54edf6131d3203e2.tmp)

## **测试与规范**

兼容性排查，排查修改类被引用到的地方是否正常？

修改了UIGraphicText类和UIGraphicTextSprites类，两者在项目中引用我排查过，变更代码不会引起错误，但UIGraphicTextSprites类属于submodule的框架层，不知其他项目的情况。

测试案例如下：

![C:\\b7c4a0986ebdf75c5ef2bda9f54b15cf](media/584310e308f30ee963734e6004db68f7.tmp)![C:\\b40295cf6f4f0fca2550c102727d03d9](media/4e5d287689f65ab1fab2db8cc848f218.tmp)

观察在各个平台上打不打图集的表现情况：

| **测试平台** | **是否打图集** | **结果（emoji、多个emoji、动图emoji、动图）** |
|--------------|----------------|-----------------------------------------------|
| Unity        | 不打           | 正常                                          |
| Unity        | 打             | 正常                                          |
| 安卓         | 打             | 正常                                          |
| IOS          | 打             | 正常                                          |
