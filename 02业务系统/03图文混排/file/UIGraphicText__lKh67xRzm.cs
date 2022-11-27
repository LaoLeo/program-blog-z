using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using LuaInterface;
using Framework;
using System;

//[ExecuteInEditMode]
using Pjg;


public class UIGraphicText : Text, IPointerClickHandler
{
    /// <summary>
    /// 需要显示的链接文字颜色
    /// </summary>
    public string HyperLinkTextColor = "#FF0000";
	/// <summary>
	/// 下划线颜色
	/// </summary>
	public string UnderlineColor = "#0000FF";
    /// <summary>
    /// 动态表情切换的时间间隔
    /// </summary>
    public float DynamicTagSwitchInterval = 0.2f;
    /// <summary>
    /// 用正则取标签属性 名称-大小-宽度比例
    /// </summary> 
    private static readonly Regex m_spriteTagRegex =
          new Regex(@"<quad name=(.+?) size=(\d*\.?\d+%?) width=(\d*\.?\d+%?) des=(.+?) />", RegexOptions.Singleline);
    /// <summary>
    /// 需要渲染的图片信息列表
    /// </summary>
    private List<InlineSpriteInfor> listSprite;
    /// <summary>
    /// 标签的信息列表
    /// </summary>
    private List<SpriteTagInfor> listTagInfor;
    /// <summary>
    /// 图片渲染组件
    /// </summary>
    private UIGraphicTextSpritesMgr m_spriteGraphicMgr;
    private UIGraphicTextSprites m_spriteGraphic;
    private Dictionary<Texture, UIGraphicTextSprites> m_textureToTextSpriteCompDict = new Dictionary<Texture, UIGraphicTextSprites>();
    private Dictionary<UIGraphicTextSprites, List<int>> m_textGraphicToIndexDict = new Dictionary<UIGraphicTextSprites, List<int>>();
    /// <summary>
    /// CanvasRenderer
    /// </summary>
    private CanvasRenderer m_spriteCanvasRenderer;
	/// <summary>
	/// 图片Mesh，只创建一个，每次刷新Sprites复用现有Mesh
	/// </summary>
	private Mesh m_spriteMesh;
    private Dictionary<string, Sprite> m_nameToSpriteDict = new Dictionary<string, Sprite>();

    #region 动画标签解析
    List<int> m_AnimIndex;
    Dictionary<int, SpriteTagInfor[]> m_AnimSpriteTag;
    Dictionary<int, InlineSpriteInfor[]> m_AnimSpriteInfo;
    Dictionary<int, int> m_AnimSpriteStep;
    #endregion

    /// <summary>
    /// 初始化 
    /// </summary>
    protected override void OnEnable()
    {
        //在编辑器中，可能在最开始会出现一张图片，就是因为没有激活文本，在运行中是正常的。可根据需求在编辑器中选择激活...
        base.OnEnable();
        //对齐几何
        alignByGeometry = true;
        if (m_spriteGraphic == null)
            m_spriteGraphic = GetComponentInChildren<UIGraphicTextSprites>();
        if (m_spriteCanvasRenderer == null && m_spriteGraphic != null)
        {
            m_spriteGraphic.onLoadCallback += OnSpriteAssetLoaded;
            m_spriteCanvasRenderer = m_spriteGraphic.GetComponentInChildren<CanvasRenderer>();
        }
        if (m_spriteGraphicMgr==null)
            m_spriteGraphicMgr = UIGraphicTextSpritesMgr.Instance;
    }

    private void OnSpriteAssetLoaded()
    {
        SetVerticesDirty();
    }

    /// <summary>
    /// 在设置顶点时调用
    /// </summary>】、
    //解析超链接
    string m_OutputText;
    bool m_hasAnimTag;
	private List<string> setVerticesDirtyListName;
	private List<bool> setVerticesDirtyListIsEmpty;
	public override void SetVerticesDirty()
    {
        // base.OnEnable会调用SetVerticesDirty()

        //解析超链接
        m_OutputText = GetOutputText();


        //解析标签属性
        listTagInfor = new List<SpriteTagInfor>();
        m_AnimIndex = new List<int>();
        m_AnimSpriteTag = new Dictionary<int, SpriteTagInfor[]>();
        m_AnimSpriteStep = new Dictionary<int, int>(); 
        m_nameToSpriteDict.Clear();
        m_hasAnimTag = false;

        foreach (Match match in m_spriteTagRegex.Matches(m_OutputText))
        {
            #region 解析动画标签
            if (setVerticesDirtyListName == null) setVerticesDirtyListName = new List<string>();
			else setVerticesDirtyListName.Clear();

			if (setVerticesDirtyListIsEmpty == null) setVerticesDirtyListIsEmpty = new List<bool>();
			else setVerticesDirtyListIsEmpty.Clear();

			string name = match.Groups[1].Value;
            string[] cinListName = name.Split('#');
            for (int j = 0; j < cinListName.Length; j++)
            {
                var found = false;
                if (m_spriteGraphic != null && m_spriteGraphic.spriteAsset != null)
                {
                    var listSpriteInfor = m_spriteGraphic.spriteAsset.listSpriteInfor;
                    for (int i = 0; i < listSpriteInfor.Count; i++)
                    {
                        if (listSpriteInfor[i].name.Contains(cinListName[j]))
                        {
                            found = true;
                            setVerticesDirtyListName.Add(listSpriteInfor[i].name);
                            // 索引表情来自的图集
                            m_nameToSpriteDict[cinListName[j]] = listSpriteInfor[i].sprite;
                            break;
                        }
                    }
                }
                if (!found)
                {
                    setVerticesDirtyListName.Add(cinListName[j]);
                }
                setVerticesDirtyListIsEmpty.Add(!found);
            }
            if (setVerticesDirtyListName.Count > 0)
            {
                SpriteTagInfor[] tempArrayTag = new SpriteTagInfor[setVerticesDirtyListName.Count];
                for (int i = 0; i < tempArrayTag.Length; i++)
                {
                    tempArrayTag[i] = new SpriteTagInfor();
                    tempArrayTag[i].name = setVerticesDirtyListName[i];
                    tempArrayTag[i].index = match.Index;
                    tempArrayTag[i].size = new Vector2(float.Parse(match.Groups[2].Value) * float.Parse(match.Groups[3].Value), float.Parse(match.Groups[2].Value));
                    tempArrayTag[i].Length = match.Length;
                    tempArrayTag[i].isEmptySprite = setVerticesDirtyListIsEmpty[i];

                    if (match.Groups[4].Value == "0")
                    {
                        tempArrayTag[i].isButton = false;
                        tempArrayTag[i].des = "";
                    }
                    else
                    {
                        tempArrayTag[i].isButton = true;
                        tempArrayTag[i].des = match.Groups[4].Value;
                    }
                }
                listTagInfor.Add(tempArrayTag[0]);
                m_AnimSpriteTag.Add(listTagInfor.Count - 1, tempArrayTag);
                m_AnimSpriteStep.Add(listTagInfor.Count - 1, 0);
                m_AnimIndex.Add(listTagInfor.Count - 1);
                if (tempArrayTag.Length>1) m_hasAnimTag = true;
            }
            #endregion
        }

        if (m_spriteGraphic!=null&&m_spriteGraphic.spriteAsset!=null&& m_spriteGraphicMgr!=null)
        {
            // 重置回收节点下所有UIGraphicTextSprites
            var listTextSprites = GetComponentsInChildren<UIGraphicTextSprites>();
            m_textureToTextSpriteCompDict.Clear();
            for (int i=0; i<listTextSprites.Length; i++)
            {
                DisposeTextSprite(listTextSprites[i]);
            }
            // 统一分配，静图按texture分配，动图各占一个UIGraphicTextSprites
            if (listTagInfor.Count>0) 
            {
                m_spriteGraphicArray = new UIGraphicTextSprites[listTagInfor.Count];
                DistributeTextSprite(listTagInfor);
            }
        }

        base.SetVerticesDirty();
    }

    UIGraphicTextSprites[] m_spriteGraphicArray;
    private void DistributeTextSprite(List<SpriteTagInfor> listTag)
    {
        var staticEmojiDict = new Dictionary<Texture, List<int>>();
        for (var i=0;i<listTag.Count;i++)
        {
            var tagInfor = listTag[i];
            var animTagArray = m_AnimSpriteTag[i];
            if (animTagArray.Length>1) 
            {
                var textSprite = CreateTextSprite();
                m_spriteGraphicArray[i] = textSprite;
                
            }
            else
            {
                var sprite = m_nameToSpriteDict[tagInfor.name];
                if (!staticEmojiDict.ContainsKey(sprite.texture)) {
                    staticEmojiDict[sprite.texture] = new List<int>(){i};
                }
                staticEmojiDict[sprite.texture].Add(i);
            }
        }
        // staticEmojiDict : {texture1=>{1，3}}
        foreach (var item in staticEmojiDict)
        {
            var textSprite = CreateTextSprite();
            foreach(var index in item.Value)
            {
                m_spriteGraphicArray[index] = textSprite;
            }
        }
    }

    private UIGraphicTextSprites CreateTextSprite()
    {
        if (Array.IndexOf(m_spriteGraphicArray, m_spriteGraphic) < 0) {
            return m_spriteGraphic;
        }
        
        UIGraphicTextSprites textSpriteComp;
        textSpriteComp = m_spriteGraphicMgr.Distribute(m_spriteGraphic.gameObject, transform);
        textSpriteComp.transform.SetParent(transform);
        textSpriteComp.transform.localPosition = m_spriteGraphic.transform.localPosition;
        return textSpriteComp;
    }

    private void DisposeTextSprite(UIGraphicTextSprites textSpriteComp)
    {
        if (textSpriteComp==null) return;
        if (textSpriteComp != m_spriteGraphic)
        {
            m_spriteGraphicMgr.Dispose(textSpriteComp);
        }
        else
        {
            m_spriteGraphic.SetMainTexture(null);
            m_spriteGraphic.SetAllDirty();
        }
    }  

    readonly UIVertex[] m_TempVerts = new UIVertex[4];
    /// <summary>
    /// 绘制模型
    /// </summary>
    /// <param name="toFill"></param>
    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        //  base.OnPopulateMesh(toFill);

        if (font == null)
            return;

        // We don't care if we the font Texture changes while we are doing our Update.
        // The end result of cachedTextGenerator will be valid for this instance.
        // Otherwise we can get issues like Case 619238.
        m_DisableFontTextureRebuiltCallback = true;

        Vector2 extents = rectTransform.rect.size;

        var settings = GetGenerationSettings(extents);
        cachedTextGenerator.Populate(m_OutputText, settings);

        Rect inputRect = rectTransform.rect;

        // get the text alignment anchor point for the text in local space
        Vector2 textAnchorPivot = GetTextAnchorPivot(alignment);
        Vector2 refPoint = Vector2.zero;
        refPoint.x = (textAnchorPivot.x == 1 ? inputRect.xMax : inputRect.xMin);
        refPoint.y = (textAnchorPivot.y == 0 ? inputRect.yMin : inputRect.yMax);

        // Determine fraction of pixel to offset text mesh.
        Vector2 roundingOffset = PixelAdjustPoint(refPoint) - refPoint;

        // Apply the offset to the vertices
        IList<UIVertex> verts = cachedTextGenerator.verts;
        float unitsPerPixel = 1 / pixelsPerUnit;
        //Last 4 verts are always a new line...
        int vertCount = verts.Count - 4;

        toFill.Clear();

        //清楚乱码
        for (int i = 0; i < listTagInfor.Count; i++)
        {
            //UGUIText不支持<quad/>标签，表现为乱码，我这里将他的uv全设置为0,清除乱码
            for (int m = listTagInfor[i].index * 4; m < listTagInfor[i].index * 4 + 4; m++)
            {
                //超出可视范围的不会绘制，即leftBottomIndex >= verts.Count。
                //所以这里不需要处理也不应该处理。若处理，则数组越界。
                if (m >= verts.Count)
                {
                    break;
                }
                UIVertex tempVertex = verts[m];
                tempVertex.uv0 = Vector2.zero;
                verts[m] = tempVertex;
            }
        }
        //计算标签   其实应该计算偏移值后 再计算标签的值    算了 后面再继续改吧
        //  CalcQuadTag(verts);

        if (roundingOffset != Vector2.zero)
        {
            for (int i = 0; i < vertCount; ++i)
            {
                int tempVertsIndex = i & 3;
                m_TempVerts[tempVertsIndex] = verts[i];
                m_TempVerts[tempVertsIndex].position *= unitsPerPixel;
                m_TempVerts[tempVertsIndex].position.x += roundingOffset.x;
                m_TempVerts[tempVertsIndex].position.y += roundingOffset.y;
                if (tempVertsIndex == 3)
                    toFill.AddUIVertexQuad(m_TempVerts);
            }
        }
        else
        {
            for (int i = 0; i < vertCount; ++i)
            {
                int tempVertsIndex = i & 3;
                m_TempVerts[tempVertsIndex] = verts[i];
                m_TempVerts[tempVertsIndex].position *= unitsPerPixel;
                if (tempVertsIndex == 3)
                    toFill.AddUIVertexQuad(m_TempVerts);
            }
        }

        //计算标签 计算偏移值后 再计算标签的值
        List<UIVertex> vertsTemp = new List<UIVertex>();
        for (int i = 0; i < vertCount; i++)
        {
            UIVertex tempVer = new UIVertex();
            toFill.PopulateUIVertex(ref tempVer, i);
            vertsTemp.Add(tempVer);
        }
        CalcQuadTag(vertsTemp);

        m_DisableFontTextureRebuiltCallback = false;

        // 分组
        if (listSprite.Count>0&&m_spriteGraphicArray!=null) {
            m_textGraphicToIndexDict.Clear();
            var dict = m_textGraphicToIndexDict;
            for (var i=0;i< m_spriteGraphicArray.Length;i++)
            {
                var textSprite = m_spriteGraphicArray[i];
                if (!dict.ContainsKey(textSprite))
                {
                    dict.Add(textSprite, new List<int>(){i});
                }
                else
                {
                    dict[textSprite].Add(i);
                }
            }
            // 按组绘制图片
            foreach (var item in dict)
            {
                var spriteGraphic = item.Key;
                var inlineSprite = listSprite[item.Value[0]];
                var sprite = m_nameToSpriteDict[inlineSprite.name];
                spriteGraphic.SetMainTexture(sprite.texture);
                var list = new List<InlineSpriteInfor>();
                foreach (var index in item.Value)
                {
                    list.Add(listSprite[index]);
                }
                DrawSprite(spriteGraphic, list);
            }
            // 处理空图的emoji
            callOnDrawSprite();
        }
        
        

        #region 处理超链接的包围盒
        // 处理超链接包围框
        UIVertex vert = new UIVertex();
        foreach (var hrefInfo in m_HrefInfos)
        {
            hrefInfo.boxes.Clear();
            if (hrefInfo.startIndex >= toFill.currentVertCount)
            {
                continue;
            }

            // 将超链接里面的文本顶点索引坐标加入到包围框
            toFill.PopulateUIVertex(ref vert, hrefInfo.startIndex);
            var pos = vert.position;
            var bounds = new Bounds(pos, Vector3.zero);
            for (int i = hrefInfo.startIndex, m = hrefInfo.endIndex; i < m; i++)
            {
                if (i >= toFill.currentVertCount)
                {
                    break;
                }

                toFill.PopulateUIVertex(ref vert, i);
                pos = vert.position;
                if (pos.x < bounds.min.x) // 换行重新添加包围框
                {
                    hrefInfo.boxes.Add(new Rect(bounds.min, bounds.size));
                    bounds = new Bounds(pos, Vector3.zero);
                }
                else
                {
                    bounds.Encapsulate(pos); // 扩展包围框
                }
            }
            hrefInfo.boxes.Add(new Rect(bounds.min, bounds.size));
        }
        #endregion

        #region 添加下划线
        TextGenerator _UnderlineText = new TextGenerator();
		//settings.color = Color.green;
	    Color underlineColor;
	    ColorUtility.TryParseHtmlString(UnderlineColor, out underlineColor);
        _UnderlineText.Populate("_", settings);
        IList<UIVertex> _TUT = _UnderlineText.verts;
        foreach (var item in m_HrefInfos)
        {
            for (int i = 0; i < item.boxes.Count; i++)
            {
                //计算下划线的位置
                Vector3[] _ulPos = new Vector3[4];
                float leftOffest = item.boxes[i].width / 10f;
                float rightOffest = item.boxes[i].width / 10f;
                _ulPos[0] = item.boxes[i].position + new Vector2(-leftOffest, fontSize * 0.1f);
                _ulPos[1] = _ulPos[0] + new Vector3(item.boxes[i].width + leftOffest + rightOffest, 0.0f);
                _ulPos[2] = item.boxes[i].position + new Vector2(item.boxes[i].width + rightOffest, 0.0f);
                _ulPos[3] = item.boxes[i].position + new Vector2(-leftOffest, 0.0f);
                //绘制下划线
                for (int j = 0; j < 4; j++)
                {
                    m_TempVerts[j] = _TUT[j];
                    m_TempVerts[j].color = underlineColor;
                    m_TempVerts[j].position = _ulPos[j];
                    if (j == 3)
                        toFill.AddUIVertexQuad(m_TempVerts);
                }

            }
        }

        #endregion
    }

    /// <summary>
    /// 解析quad标签  主要清除quad乱码 获取表情的位置
    /// </summary>
    /// <param name="verts"></param>
    void CalcQuadTag(IList<UIVertex> verts)
    {
        m_AnimSpriteInfo = new Dictionary<int, InlineSpriteInfor[]>();

        //通过标签信息来设置需要绘制的图片的信息
        listSprite = new List<InlineSpriteInfor>();
        for (int i = 0; i < listTagInfor.Count; i++)
        {
            ////UGUIText不支持<quad/>标签，表现为乱码，我这里将他的uv全设置为0,清除乱码
            //for (int m = listTagInfor[i].index * 4; m < listTagInfor[i].index * 4 + 18*4; m++)
            //{
            //    UIVertex tempVertex = verts[m];
            //    tempVertex.uv0 = Vector2.zero;
            //    verts[m] = tempVertex;
            //}
            //获取表情的第一个位置,则计算他的位置为quad占位的第四个点   顶点绘制顺序:       
            //                                                                              0    1
            //                                                                              3    2
            var leftBottomIndex = ((listTagInfor[i].index + 1) * 4) - 1;
            //超出可视范围的不会绘制，即leftBottomIndex >= verts.Count。
            //所以这里不需要处理也不应该处理。若处理，则数组越界。
            if (leftBottomIndex >= verts.Count)
            {
                break;
            }
            InlineSpriteInfor tempSprite = new InlineSpriteInfor();
            tempSprite.name = listTagInfor[i].name;
            tempSprite.isEmptySprite = listTagInfor[i].isEmptySprite;
            tempSprite.textpos = verts[leftBottomIndex].position;

            //设置图片的位置
            tempSprite.vertices = new Vector3[4];
            tempSprite.vertices[0] = new Vector3(0, 0, 0) + tempSprite.textpos;
            tempSprite.vertices[1] = new Vector3(listTagInfor[i].size.x, listTagInfor[i].size.y, 0) + tempSprite.textpos;
            tempSprite.vertices[2] = new Vector3(listTagInfor[i].size.x, 0, 0) + tempSprite.textpos;
            tempSprite.vertices[3] = new Vector3(0, listTagInfor[i].size.y, 0) + tempSprite.textpos;

            //给图片添加包装盒
            if (listTagInfor[i].isButton == true)
            {
                var bounds = new Bounds(tempSprite.vertices[0], Vector3.zero);
                bounds.Encapsulate(tempSprite.vertices[1]);
                bounds.Encapsulate(tempSprite.vertices[2]);
                bounds.Encapsulate(tempSprite.vertices[3]);

                listTagInfor[i].boxe = new Rect(bounds.min, bounds.size);
            }

            if (m_spriteGraphic == null || m_spriteGraphic.spriteAsset == null)
            {
                listSprite.Add(tempSprite);
                continue;
            }
            //计算其uv
            //Rect spriteRect = m_spriteAsset.listSpriteInfor[0].rect;
            Sprite sprite;
            m_nameToSpriteDict.TryGetValue(listTagInfor[i].name, out sprite);
            Rect spriteRect = sprite.textureRect;
            Texture texSource = sprite.texture;
            tempSprite.texture = texSource;

            // 改为全图啊 @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
            //Texture texCopy = new Texture2D
            //texSource.GetRawTextureData()
            Vector2 texSize = new Vector2(texSource.width, texSource.height);

            tempSprite.uv = new Vector2[4];
            tempSprite.uv[0] = new Vector2(spriteRect.x / texSize.x, spriteRect.y / texSize.y);
            tempSprite.uv[1] = new Vector2((spriteRect.x + spriteRect.width) / texSize.x, (spriteRect.y + spriteRect.height) / texSize.y);
            tempSprite.uv[2] = new Vector2((spriteRect.x + spriteRect.width) / texSize.x, spriteRect.y / texSize.y);
            tempSprite.uv[3] = new Vector2(spriteRect.x / texSize.x, (spriteRect.y + spriteRect.height) / texSize.y);

            //声明三角顶点所需要的数组
            tempSprite.triangles = new int[6];
            listSprite.Add(tempSprite);


            if (m_AnimSpriteTag[i] != null && m_AnimSpriteTag[i].Length>1)
            {
                SpriteTagInfor[] tempTagInfor = m_AnimSpriteTag[i];
                InlineSpriteInfor[] tempSpriteInfor = new InlineSpriteInfor[tempTagInfor.Length];
                for (int j = 0; j < tempTagInfor.Length; j++)
                {
                    tempSpriteInfor[j] = new InlineSpriteInfor();

                    tempSpriteInfor[j].name = tempTagInfor[j].name;
                    tempSpriteInfor[j].textpos = verts[((tempTagInfor[j].index + 1) * 4) - 1].position;
                    //设置图片的位置
                    tempSpriteInfor[j].vertices = new Vector3[4];
                    tempSpriteInfor[j].vertices[0] = new Vector3(0, 0, 0) + tempSpriteInfor[j].textpos;
                    tempSpriteInfor[j].vertices[1] = new Vector3(tempTagInfor[j].size.x, tempTagInfor[j].size.y, 0) + tempSpriteInfor[j].textpos;
                    tempSpriteInfor[j].vertices[2] = new Vector3(tempTagInfor[j].size.x, 0, 0) + tempSpriteInfor[j].textpos;
                    tempSpriteInfor[j].vertices[3] = new Vector3(0, tempTagInfor[j].size.y, 0) + tempSpriteInfor[j].textpos;

                    //计算其uv
                    m_nameToSpriteDict.TryGetValue(tempTagInfor[j].name, out sprite);
                    Rect newSpriteRect = sprite.textureRect;
                    Vector2 newTexSize = new Vector2(sprite.texture.width, sprite.texture.height);

                    tempSpriteInfor[j].uv = new Vector2[4];
                    tempSpriteInfor[j].uv[0] = new Vector2(newSpriteRect.x / newTexSize.x, newSpriteRect.y / newTexSize.y);
                    tempSpriteInfor[j].uv[1] = new Vector2((newSpriteRect.x + newSpriteRect.width) / newTexSize.x, (newSpriteRect.y + newSpriteRect.height) / newTexSize.y);
                    tempSpriteInfor[j].uv[2] = new Vector2((newSpriteRect.x + newSpriteRect.width) / newTexSize.x, newSpriteRect.y / newTexSize.y);
                    tempSpriteInfor[j].uv[3] = new Vector2(newSpriteRect.x / newTexSize.x, (newSpriteRect.y + newSpriteRect.height) / newTexSize.y);

                    //声明三角顶点所需要的数组
                    tempSpriteInfor[j].triangles = new int[6];
                }
                m_AnimSpriteInfo.Add(i, tempSpriteInfor);
            }
        }
    }

	private List<Vector3> drawSpriteVertices;
	private List<Vector2> drawSpriteUv;
	private List<int> drawSpriteTriangles;
	private Vector3[] _C_ZeroVertices = new Vector3[] {
		new Vector3 (-1, -1, 0),
		new Vector3 (-1, -1, 0),
		new Vector3 (-1, -1, 0),
		new Vector3 (-1, -1, 0)
	};
	private Vector2[] _C_ZeroUVs = new Vector2[] {
		new Vector2 (0, 1),
		new Vector2 (1, 1),
		new Vector2 (1, 0),
		new Vector2 (0, 0)
	};
	private int[] _C_ZeroTriangles = new int[]{ 0, 1, 2, 2, 3, 0 };
	/// <summary>
	/// 绘制图片
	/// </summary>
	void DrawSprite(UIGraphicTextSprites spriteGraphic, List<InlineSpriteInfor> listInlineSprite)
	{
        var spriteCanvasRenderer = spriteGraphic.GetComponentInChildren<CanvasRenderer>();

		//偶发在显示隐藏时，会有报空，有时机问题
		if (spriteCanvasRenderer == null || spriteGraphic == null) {
			return;
		}

		if (m_spriteMesh == null)
		{
			m_spriteMesh = new Mesh();
		}

		if (drawSpriteVertices == null) drawSpriteVertices = new List<Vector3>();
		else drawSpriteVertices.Clear();

		if (drawSpriteUv == null) drawSpriteUv = new List<Vector2>();
		else drawSpriteUv.Clear();

		if (drawSpriteTriangles == null) drawSpriteTriangles = new List<int>();
		else drawSpriteTriangles.Clear();

        for (int i = 0; i < listInlineSprite.Count; i++)
        {
            var inlineSprite = listInlineSprite[i];
            if (inlineSprite.isEmptySprite) 
            {
                continue;
            }
            for (int j = 0; j < inlineSprite.vertices.Length; j++)
            {
                drawSpriteVertices.Add(inlineSprite.vertices[j]);
            }
            for (int j = 0; j < inlineSprite.uv.Length; j++)
            {
                drawSpriteUv.Add(inlineSprite.uv[j]);
            }
            for (int j = 0; j < inlineSprite.triangles.Length; j++)
            {
                drawSpriteTriangles.Add(inlineSprite.triangles[j]);
            }
        }
        //计算顶点绘制顺序
        for (int i = 0; i < drawSpriteTriangles.Count; i++)
        {
            if (i % 6 == 0)
            {
                int num = i / 6;
                drawSpriteTriangles[i] = 0 + 4 * num;
                drawSpriteTriangles[i + 1] = 1 + 4 * num;
                drawSpriteTriangles[i + 2] = 2 + 4 * num;

                drawSpriteTriangles[i + 3] = 1 + 4 * num;
                drawSpriteTriangles[i + 4] = 0 + 4 * num;
                drawSpriteTriangles[i + 5] = 3 + 4 * num;
            }
        }

		if (m_spriteMesh.vertices.Length > drawSpriteVertices.Count && m_spriteMesh.vertices.Length == _C_ZeroVertices.Length && drawSpriteVertices.Count == 0) {
			//缓存的mesh顶点数组大于目标值，缩减到0，表示没有图片需要，用常量归0，暂定4顶点
//			Debug.LogWarning (m_spriteMesh.vertices.Length + "_%%%%%%%%%%__" + drawSpriteVertices.Count);
			m_spriteMesh.vertices = _C_ZeroVertices;
			m_spriteMesh.uv = _C_ZeroUVs;
			m_spriteMesh.triangles = _C_ZeroTriangles;
		} 
		else if(m_spriteMesh.vertices.Length > drawSpriteVertices.Count && m_spriteMesh.vertices.Length != _C_ZeroVertices.Length){
			//缓存的mesh顶点过多无法缩减到小，只能new一个新缓存
//			Debug.LogWarning (m_spriteMesh.vertices.Length + "_^^^^^^^^^^^^^^^^^^^^__" + drawSpriteVertices.Count);
			m_spriteMesh = new Mesh();
			m_spriteMesh.vertices = drawSpriteVertices.ToArray ();
			m_spriteMesh.uv = drawSpriteUv.ToArray ();
			m_spriteMesh.triangles = drawSpriteTriangles.ToArray ();
		}
		else {
			//普通情况直接赋予
//			Debug.LogWarning (m_spriteMesh.vertices.Length + "___" + drawSpriteVertices.Count);
			m_spriteMesh.vertices = drawSpriteVertices.ToArray ();
			m_spriteMesh.uv = drawSpriteUv.ToArray ();
			m_spriteMesh.triangles = drawSpriteTriangles.ToArray ();
		}

        if (m_spriteMesh == null)
            return;

        spriteCanvasRenderer.SetMesh(m_spriteMesh);
        spriteGraphic.UpdateMaterial();
    }

	private List<Vector2> emptySizes;
	private List<Vector2> emptyPoses;
	void callOnDrawSprite()
    {
		if (emptySizes == null) emptySizes = new List<Vector2>();
		else emptySizes.Clear();

		if (emptyPoses == null) emptyPoses = new List<Vector2>();
		else emptyPoses.Clear();

        for (int i = 0; i < listTagInfor.Count; i++)
        {
            if (!listTagInfor[i].isEmptySprite)
            {
                continue;
            }
            var vertices = listSprite[i].vertices;
            var sizeVec3 = vertices[1] - vertices[0];
            var posVec3 = vertices[0] + sizeVec3 / 2;
            emptySizes.Add(sizeVec3);
            emptyPoses.Add(posVec3);
        }
        if (_luaHandlerOnDrawSprite != null)
        {
            if (_luaHandlerTargetOnDrawSprite != null)
            {
                _luaHandlerOnDrawSprite.Call(_luaHandlerTargetOnDrawSprite, emptyPoses.ToArray(), emptySizes.ToArray());
            }
            else
            {
                _luaHandlerOnDrawSprite.Call(emptyPoses.ToArray(), emptySizes.ToArray());
            }
        }
    }


    float fTime = 0.0f;
    //int iIndex = 0;
    void Update()
    {
		if(listSprite == null || listSprite.Count==0 || m_hasAnimTag==false) return;

		fTime += Time.deltaTime;
		if (fTime >= DynamicTagSwitchInterval)
		{
            UpdateAnimSprite();
			fTime = 0.0f;
		}

	}

    void UpdateAnimSprite() {
        for (int i = 0; i < m_AnimIndex.Count; i++)
        {
            var animIndex = m_AnimIndex[i];
            if (!m_AnimSpriteInfo.ContainsKey(animIndex)) continue;

            m_AnimSpriteStep[animIndex]++;
            if (m_AnimSpriteStep[animIndex] >= m_AnimSpriteInfo[animIndex].Length)
            {
                m_AnimSpriteStep[animIndex] = 0;
            }
            var step = m_AnimSpriteStep[animIndex];
            var inlineSprite = m_AnimSpriteInfo[animIndex][step];
            Sprite sprite;
            m_nameToSpriteDict.TryGetValue(inlineSprite.name, out sprite);
            var spriteGraphic = m_spriteGraphicArray[animIndex];
            spriteGraphic.SetMainTexture(sprite.texture);
            DrawSprite(spriteGraphic, new List<InlineSpriteInfor>(){inlineSprite});
        }

    }

    #region 超链接
    /// <summary>
    /// 超链接信息列表
    /// </summary>
    private readonly List<HrefInfo> m_HrefInfos = new List<HrefInfo>();

    /// <summary>
    /// 文本构造器
    /// </summary>
    private static readonly StringBuilder s_TextBuilder = new StringBuilder();

    /// <summary>
    /// 超链接正则
    /// </summary>
    private static readonly Regex s_HrefRegex =
        new Regex(@"<a href=([^>\n\s]+)>(.*?)(</a>)", RegexOptions.Singleline);

    private LuaFunction _luaHandler = null;
    private LuaTable _luaHandlerTarget = null;

    private LuaFunction _luaHandlerOnDrawSprite = null;
    private LuaTable _luaHandlerTargetOnDrawSprite = null;
    public void AddClickListener(LuaFunction luaHandler, LuaTable luaTarget = null)
    {
        _luaHandler = luaHandler;
        _luaHandlerTarget = luaTarget;
    }

    public void RemoveClickListener()
    {
        _luaHandler = null;
        _luaHandlerTarget = null;
    }

    public void AddDrawSpriteListener(LuaFunction luaHandler, LuaTable luaTarget = null)
    {
        _luaHandlerOnDrawSprite = luaHandler;
        _luaHandlerTargetOnDrawSprite = luaTarget;
    }

    public void RemoveDrawSpriteListener()
    {
        _luaHandlerOnDrawSprite = null;
        _luaHandlerTargetOnDrawSprite = null;
    }

    /// <summary>
    /// 获取超链接解析后的最后输出文本
    /// </summary>
    /// <returns></returns>
    protected string GetOutputText()
    {
        s_TextBuilder.Length = 0;
        m_HrefInfos.Clear();
        var indexText = 0;
        foreach (Match match in s_HrefRegex.Matches(text))
        {
            s_TextBuilder.Append(text.Substring(indexText, match.Index - indexText));
            s_TextBuilder.Append(string.Format("<color={0}>" , HyperLinkTextColor));  // 超链接颜色

            var group = match.Groups[1];
            var hrefInfo = new HrefInfo
            {
                startIndex = s_TextBuilder.Length * 4, // 超链接里的文本起始顶点索引
                endIndex = (s_TextBuilder.Length + match.Groups[2].Length - 1) * 4 + 3,
                name = group.Value,
            };
            m_HrefInfos.Add(hrefInfo);

            s_TextBuilder.Append(match.Groups[2].Value);
            s_TextBuilder.Append("</color>");
            indexText = match.Index + match.Length;
        }
        s_TextBuilder.Append(text.Substring(indexText, text.Length - indexText));
        return s_TextBuilder.ToString();
    }

    /// <summary>
    /// 点击事件检测是否点击到超链接文本
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerClick(PointerEventData eventData)
    {
        Vector2 lp;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out lp);

        //判断是否点击到超链接
        foreach (var hrefInfo in m_HrefInfos)
        {
            var boxes = hrefInfo.boxes;
            for (var i = 0; i < boxes.Count; ++i)
            {
                if (boxes[i].Contains(lp))
                {
                    _luaHandler.Call(_luaHandlerTarget,hrefInfo.name);
                    return;
                }
            }
        }

        //判断是否可点击 的图片
        foreach(var tagInfo in listTagInfor)
        {
            if (tagInfo.isButton == false)
                continue;
            var boxe = tagInfo.boxe;
            if(boxe.Contains(lp))
            {
                _luaHandler.Call(_luaHandlerTarget, tagInfo.des);
                return;
            }
        }
    }

    /// <summary>
    /// 超链接信息类
    /// </summary>
    private class HrefInfo
    {
        public int startIndex;

        public int endIndex;

        public string name;

        public readonly List<Rect> boxes = new List<Rect>();
    }
	#endregion

	protected override void OnDestroy(){
		if (_luaHandler != null)
		{
			UIEventUtil.LogErrorGOPath("UIGraphicText.click", gameObject);
		}
		if (_luaHandlerOnDrawSprite != null)
		{
			UIEventUtil.LogErrorGOPath("UIGraphicText.onDrawSprite", gameObject);
		}
		RemoveClickListener ();
		RemoveDrawSpriteListener ();
		m_spriteMesh = null;
	}

	#region 复写Text的属性适应多语言的剥离Font与语言切换
	bool _hasInit = false;
	private void TryInitFont()
	{
		if (!_hasInit)
		{
			_hasInit = true;
			if (null == this.font)
			{
				var tl = this.GetComponent<TextLocalization>();
				if (null != tl)
				{
					tl.CheckFont();
				}
			}
		}
	}
	public override float preferredWidth
	{
		get
		{
			TryInitFont();
			return this.cachedTextGeneratorForLayout.GetPreferredWidth(this.m_OutputText, this.GetGenerationSettings(Vector2.zero)) / this.pixelsPerUnit;
		}
	}

	public override float preferredHeight
	{
		get
		{
			TryInitFont();
			return this.cachedTextGeneratorForLayout.GetPreferredHeight(this.m_OutputText, this.GetGenerationSettings(new Vector2(this.GetPixelAdjustedRect().size.x, 0.0f))) / this.pixelsPerUnit;
		}
	}
	public override float flexibleWidth
	{
		get
		{
			TryInitFont();
			return base.flexibleWidth;
		}
	}
	public override float flexibleHeight
	{
		get
		{
			TryInitFont();
			return base.flexibleHeight;
		}
	}

	public override float minWidth
	{
		get
		{
			TryInitFont();
			return base.minWidth;
		}
	}
	public override float minHeight
	{
		get
		{
			TryInitFont();
			return base.minHeight;
		}
	}
	#endregion
}


[System.Serializable]
public class SpriteTagInfor
{
    /// <summary>
    /// sprite名称
    /// </summary>
    public string name;
    /// <summary>
    /// 对应的字符索引
    /// </summary>
    public int index;
    /// <summary>
    /// 大小
    /// </summary>
    public Vector2 size;

    /// <summary>
    /// 是否可以点击
    /// </summary>
    public bool isButton;

    /// <summary>
    /// 附带按钮描述，用于按钮点击
    /// </summary>
    public string des;

    /// <summary>
    /// 是否为空节点（空节点则不设置图片，只返回position和size,由外部设置图（如big bg下的图片或其它预制等））
    /// </summary>
    public bool isEmptySprite;

    /// <summary>
    /// 点击区域
    /// </summary>
    public Rect boxe;

    public int Length;
}


// [System.Serializable]
public struct InlineSpriteInfor
{
    // 文字的最后的位置
    public Vector3 textpos;
    // 4 顶点 
    public Vector3[] vertices;
    //4 uv
    public Vector2[] uv;
    //6 三角顶点顺序
    public int[] triangles;
    public bool isEmptySprite;
    public Texture texture;
    public string name;
}