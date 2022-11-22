应用纹理

简单的纹理映射

![clipboard.png](media/8f0c421ef5ee37d0b65283f77fe71ab6.png)

-   每个上的每个像素点都有坐标x y
-   每个x y点通过重心坐标插值得到纹理坐标u v
-   在纹理上采样出u v的颜色
-   对颜色应用blinn-phong关照模型（通常是漫反射系数）

上述过程即是纹理贴在模型上的过程。

纹理反走样

纹理放大texture magnification（假如一个像素中的纹理太小）

![clipboard.png](media/420be1eccb7f67c3cb04850d0a0f45db.png)

即将小图放大的效果，学术名叫纹理分辨率不足

三种解决方法

-   Nearest（接近值，四舍五入）
-   Bilinear（双线性插值）
-   Bicubic

Bilinear

![clipboard.png](media/770a8ad8bcbb26491dac64c54a98fd01.png)

-   找出周围四个纹素
-   通过四个纹素的uv值做水平和竖直方向的插值

Bicubic

-   通过找出周围16个纹素，做双线性插值。
-   效果比bilinear要好，但是开销更大。

纹理放大（假如一个像素占的纹理太大）

![clipboard.png](media/b3390c2022f2275106943f015824a61d.png)

点采样纹理存在的问题：

-   摩尔纹
-   锯齿

点采样纹理解析：指应用纹理的方法是通过像素中心查询纹理坐标，将纹理颜色值应用到像素。

产生的原因可以用之前走样的原因解析：

![clipboard.png](media/29f3e8543eb0631e0c85c611b0c76834.png)

一个像素内占据的信号变化过快（包含区域过大），采样速度跟不上（一个点代表不了这么大的区域）

用反走样的方法：超采样解决

![clipboard.png](media/0cbae8a0c411782c46d9663ff428ce3e.png)

每个像素用512个采样点的结果。

超采样起作用的原因和另一种解决办法--区域查询

![clipboard.png](media/2d42865503a171fa5f5aea9d89ad5866.png)

假如可以快速得出一块纹理区域的平均值（能代表区域的信号变化），也可以解决问题。

点查询vs范围查询

-   点查询：从一个像素映射到一个纹理坐标的值
-   范围查询：从一个像素映射到一个纹理区域的平均值

Mipmap

可以做快速的、近似正方形的范围查询

![clipboard.png](media/47980467a4598200f10bc6d853e12b05.png)

从一张原始图每次分辨率变成之前的四分一生成一系列的图，存储大小只是增加了原来的三分一。

每一层叠加组成纹理金字塔：

![clipboard.png](media/b7aab6f4392e29711e0bfc9175bc12b0.png)

![clipboard.png](media/54e070945f5c66e87ccfeaa815bb6b10.png)

![clipboard.png](media/c40aae9effc5d713404b37ec94854678.png)

原理：

-   像素点映射到纹理上的区域，取上右两点间距离最大的L，画出像素在纹理上的近似正方形
-   用L计算出查询的层数D

D的计算公式解析：假如映射在纹理的正方形边长为1，则在第0层查；假如是2，则是1成查；假如是4，则是2层查。由此得出。

得出结果

![clipboard.png](media/8a714b1b95a5722da077a08058381f5c.png)

但是由于查询的层是离散的，所以有不连续的效果。

怎么解决呢？三线性插值

![clipboard.png](media/10351fb4d77d317fc898109c57f62853.png)

在相邻两层查通过双线性插值得出的结果，再做一次插值。

得到的结果会更加连续：

![clipboard.png](media/51e39f7e7565244972753d84f9fe146c.png)

两种方法的结果对比：

超采样

![clipboard.png](media/de083bb6756be88f127c997fb6035bdb.png)

mipmap结果：

![clipboard.png](media/804ae5c03deeea381593905d858cd8c6.png)

mipmap的局限会造成overblur。

另一种更好的方法：各向异性过滤（anisotropic filtering）

![clipboard.png](media/3138c67364f2bfa8200e765b463997d8.png)

通过对比，可以知道各向异性过滤包含的图比mipmap多了横向和纵向的拉伸的矩形区域，比只能查询正方形区域的mipmap覆盖的查询区域更多。

像素点映射到纹理的区域并不都能近似成正方形：

![clipboard.png](media/dccb5bd8fd790140a0c028f3bff27bbc.png)

EWA filtering：包含处理前两种区域，还能够处理不规则的矩形

![clipboard.png](media/bbae1efcf22e16e71a9f142891e3636a.png)
