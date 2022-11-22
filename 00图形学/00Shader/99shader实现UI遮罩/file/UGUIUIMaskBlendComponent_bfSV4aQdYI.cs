using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;

[AddComponentMenu("UI/UGUI UIMask Blend Component")]
//[RequireComponent(typeof(Image))]
[ExecuteInEditMode]
// 第一版本
/// <summary>
/// 修改Mesh，通过UV2的设置，让材质的mask贴图可以获得正确的uv数据。
/// 支持共用材质，从而支持合批。
/// 此脚本专门服务pjg/ui/ui_mask 这个shader
/// </summary>
public class UGUIUIMaskBlendComponent : BaseMeshEffect {
	Image image;
	//目前为了兼容TiledImage，RawImage这两种ui组件
	RawImage rawimage;

	protected override void Awake ()
	{
		//一个物体只可能有一种组件
		image = this.GetComponent<Image> ();
		rawimage = this.GetComponent<RawImage> ();
	}

	public override void ModifyMesh(VertexHelper vh)
	{
		if (!IsActive ())
			return;

		Vector4 uv = image && image.overrideSprite != null ? DataUtility.GetOuterUV (image.overrideSprite) : Vector4.zero;
		float uvWidth = uv.z - uv.x;
		float uvHeight = uv.w - uv.y;
		if (rawimage) {
			//RawImage的uv变换是靠uvRect属性控制，ugui没有自动转换到图集，都是走单图形式
			uv.x = rawimage.uvRect.x;
			uv.y = rawimage.uvRect.y;
			uv.z = rawimage.uvRect.width;
			uv.w = rawimage.uvRect.height;
			uvWidth = rawimage.uvRect.width;
			uvHeight = rawimage.uvRect.height;
		}
		if (uvWidth == 0f || uvHeight == 0f) {
			return;
		}

		//换算回，ugui自动转换前的uv值
		//转换公式：图集中uvx = 源uvx * uvWidth图片图集中宽度比例 + uv.x图集中图片的x坐标
		//目的另mask遮罩图片使用转换前的uv值来采样像素
		int vertCount = vh.currentVertCount;
		var vert = new UIVertex ();
		for (int i = 0; i < vertCount; ++i) {
			vh.PopulateUIVertex (ref vert, i);

			//换回原来的后，故意加大1的uv值，用于另shader可以兼容过渡没有uv1的情况
			vert.uv1.x = (vert.uv0.x - uv.x) / uvWidth + 1f;
			vert.uv1.y = (vert.uv0.y - uv.y) / uvHeight + 1f;
			vh.SetUIVertex (vert, i);
		}
	}
}


/*
//第二版本
public class UGUIUIMaskBlendComponent : MonoBehaviour {
	Image image;
	//目前为了兼容TiledImage，RawImage这两种ui组件
	RawImage rawimage;

	void Awake ()
	{
		//一个物体只可能有一种组件
		image = this.GetComponent<Image> ();
		if (image != null) {
			image.RegisterDirtyMaterialCallback(OnDirtyMaterial);
		} else {
			rawimage = this.GetComponent<RawImage> ();
			rawimage.RegisterDirtyMaterialCallback(OnDirtyMaterial);
		}
	}

	private void Start()
	{
		OnDirtyMaterial();
	}

	void OnDirtyMaterial()
	{
		Material mat;
		if (image != null) {
			mat = image.material;
			if (mat.shader.name.Contains ("ui_mask")) {
				//对于图集中的精灵的 uv，是【0,1】之间的一个子区间
				//通过脚本传入 这个子区间的min和max，
				//shader的vert顶点着色，就又可以变换到了【0，1】区间
				Vector4 uvRect = UnityEngine.Sprites.DataUtility.GetOuterUV (image.overrideSprite);
				mat.SetVector ("_UVRange", uvRect);
			}
		} else if (rawimage != null) {
			mat = rawimage.material;
			if (mat.shader.name.Contains ("ui_mask")) {
				//RawImage的uv变换是靠uvRect属性控制，ugui没有自动转换到图集，都是走单图形式
				Vector4 uvRect = Vector4.zero;
				uvRect.x = rawimage.uvRect.x;
				uvRect.y = rawimage.uvRect.y;
				//这里是为了跟image的计算方式统一，所以主动加大x，y。另shader的公式一样成立
				uvRect.z = rawimage.uvRect.width + rawimage.uvRect.x;
				uvRect.w = rawimage.uvRect.height + rawimage.uvRect.y;
				mat.SetVector ("_UVRange", uvRect);
			}
		}
	}

	private void OnDestroy()
	{
		if (image != null) {
			image.UnregisterDirtyMaterialCallback (OnDirtyMaterial);
		} else if (rawimage != null) {
			rawimage.UnregisterDirtyMaterialCallback (OnDirtyMaterial);
		}
	}
}
*/