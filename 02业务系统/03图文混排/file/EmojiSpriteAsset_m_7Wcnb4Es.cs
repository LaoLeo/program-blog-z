﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EmojiSpriteAsset : ScriptableObject
{
    /// <summary>
    /// 所有sprite信息 SpriteAssetInfor类为具体的信息类
    /// </summary>
    public List<EmojiSpriteInfo> listSpriteInfor;
}

[System.Serializable]
public class EmojiSpriteInfo
{
    /// <summary>
    /// ID
    /// </summary>
    public int ID;
    /// <summary>
    /// 名称
    /// </summary>
    public string name;
    /// <summary>
    /// 中心点
    /// </summary>
    public Vector2 pivot;
    /// <summary>
    ///坐标&宽高
    /// </summary>
    public Rect rect;
    /// <summary>
    /// 精灵
    /// </summary>
    public Sprite sprite;
}