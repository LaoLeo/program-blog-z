### 渲染流水线
![image](https://raw.githubusercontent.com/LaoLeo/ImageBed/master/graphics/renderPipeline.png)
详细请参考：[unity-shaders-book.pdf](http://candycat1992.github.io/unity_shaders_book/unity_shaders_book_images.pdf)

## Shader
### 模型组成
![image](https://github.com/LaoLeo/ImageBed/blob/master/graphics/modelComponent.png?raw=true)
### Shader、OpenGL和DirectX的关系
- OpenGL是3D程序接口，DirectX是应用程序接口，两者都是用于渲染的二维或三维图形应用程序编程接口，链接应用程序和GPU。
- shader着色器，有shader语言实现
    - GLSL shader语言用于OpenGL
    - HLSL shader语言用于DirectX
    - CG语言可以调用这两个API
- 应用程序->shader语言->OpenGL/DirectX->显卡驱动程序->GPU

### Unity Shader分类
Unity Shader在CG语言上封装，叫做shaderLab语言，能够忽略实现shader时的一系列工作（选用图形接口，设置和编写顶点/片元着色器，渲染顺序和渲染状态），直接写shader程序就可以实现渲染效果。
![image](https://raw.githubusercontent.com/LaoLeo/ImageBed/master/graphics/unityShader.png)
- 表面着色器 Surface Shader：在顶点
- 顶点/片元着色器 Vertex/Fragment Shader
- 固定函数着色器 Fixed Function Shader

### shaderLab语法
```
Properties{
    //属性
    _Color("Color", Color)=(1,1,1,1)
    _Vector("Vector",Vector)=(1,2,3,4)
    _Int("Int",Int)=12345
    _Float("Float",Float)=1.123
    _Range("Range",Range(1,100))=50
    _2D("Texture",2D) = "red"{}
    _Cube("Cube",Cube)= "white"{}
    _3D("Texture",3D)= "black"{}
}

// subShader可以写多个，显卡运行会从第一个subShader开始检查效果是不是都能实现，应用第一个检查通过的SubShader，都不通过则使用Fallback版
SubShader{
    //至少一个pass，通道
    Pass{
        //在这里编写shader代码，可以写HLSLPROGRAM
        CGPROGRAM
        //使用CG语言编写shader代码
        // 声明属性
        float4 _Color; // float(32位存储) half(16位) fixed（11位）
        float3 t3;
        float2 t2;
        float t;
        sampler2D _2D;
        samplerCube _Cube;
        sampler3D _3D;
        
        //声明顶点函数
        //顶点函数，完成模型顶点从模型空间到屏幕空间上的转换
        #pragma vertex vert
        //声明片元函数
        //片元函数，输出屏幕上每个像素的颜色值
        #pragma fragment frag
        
        struct a2v {
            float4 vertex:POSITION;//告诉unity把模型空间下的顶点坐标填充给vertex
            float3 normal:NORMAL;//把模型空间下的法线方向填充给normal
            float4 texcoord:TEXCOORD0;//把第一套纹理坐标填充给texcoord
        }
        //利用结构体，将参数从顶点函数传递给片元函数
        struct v2f {
            float4 position:SV_POSITION;//SV_POSITION裁剪空间坐标
            float3 temp:COLOR0;
        }
        
        v2f vert(a2v v){
            v2f f;
            f.position = mul(UNITY_MATRIX_MVP,v.vertex);
            f.temp = v.normal;
            return f
        }
        fixed4 frag(v2f f) : SV_Target{//SV_Target像素颜色值
            return fixed4(f.temp, 1)
        }
        
        ENDCG
    }
    
    Fackback "VertexLit"
}
```
从应用程序传递到顶点函数的语义有哪些a2v
- POSITION 顶点坐标（模型空间）
- NORMAL 法线（模型空间）
- TANGENT 切线（模型空间）
- TEXCOORD0 ~ n 纹理坐标uv
- COLOR 顶点颜色

从顶点函数传递给片元函数时可以用的语义v2f
- SV_POSITION 裁剪空间中的顶点坐标
- COLOR 可以传递一组（fixed3），相对于一个寄存空间
- COLOR0~3 可以传递一组值4个
- TEXCOORD0 ~ 7传递纹理坐标

片元函数传递给系统
- SV_Target 颜色值，显示到屏幕上的颜色

### shader demo

岗位应用shader
- 不是要求游戏前端都必须精通，写shader是TA的主要工作、
- 小公司会直接使用市面上的shader，大公司有专门写shader的TA。

### 光照模型
> 什么是光照模型？

光照模型就是一个公式，用来计算某个点的光照效果。光照模型跟显示中光的计算不一样，是个模拟的过程，不用纠结。

标准光照模型  
在标准光照模型里面，我们吧进入摄像机的光分为下面四个部分：
- 自发光
- 高光反射 
- 漫反射：diffuse=直射光颜色 * max(0, cosθ)，θ：入射光和法线的夹角
- 环境光

Blinn光照模型（标准光照模型）
高光反射：直射光 * pow(max(cosθ, 0), 10),θ：反射光方向和视野方向的夹角

Blinn-phong光照模型（常用）
高光反射：直射光 * pow(max(cosθ, 0), 10),θ：平行光和视野方向的平分向量

光颜色的融合是用乘法，叠加效果用加法。
```
// 入射光颜色与设置的颜色融合
fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb;
fixed3 diffuse = _LightColor0.rgb * max(dot(worldNormal, lightDir), 0) * _Diffuse.rgb;
// 漫反射光与环境光叠加
f.color = diffuse + ambient;
```

### unity内置变量和方法
- [UnityShaderVariables](https://docs.unity3d.com/Manual/SL-UnityShaderVariables.html)
- [BuildInFunctions](https://docs.unity3d.com/Manual/SL-BuiltinFunctions.html)
- [shaderLab框架手册](https://shenjun4shader.github.io/shaderhtml/)

### 法线贴图
定义：用贴图像素的rgb值记录法线矢量信息，应用于模型网格上会影响光照效果从而表现表面光滑凹凸细节。

法线矢量值范围[-1，1],rbg取值范围[0,1]，将rgb颜色转换为矢量方向，必须乘以2，再减去1。例如，RGB 值 (0.5, 0.5, 1) 或十六进制的 #8080FF 将得到矢量 (0,0,1)，即用于法线贴图的向上方向，表示光照对表面无影响。
```
//获取法线贴图上的法线值
fixed4 normalColor = tex2D(_normalColor, f.uv.zw);
fixed3 tangentNormal = UnpackNormal(normalColor);
tangentNormal = normalize(tangentNormal);
```

### 切线空间
...