using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;
using System.Collections;
using System.Collections.Generic;

public class ChunkDisappearImage : Image
{     
    //小方块飞行的速度  
    public float Speed = 50;
    //目标点的本地坐标（在该图片上的坐标）
    public float TargetX = 0f;
    public float TargetY = 0f;
    //小方块的大小
    public float SubRectX = 5;
    public float SubRectY = 5;
    //间隔
    public float Interval = 2;
    //速度参数，为1时，所有方块运行速度相同，值越小，距离越远的方块运行的越快
    [Range(0,1)]
    public float SpeedArg = 1;
    //移动参数
    private float mDelta;
    //是否在运行
    private bool isRunning = false;
    //本地坐标到Canvas上坐标的矩阵
    private Matrix4x4 mLocalToCanvas;

    protected override void Awake()
    {
        ResetImage(color.a);
    }

    //开始消失
    public void StartDisappear(float alpha = 1)
    {
        color = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        isRunning = true;
        
        //计算起始时间，保证一开始就会有小方块在移动
        mDelta = CalStartTime();   
        //设置shader要用到的uniform值     
        material.SetFloat("_Delta", mDelta);
        material.SetVector("_Target", new Vector2(TargetX, TargetY));
        material.SetFloat("_Interval", Interval);
        material.SetFloat("_SpeedArg", SpeedArg);
        mLocalToCanvas = canvas.rootCanvas.transform.localToWorldMatrix.inverse * transform.localToWorldMatrix;
        material.SetMatrix("_LocalToCanvas", mLocalToCanvas);
        //更新mesh，拆分图片mesh
        UpdateGeometry();
    }

    //重置图片
    public void ResetImage(float alpha = 1)
    {
        isRunning = false;
        color = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha)); 
        //初始化移动参数
        mDelta = 0;
        material.SetFloat("_Delta", mDelta);
        //更新mesh，还原图片mesh
        UpdateGeometry();
    }

    //图片消散结束后，自动销毁
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
        material.SetVector("_Target", new Vector2(TargetX, TargetY));
        material.SetFloat("_Interval", Interval);
        material.SetFloat("_SpeedArg", SpeedArg);
        //更新转换矩阵
        mLocalToCanvas = canvas.rootCanvas.transform.localToWorldMatrix.inverse * transform.localToWorldMatrix;
        material.SetMatrix("_LocalToCanvas", mLocalToCanvas);
        //更新位移参数
        mDelta += Time.unscaledDeltaTime * Speed;
        material.SetFloat("_Delta", mDelta);
        //判断是否结束
        if (mDelta > CalEndTime())
        {
            isRunning = false;
            //重置图片
            ResetImage(0);
        }
    }
    
    //计算结束时的mDelta值
    float CalEndTime()
    {
        //最远的点最后消失，而最远的点一定是四个定点，所以只用四个定点的信息反推最大的mDelta即可
        mLocalToCanvas = canvas.rootCanvas.transform.localToWorldMatrix.inverse * transform.localToWorldMatrix;
        Vector4 v = GetDrawingDimensions(false);        
        List<Vector2> vertexList = new List<Vector2> {
            new Vector2(v.x, v.y),
            new Vector2(v.x, v.w),
            new Vector2(v.z, v.y),
            new Vector2(v.z, v.w)
        };
        Vector3 canvasTarget = mLocalToCanvas * (new Vector4(TargetX, TargetY, 0,1));
        float farthestDistance = 0;
        foreach(Vector2 vertex in vertexList)
        {
            Vector3 canvasPoint = mLocalToCanvas * (new Vector4(vertex.x, vertex.y, 0, 1));
            float distance = (canvasTarget - canvasPoint).magnitude;
            farthestDistance = farthestDistance > distance ? farthestDistance : distance;
        }
        //根据Shader中使用的公式反推
        return farthestDistance * Interval + (1 + farthestDistance * SpeedArg);
    }

    //计算起始时间
    float CalStartTime()
    {
        mLocalToCanvas = canvas.rootCanvas.transform.localToWorldMatrix.inverse * transform.localToWorldMatrix;
        Vector4 v = GetDrawingDimensions(false);
        List<Vector2> vertexList = new List<Vector2> {
            new Vector2(v.x, v.y),
            new Vector2(v.x, v.w),
            new Vector2(v.z, v.y),
            new Vector2(v.z, v.w)
        };
        //如果目标点在图片范围中，则起始时间为零，忽视subCube的大小，方便计算
        if(TargetX >= v.x && TargetX <= v.z && TargetY >= v.y && TargetY <= v.w)
        {
            return 0;
        }
        //如果目标点在图片范围外，则最近点必是四个顶点之一
        Vector3 canvasTarget = mLocalToCanvas * (new Vector4(TargetX, TargetY, 0, 1));
        float nearestDistance = float.MaxValue;
        foreach (Vector2 vertex in vertexList)
        {
            Vector3 canvasPoint = mLocalToCanvas * (new Vector4(vertex.x, vertex.y, 0, 1));
            float distance = (canvasTarget - canvasPoint).magnitude;
            nearestDistance = nearestDistance < distance ? nearestDistance : distance;
        }
        //通过shader使用的公式反推最近的方块移动开始移动的时间
        return nearestDistance * Interval;
    }

    //将mesh信息设为普通图片mesh
    void SetNormalMesh(VertexHelper vh,Vector4 v,Vector4 uv,Color color32)
    {
        vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(uv.x, uv.y));
        vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(uv.x, uv.w));
        vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(uv.z, uv.w));
        vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(uv.z, uv.y));
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }
    
    //重写Image的OnpopultateMesh，用于拆分还原图片的mesh
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
            //因为输入值不一定能能被图片大小整除，所以做一个近似的修正     
            Vector2 subRect = new Vector2(SubRectX, SubRectY);
            int ColNum = Mathf.CeilToInt((v.z - v.x) / subRect.x);
            int LineNum = Mathf.CeilToInt((v.w - v.y) / subRect.y);
            subRect = new Vector2((v.z - v.x) / ColNum, (v.w - v.y) / LineNum);
            //计算每个小方块对应的uv块
            Vector2 uvDelta = new Vector2(subRect.x / (v.z - v.x), subRect.y / (v.w - v.y));
            //拆分图片mesh
            int startNum = 0;
            for (int i = 0; i < LineNum; i++)
            {
                for (int j = 0; j < ColNum; j++)
                {
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

                    //用每个小方块的左下角的值作为方块的位置，使用TEXTURE1出入shader中
                    AddVertAndExtraInfo(vh, new Vector3(leftBottom.x, leftBottom.y), color32, new Vector2(leftBottomUV.x, leftBottomUV.y), leftBottom.x, leftBottom.y);
                    AddVertAndExtraInfo(vh, new Vector3(leftTop.x, leftTop.y), color32, new Vector2(leftTopUV.x, leftTopUV.y), leftBottom.x, leftBottom.y);
                    AddVertAndExtraInfo(vh, new Vector3(rightBottom.x, rightBottom.y), color32, new Vector2(rightBottomUV.x, rightBottomUV.y), leftBottom.x, leftBottom.y);
                    AddVertAndExtraInfo(vh, new Vector3(rightTop.x, rightTop.y), color32, new Vector2(rightTopUV.x, rightTopUV.y), leftBottom.x, leftBottom.y);

                    vh.AddTriangle(startNum + 0, startNum + 1, startNum + 3);
                    vh.AddTriangle(startNum + 0, startNum + 3, startNum + 2);
                    startNum += 4;
                }
            }
        }
    }
    
    //添加顶点信息
    public void AddVertAndExtraInfo(VertexHelper vh,Vector3 position, Color32 color, Vector2 uv0, float info1,float info2)
    {
        UIVertex v = new UIVertex();
        v.position = position;
        v.color = color;
        v.uv0 = uv0;
        v.uv1 = new Vector2(info1, info2);
        vh.AddVert(v);
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
