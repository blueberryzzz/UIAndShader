using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;
using System.Collections;

public class ChunkDisappearImageUI : Image
{       
    public float Speed = 50;    
    public float TargetX = 0f;
    public float TargetY = 0f;

    public float SubRectX = 5;
    public float SubRectY = 5;

    public float Interval = 0.5f;
    [Range(0,1)]
    public float Acceleration = 0;

    public float TimeDelta = 0;
    private float mDelta;

    private bool isRunning = false;
    
    public void StartDisappear()
    {
        color = new Color(color.r, color.g, color.b,1);
        isRunning = true;
        TimeDelta = 0;
    }

    public void ResetImage(float alpha = 1)
    {
        color = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        isRunning = false;
        UpdateGeometry();
    }
    public IEnumerator DestroyAfterDisappear()
    {
        StartDisappear();
        while (isRunning)
        {
            yield return null;
        }
        Destroy(gameObject);
    }
    
    void LateUpdate () {
        if (!isRunning) return;
        TimeDelta += Time.unscaledDeltaTime;
        mDelta = TimeDelta * Speed;
        UpdateGeometry();
    }
    
    void SetNormalMesh(VertexHelper vh,Vector4 v,Vector4 uv,Color color32)
    {
        vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(uv.x, uv.y));
        vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(uv.x, uv.w));
        vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(uv.z, uv.w));
        vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(uv.z, uv.y));

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        Vector4 v = GetDrawingDimensions(false);
        Vector4 uv = overrideSprite != null ? DataUtility.GetOuterUV(overrideSprite) : Vector4.zero;

        var color32 = color;
        vh.Clear();
        if (!isRunning)
        {
            SetNormalMesh(vh, v, uv, color32);            
        }
        else
        {
            Vector2 subRect = new Vector2(SubRectX, SubRectY);          

            int ColNum = Mathf.CeilToInt((v.z - v.x) / subRect.x);
            int LineNum = Mathf.CeilToInt((v.w - v.y) / subRect.y);

            subRect = new Vector2((v.z - v.x) / ColNum, (v.w - v.y) / LineNum);
            Vector2 uvDelta = new Vector2(subRect.x / (v.z - v.x), subRect.y / (v.w - v.y));

            int startNum = 0;
            Vector2 target = new Vector2(TargetX, TargetY);
            for (int i = 0; i < LineNum; i++)
            {
                for (int j = 0; j < ColNum; j++)
                {
                    color32 = color;
                    //计算对应的v块
                    Vector2 leftBottom = new Vector2(v.x + j * subRect.x, v.y + i * subRect.y);
                    Vector2 leftTop = new Vector2(leftBottom.x, leftBottom.y + subRect.y);
                    Vector2 rightBottom = new Vector2(leftBottom.x + subRect.x, leftBottom.y);
                    Vector2 rightTop = new Vector2(leftBottom.x + subRect.x, leftBottom.y + subRect.y);

                    //计算对应的uv块
                    Vector2 leftBottomUV = new Vector2(uv.x + j * uvDelta.x, uv.y + i * uvDelta.y);
                    Vector2 leftTopUV = new Vector2(leftBottomUV.x, leftBottomUV.y + uvDelta.y);
                    Vector2 rightBottomUV = new Vector2(leftBottomUV.x + uvDelta.x, leftBottomUV.y);
                    Vector2 rightTopUV = new Vector2(leftBottomUV.x + uvDelta.x, leftBottomUV.y + uvDelta.y);

                    Vector2 position = new Vector2();
                    float distance = (target - leftBottom).magnitude;
                    float percent = (mDelta - distance * Interval) / Mathf.Pow(distance,Acceleration);
                    position.x = Mathf.Lerp(leftBottom.x, target.x, percent);
                    position.y = Mathf.Lerp(leftBottom.y, target.y, percent);
                    color32.a = Mathf.Clamp01(Mathf.Lerp(10, 0, percent));
                    if (color32.a < 0.01f) continue;

                    Vector2 delta = position - leftBottom;
                    vh.AddVert(new Vector3(leftBottom.x + delta.x, leftBottom.y + delta.y), color32, new Vector2(leftBottomUV.x, leftBottomUV.y));
                    vh.AddVert(new Vector3(leftTop.x + delta.x, leftTop.y + delta.y), color32, new Vector2(leftTopUV.x, leftTopUV.y));
                    vh.AddVert(new Vector3(rightBottom.x + delta.x, rightBottom.y + delta.y), color32, new Vector2(rightBottomUV.x, rightBottomUV.y));
                    vh.AddVert(new Vector3(rightTop.x + delta.x, rightTop.y + delta.y), color32, new Vector2(rightTopUV.x, rightTopUV.y));
                    vh.AddTriangle(startNum + 0, startNum + 1, startNum + 3);
                    vh.AddTriangle(startNum + 0, startNum + 3, startNum + 2);
                    startNum += 4;
                }
            }
            if (startNum == 0)
            {
                ResetImage(0);
            }
        }
    }
    
    public Vector2 FixVert(Vector4 v,float x,float y)
    {
        if (x < v.x) x = v.x;
        if (x > v.z) x = v.z;

        if (y < v.y) y = v.y;
        if (y > v.w) y = v.w;

        return new Vector2(x, y);
    }

    private Vector4 GetDrawingDimensions(bool shouldPreserveAspect)
    {
        var padding = overrideSprite == null ? Vector4.zero : DataUtility.GetPadding(overrideSprite);
        Rect r = GetPixelAdjustedRect();
        var size = overrideSprite == null ? new Vector2(r.width, r.height) : new Vector2(overrideSprite.rect.width, overrideSprite.rect.height);

        int spriteW = Mathf.RoundToInt(size.x);
        int spriteH = Mathf.RoundToInt(size.y);

        if (shouldPreserveAspect && size.sqrMagnitude > 0.0f)
        {
            var spriteRatio = size.x / size.y;
            var rectRatio = r.width / r.height;

            if (spriteRatio > rectRatio)
            {
                var oldHeight = r.height;
                r.height = r.width * (1.0f / spriteRatio);
                r.y += (oldHeight - r.height) * rectTransform.pivot.y;
            }
            else
            {
                var oldWidth = r.width;
                r.width = r.height * spriteRatio;
                r.x += (oldWidth - r.width) * rectTransform.pivot.x;
            }
        }

        var v = new Vector4(
                padding.x / spriteW,
                padding.y / spriteH,
                (spriteW - padding.z) / spriteW,
                (spriteH - padding.w) / spriteH);

        v = new Vector4(
                r.x + r.width * v.x,
                r.y + r.height * v.y,
                r.x + r.width * v.z,
                r.y + r.height * v.w
                );

        return v;
    }
}
