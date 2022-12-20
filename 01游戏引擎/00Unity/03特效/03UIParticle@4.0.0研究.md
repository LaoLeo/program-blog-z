# UIParticle\@4.0.0研究

## 背景

UIParticle刚开始引进项目中的版本是@3.3.10，发现的问题比较多，材质数不能超8个的问题也在github上提了[bug#122](https://github.com/mob-sakai/ParticleEffectForUGUI/issues/122 "bug#122")。

作者最近升级UIParticle到v4，解决了一些问题和增加了新特性，其中包括bug#122。

所以来研究一下，这个新版本的优化内容对项目有没有价值，考虑是否有必要升级版本。

## bug修复

1.submesh can not over 8问题修复

即使是使用很多材质的特效，都没有输出警告了。

2.使用meshRenderer和uv动画实现的烟雾渲染层级不在UI layer问题

使用meshRenderer的粒子不会被UIParticle渲染，因为UIParticle使用了新类UIParticleRenderer进行渲染，同样不会处理非粒子系统下的网格和材质。

3.共享材质问题

动画修改材质属性，共享材质不会同步变化，估计是内部实现已经不使用sharedMaterial属性。

4.ParticleSystem的所有的粒子都能被UIParticle渲染出来吗？

还是拿家具抽奖的那个特效ui\_jiajuchoujiang来测试，依然有部分没有渲染出来，而且跟渲染顺序无关。

5.勾选ignoreCanvasScale后不同分辨率下大小不对问题

放弃了ignoreCanvasScale属性，重构了ui缩放逻辑，所以这么问题应该是解决了。

## 新特性

1.UI自适应缩放

也就是上述第5点。

2.新增吸引子组件

可以吸引粒子沿着某种路径到某个位置，属于效果扩展上的新特性。

3.网格共享组

渲染大量相同粒子时可以使用相同的网格共享组，属于性能优化上的改进。

4.add overlay window

overlay window是unity2021.2开始提供的scene window的覆盖窗口特性，可以自定义菜单，unity2020没有这个特效。

另外Inspector面板做了优化，去掉了IgnoreCanvasScale属性，新增了mesh sharing接口，也支持输出一些警告。

详细看看[更新日志](https://github.com/mob-sakai/ParticleEffectForUGUI/releases "更新日志")。

**有个问题就是，升级后需要对UIparticle特效的scale重新调整，这点比较麻烦。**
