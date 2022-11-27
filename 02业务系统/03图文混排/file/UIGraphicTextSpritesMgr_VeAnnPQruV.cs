using Framework;
using UnityEngine;
using System.Collections.Generic;

namespace Pjg
{
    /// <summary>
    /// 针对多emoji显示的消息，负责分配显示组件
    /// </summary>
    public class UIGraphicTextSpritesMgr : SingletonSpawningMonoBehaviour<UIGraphicTextSpritesMgr>
    {
        public static int MAX_POOL_ENTRY = 10;
        GameObject m_goUIRoot;
        protected Queue<UIGraphicTextSprites> _GraphicSpriteQueue;

        protected override void Awake() {
            base.Awake();
            _GraphicSpriteQueue = new Queue<UIGraphicTextSprites>();
            m_goUIRoot = GameObject.Find("UIROOT");
            this.gameObject.transform.SetParent(m_goUIRoot.transform);
            this.gameObject.SetActive(false);
        }

        // 分发
        public UIGraphicTextSprites Distribute(GameObject goSource, Transform parent)
        {
            if (_GraphicSpriteQueue.Count > 0)
            {
                return _GraphicSpriteQueue.Dequeue();
            }
            else
            {
                var go = GameObject.Instantiate(goSource, parent, true);
                var comp = go.GetComponent<UIGraphicTextSprites>();
                return comp;
            }
        }

        // 回收
        public void Dispose(UIGraphicTextSprites graphicSprite)
        {
            graphicSprite.transform.SetParent(this.gameObject.transform);
            graphicSprite.SetMainTexture(null);
            graphicSprite.SetAllDirty();

            if (_GraphicSpriteQueue.Count<MAX_POOL_ENTRY)
            {
                _GraphicSpriteQueue.Enqueue(graphicSprite);
            }
            else
            {
                GameObject.Destroy(graphicSprite.gameObject);
            }
        }
    }
}