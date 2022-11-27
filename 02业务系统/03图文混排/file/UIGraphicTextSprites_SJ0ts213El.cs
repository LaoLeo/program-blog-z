using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Framework;
using System;

public class UIGraphicTextSprites : MaskableGraphic
{
    Resource _res;
    private EmojiSpriteAsset m_spriteAsset;
    public string EmojiSpriteAssetPath;
    public Action onLoadCallback;


    public EmojiSpriteAsset spriteAsset
    {
        get { return m_spriteAsset; }
    }

    private Texture m_textSource; // 由外部传入改写mainTexture
    public override Texture mainTexture
    {
        get
        {
            if (m_spriteAsset == null)
                return s_WhiteTexture;

            if (m_spriteAsset.listSpriteInfor == null || m_spriteAsset.listSpriteInfor.Count == 0)
            {
                return s_WhiteTexture;
            }

            if (m_textSource != null)
            {
                return m_textSource;
            }

            var texSource = m_spriteAsset.listSpriteInfor[0].sprite.texture;
            if (texSource == null)
                return s_WhiteTexture;
            else
                return texSource;
        }
    }
    public void SetMainTexture(Texture t)
    {
        m_textSource = t;
    }

    protected override void OnEnable()
    {
        //不调用父类的OnEnable 他默认会渲染整张图片
        //base.OnEnable();
        Load();
    }

    protected override void OnDisable()
    {
        DoClear();
        base.OnDisable();
    }

    public override void SetAllDirty()
    {
        base.SetLayoutDirty();
        base.SetMaterialDirty();
    }

#if UNITY_EDITOR
    //在编辑器下
    protected override void OnValidate()
    {
        base.OnValidate();
        //Debug.Log("Texture ID is " + this.texture.GetInstanceID());
    }
#endif

    protected override void OnRectTransformDimensionsChange()
    {
        // base.OnRectTransformDimensionsChange();
    }

    /// <summary>
    /// 绘制后 需要更新材质
    /// </summary>
    public new void UpdateMaterial()
    {
        base.UpdateMaterial();
    }


    protected override void OnDestroy()
    {
        DoClear();
        base.OnDestroy();
    }

    void DoClear()
    {
        if (null != _res)
        {
            _res.Release();
            _res = null;
        }
        this.m_spriteAsset = null;
        //路径存在才移除资源监听
        if (!string.IsNullOrEmpty(EmojiSpriteAssetPath))
        {
            ResourceCache.Instance.RemoveListener(EmojiSpriteAssetPath, OnResLoaded);
        }
    }

    void Load()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return;
#endif

        if (!ResourceCache.Exists || string.IsNullOrEmpty(EmojiSpriteAssetPath))
        {
            if (null != _res)
            {
                _res.Release();
                _res = null;
            }
            this.m_spriteAsset = null;
            return;
        }

        ResourceCache.Instance.RemoveListener(EmojiSpriteAssetPath, OnResLoaded);
        ResourceCache.Instance.GetResource(EmojiSpriteAssetPath, OnResLoaded);
    }

    void OnResLoaded(Resource res)
    {
        if (null != _res)
        {
            _res.Release();
            _res = null;
        }
        if (!res.IsSuccess)
        {
            CLogger.LogError(string.Format("!res.IsSuccess, res.ResPath:{0}", res.ResPath));
            return;
        }
        _res = res;
        res.Retain();
        var asset = res.GetMainAsset() as EmojiSpriteAsset;
        if (null == asset)
        {
            CLogger.LogError(string.Format("null == asset, res.ResPath:{0}", res.ResPath));
            return;
        }
        m_spriteAsset = asset;
        if (onLoadCallback!=null)
        {
            onLoadCallback();
        }
    }

}
