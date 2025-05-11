
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;
using Cinemachine.Utility;

//矩形排列, TODO: 圆形排列
public static class MathLayoutTool
{
    #region   矩形排列XYZ
    //一维化
    public class GridArrangementTool

    {

        public int XLen { get; private set; } // X

        public int ZLen { get; private set; } // Z

        public int Layers { get; private set; } // Y方向上的层数

        public bool IsHollow { get; private set; } // 是否为空心网格



        public GridArrangementTool(int x, int z, int y, bool isHollow)
        {
            XLen = x;
            ZLen = z;
            Layers = y;
            IsHollow = isHollow;
        }


        // 计算总单元格数的方法
        public int TotalCells()
        {
            if (!IsHollow)
            {
                return XLen * ZLen * Layers;
            }
            else
            {
                int count = XLen * ZLen * Layers;

                int off = GetMinMult(XLen - 2) * (ZLen - 2) * Layers;

                return count - off;
            }

        }

        private int GetMinMult(int num)
        {
            if (num < 0)
            {
                return 0;
            }
            return num;
        }



        // 通过x, y, z计算位置序号n的方法

        public int GetIndex(int x, int y, int z)
        {
            if (x < 0 || x >= XLen || y < 0 || y >= Layers || z < 0 || z >= ZLen)
            {
                throw new ArgumentOutOfRangeException("Coordinates are out of grid bounds.");
            }
            //TODO


            return y * ZLen * XLen + z * XLen + x;

        }



        // 通过序号n计算出x, y, z的方法

        public (int, int, int) GetCoordinates(int index)
        {
            if (index < 0 || index >= TotalCells())
            {
                throw new ArgumentOutOfRangeException("Index is out of range.");
            }
            if (IsHollow)
            {
                int pY = 0;
                if (index < XLen * 2)
                {

                    //前边界
                    if (index < XLen * 1)
                    {
                        return (index, pY, 0);
                    }
                    //后边界
                    else
                    {
                        return (index - XLen, pY, ZLen - 1);
                    }
                }
                //侧边
                else
                {
                    int delta = index - (XLen * 2);
                    if (delta < (ZLen - 2) * 1)
                    {
                        //+1 排除前面
                        return (0, pY, delta + 1);
                    }
                    else
                    {
                        return (XLen - 1, pY, delta - (ZLen - 2) + 1);
                    }
                }
            }

            int y = index / (ZLen * XLen);
            index %= ZLen * XLen;
            int z = index / XLen;
            int x = index % XLen;
            return (x, y, z);
        }
    }

    #endregion

    #region   矩形排列

    public enum Alignment
    {
        Left,
        Center,
        Right
    }
    private const int objectWidth = 0;
    private const int objectHeight = 0;

    //矩形排列
    //int x = i % xCount;
    //int y = i / xCount;
    public static Vector2 GetRectPos(int x, int y, int xCount, Alignment alignment, float spacingX = 10, float spacingY = 10)
    {
        float startX = 0;
        float startY = 0;

        startY = y * (objectHeight + spacingY);

        float endX = startX + x * (objectWidth + spacingX);
        switch (alignment)
        {
            case Alignment.Center:
                endX += (objectWidth - spacingX * (xCount - 1)) / 2;
                break;
            case Alignment.Right:
                endX += (objectWidth - spacingX) * (xCount - 1);
                break;
        }
        //Left不需要任何处理
        return new Vector2(endX, startY);
    }


    #endregion

    #region 圆形排列 TODO
    /// <summary>
    /// 扇形排布
    /// </summary>
    /// <returns></returns>
    public static List<Vector3> GetSectorPoints(float angle, float radius, int angleStep = 15)
    {
        List<Vector3> vertices = new List<Vector3>();
        int segments = Mathf.CeilToInt(angle / (float)angleStep);

        for (int i = 0; i <= segments; i++)
        {
            float normalizedAngle = Mathf.Lerp(0, angle, i / (float)segments);
            float radian = Mathf.Deg2Rad * (normalizedAngle + 90);

            float x = Mathf.Cos(radian) * radius;
            float y = Mathf.Sin(radian) * radius;

            // 将顶点添加到列表
            vertices.Add(new Vector3(x, y, 0));
        }

        return vertices;
    }

    /// </summary>
    /// <param name="angledegree">弧度</param>
    /// <param name="outerRadius">外圈半径</param>
    /// <param name="Height"></param>
    /// <param name="segments">分割数</param>
    /// <param name="innerRadius">内圈半径</param>
    /// <returns></returns>
    public static Mesh GetSectorMesh(float angledegree, float outerRadius, float Height, int segments, float innerRadius = 0)
    {
        float angleRad = Mathf.Deg2Rad * angledegree;
        float angleStart = -angleRad / 2; // 扇形开始角度
        float angledelta = angleRad / segments;

        //扇环两条弧上, 一条弧上顶点的数量
        int vertexCount = segments + 1;

        float halfH = Height * 0.5f;
        //Vector3 halfHeightOffset = Vector3.up * Height * 0.5f;

        // 四条弧总共的顶点数量
        Vector3[] vertex = new Vector3[vertexCount * 2 * 2];
        // 上表面两条弧的顶点
        // 大扇形弧上的顶点
        float angleCur = angleStart;
        for (int i = 0; i < vertexCount; i++)
        {
            float cosA = Mathf.Cos(angleCur);
            float sinA = Mathf.Sin(angleCur);
            vertex[i] = new Vector3(outerRadius * cosA, halfH, outerRadius * sinA);
            angleCur += angledelta; // 从左到右
        }
        // 小扇形弧上的顶点
        angleCur = angleStart;
        for (int i = vertexCount; i < vertexCount * 2; i++)
        {
            float cosA = Mathf.Cos(angleCur);
            float sinA = Mathf.Sin(angleCur);
            vertex[i] = new Vector3(innerRadius * cosA, halfH, innerRadius * sinA);
            angleCur += angledelta; // 从左到右
        }
        // 下表面两条弧的顶点
        for (int i = vertexCount * 2; i < vertexCount * 4; i++)
        {
            vertex[i] = vertex[i - vertexCount * 2] - Vector3.up * Height;
        }

        // 上，左，前，表面三角形数量
        int topTriangleCount = segments * 2;
        int leftTriangleCount = 2;
        int frontTriangleCount = segments * 2;

        // 全部数量
        int triangleCount = (topTriangleCount + leftTriangleCount + frontTriangleCount) * 2;
        // 全部三角形顶点索引
        int[] triangles = new int[triangleCount * 3]; // 三角形顶点

        // 上， 下表面三角形，顶点索引
        int verticeIndex = 0;
        int startTriangleIndex = 0;
        for (int i = startTriangleIndex; i < topTriangleCount * 2 * 3; i += 12)
        {
            //上表面
            int startIndex = verticeIndex;
            // 逆时针， 第一个三角形
            triangles[i] = startIndex;
            triangles[i + 1] = vertexCount + startIndex + 1;
            triangles[i + 2] = startIndex + 1;
            // 逆时针， 第二个三角形
            triangles[i + 3] = startIndex;
            triangles[i + 4] = vertexCount + startIndex;
            triangles[i + 5] = vertexCount + startIndex + 1;

            // 下表面
            startIndex = verticeIndex + vertexCount * 2;
            // 顺时针， 第一个三角形
            triangles[i + 6] = startIndex;
            triangles[i + 7] = startIndex + 1;
            triangles[i + 8] = vertexCount + startIndex + 1;
            // 顺时针， 第二个三角形
            triangles[i + 9] = startIndex;
            triangles[i + 10] = vertexCount + startIndex + 1;
            triangles[i + 11] = vertexCount + startIndex;
            verticeIndex++;
        }
        startTriangleIndex = topTriangleCount * 2 * 3;

        // 前，后表面三角形，顶点索引
        verticeIndex = 0;
        for (int i = startTriangleIndex; i < startTriangleIndex + frontTriangleCount * 2 * 3; i += 12)
        {
            //前表面 ，第一条弧和第三条弧组成的面
            int startIndex = verticeIndex;
            int startVertexCount = vertexCount * 2;
            // 顺时针， 第一个三角形
            triangles[i] = startIndex;
            triangles[i + 1] = startIndex + 1;
            triangles[i + 2] = startVertexCount + startIndex + 1;
            // 顺时针， 第二个三角形
            triangles[i + 3] = startIndex;
            triangles[i + 4] = startVertexCount + startIndex + 1;
            triangles[i + 5] = startVertexCount + startIndex;

            // 后表面， 第二条弧和第四条弧组成的面
            startIndex = verticeIndex + vertexCount;
            // 逆时针， 第一个三角形
            triangles[i + 6] = startIndex;
            triangles[i + 7] = startVertexCount + startIndex + 1;
            triangles[i + 8] = startIndex + 1;
            // 逆时针， 第二个三角形
            triangles[i + 9] = startIndex;
            triangles[i + 10] = startVertexCount + startIndex;
            triangles[i + 11] = startVertexCount + startIndex + 1;
            verticeIndex++;
        }
        startTriangleIndex += frontTriangleCount * 2 * 3;

        // 左，右 表面三角形，顶点索引
        verticeIndex = 0;
        for (int i = startTriangleIndex; i < startTriangleIndex + leftTriangleCount * 2 * 3; i += 12)
        {
            //左表面 ，四条弧的左端点组成的面
            int startIndex = verticeIndex;
            // 逆时针， 第一个三角形
            triangles[i] = startIndex;
            triangles[i + 1] = vertexCount * 3;
            triangles[i + 2] = vertexCount;
            // 逆时针， 第二个三角形
            triangles[i + 3] = startIndex;
            triangles[i + 4] = vertexCount * 2;
            triangles[i + 5] = vertexCount * 3;

            // 右表面， 四条弧的右端点组成的面
            // 顺时针， 第一个三角形
            triangles[i + 6] = vertexCount - 1;
            triangles[i + 7] = vertexCount * 2 - 1;
            triangles[i + 8] = vertexCount * 4 - 1;
            // 顺时针， 第二个三角形
            triangles[i + 9] = vertexCount - 1;
            triangles[i + 10] = vertexCount * 4 - 1;
            triangles[i + 11] = vertexCount * 3 - 1;
            verticeIndex++;
        }

        //uv:
        // Vector2[] uvs = new Vector2[vertexCount * 2];
        // for (int i = 0; i < vertexCount; i++)
        // {
        //     uvs[i] = new Vector2(vertex[i].x / outerRadius / 2 + 0.5f, vertex[i].z / outerRadius / 2 + 0.5f);
        // }
        // for (int i = vertexCount; i < vertexCount * 2; i++)
        // {
        //     uvs[i] = new Vector2(vertex[i].x / innerRadius / 2 + 0.5f, vertex[i].z / innerRadius / 2 + 0.5f);
        // }

        //负载属性与mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertex;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        // mesh.uv = uvs;
        return mesh;
    }
    #endregion
}

public static class MathTool
{
    #region Value
    //先慢后快 t -> [0,1]
    public static float RLerp(float start, float end, float t)
    {
        return (end - start) * t;
    }

    public static bool InRange(float value, float closedLeft, float openRight)
    {
        return value >= closedLeft && value < openRight;
    }
    /// <summary>
    /// probability 概率
    /// </summary>
    /// <returns></returns>
    public static bool IsInRandom(float probability)
    {
        // 生成0到1之间的随机数
        float randomValue = Random.Range(0f, 1f);

        // 如果随机数小于等于概率值，则触发事件
        if (randomValue <= probability)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    /// <summary>
    /// 随机球点
    /// </summary>
    /// <param name="innerRadius"></param>
    /// <param name="outerRadius"></param>
    /// <returns></returns>
    static Vector3 RandomPointInShell(float innerRadius, float outerRadius)
    {
        float radius = Random.Range(innerRadius, outerRadius);

        // 生成两个在 [0, 2π] 之间的随机角度（θ 和 φ）
        float theta = Random.Range(0, 2 * Mathf.PI);

        // [0, π]
        float phi = Mathf.Acos(Random.Range(-1f, 1f));

        // 将球坐标转换为笛卡尔坐标
        float x = radius * Mathf.Sin(phi) * Mathf.Cos(theta);
        float y = radius * Mathf.Sin(phi) * Mathf.Sin(theta);
        float z = radius * Mathf.Cos(phi);
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// 值映射, 比如原本0~1的0.4, 映射到0~100,就是40
    /// </summary>
    /// <returns></returns>
    public static float ValueMapping(float value, float from, float to, float newFrom, float newTo)
    {
        float p = (value - from) / (to - from);
        p = Mathf.Clamp01(p);

        float newValue = p * (newTo - newFrom) + newFrom;
        return newValue;
    }

    //判断浮点数是否相等
    public static bool IsFEqual(this float value, float value2)
    {
        return Math.Abs(value - value2) < 0.00001f;
    }
    //返回float最近的Int->四舍五入
    public static void GetIntExample(float value)
    {
        //向上取整 如-3.2 -> -3 ; 4.1->5
        Mathf.CeilToInt(value);
        //向下取整
        Mathf.FloorToInt(value);
        //进行标准的四舍五入
        Mathf.RoundToInt(value);
        //小数点移除
        int newInt = (int)value;
    }

    #endregion
    #region Vector & Rotate
    public static bool IsZore(this Vector2 v)
    {
        return v == Vector2.zero || float.IsNaN(v.x) || float.IsNaN(v.y);
    }
    public static bool IsZore(this Vector3 v)
    {
        return v == Vector3.zero;
    }
    public static bool IsZoreOrNaN(this Vector3 v)
    {
        return v == Vector3.zero || float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z); ;
    }

    public static Vector3 SetY(this Vector3 v, float value)
    {
        return new Vector3(v.x, value, v.z);
    }

    public static Vector3 SetX(this Vector3 v, float value)
    {
        return new Vector3(value, v.y, v.z);
    }

    public static Vector3 SetZ(this Vector3 v, float value)
    {
        return new Vector3(v.x, v.y, value);
    }


    //排除y的距离计算
    public static float GetHorDistance(Vector3 a, Vector3 b)
    {
        float num = a.x - b.x;
        float num3 = a.z - b.z;
        return (float)Math.Sqrt(num * num + num3 * num3);
    }

    /// <summary>
    /// 旋转向量,忽略y轴 
    /// </summary>
    public static Vector3 RotateY(Vector3 dir, float angle)
    {
        //angle旋转角度 axis围绕旋转轴 position自身坐标 自身坐标 center旋转中心
        //完整公式:  Quaternion.AngleAxis(angle, axis) * (position - center) + center;
        //Vec3 = R * Vec3
        return Quaternion.AngleAxis(angle, Vector3.up) * (dir);
    }

    public static Vector3 GetForwordDirection(Quaternion rotation)
    {
        //forward 可以自定义,默认是正前方
        Vector3 forward = rotation * Vector3.forward;
        return forward;
    }

    /// <summary>
    /// 二维向量旋转
    /// </summary>
    public static Vector2 Rotate(Vector2 vector, float angleInDeg)
    {
        float angleInRad = Mathf.Deg2Rad * angleInDeg;
        float cosAngle = Mathf.Cos(angleInRad);
        float sinAngle = Mathf.Sin(angleInRad);

        float x = vector.x * cosAngle - vector.y * sinAngle;
        float y = vector.x * sinAngle + vector.y * cosAngle;
        return new Vector2(x, y);
    }

    /// <summary>
    /// 向量绕某点旋转
    /// 思路:将向量移动到原点, 旋转后再移回
    /// </summary>
    public static Vector2 RotateAround(this Vector2 vector, float angleInDeg, Vector2 axisPosition)
    {
        return Rotate((vector - axisPosition), (angleInDeg)) + axisPosition;
    }


    //角度转二维向量
    //从正右方开始计算,逆时针,90度为正上方
    public static Vector2 AngleToVector(float angleInDegrees)
    {
        double angleInRadians = angleInDegrees * (Math.PI / 180.0f);
        double xComponent = Math.Cos(angleInRadians);
        double yComponent = Math.Sin(angleInRadians);

        return new Vector2((float)xComponent, (float)yComponent);
    }

    //二维向量转角度,正右方为0角
    public static float VectorToAngle(this Vector2 vector)
    {
        // 计算向量相对于 x 轴的角度（弧度）
        double angleRadians = Math.Atan2(vector.y, vector.x);
        // 将弧度转换为角度
        double angleDegrees = angleRadians * 180.0 / Math.PI;
        // 确保角度在 0 到 360 范围内
        if (angleDegrees < 0)
        {
            angleDegrees += 360;
        }
        return (float)angleDegrees;
    }

    //计算两向量夹角, 有正负号, 正顺时针
    public static float SignedAngleY(Vector3 from, Vector3 to)
    {
        return Vector2.SignedAngle(from.ToXZ(), to.ToXZ());
    }


    /// <summary>
    /// 获取两向量的夹角 （角度范围：-180~180度）
    /// 左正右负, 锐角为前,钝角为后
    /// </summary>
    public static float GetDirectionSinAngle(Vector3 playerForward, Vector3 direction)
    {
        // 计算与玩家前方的夹角
        float angle = Vector3.Angle(playerForward, direction);

        // 判断左右方向
        Vector3 cross = Vector3.Cross(playerForward, direction);
        if (cross.y < 0) angle = -angle;

        return angle;
    }

    public static Quaternion ForwardToRotation(Vector3 forward)
    {
        //对于角色的Y轴一般向上的
        return Quaternion.LookRotation(forward, Vector3.up);
    }

    ///矩阵相关
    /// <summary>
    /// 局部和世界空间转换 示例
    /// </summary>
    public static void SpaceExample(Transform tf, Vector3 worldPos, Vector3 localPos)
    {
        //世界转局部 坐标
        localPos = tf.TransformPoint(worldPos);
        //局部转世界
        worldPos = tf.InverseTransformPoint(localPos);
        //向量同理...
        tf.TransformDirection(worldPos);

        //获取旋转矩阵
        Quaternion rotation = tf.rotation;
        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(rotation);
        //或者
        Matrix4x4 localToWorldMatrix = tf.localToWorldMatrix;
        //在没有Transfrom的情况需要使用下面的方法计算
        localPos = WorldToLocalPos(rotationMatrix, worldPos);
    }

    public static Vector3 WorldToLocalPos(Matrix4x4 rotationMatrix, Vector3 worldPos)
    {
        return rotationMatrix.inverse.MultiplyPoint3x4(worldPos);
        //当然等价于 WorldToLocalDir(rotation,worldPos-centerPos)
    }

    public static Vector3 WorldToLocalDir(Matrix4x4 rotationMatrix, Vector3 worldDir)
    {
        return rotationMatrix.inverse.MultiplyVector(worldDir);
    }

    //局部空间向量 转 世界空间
    public static Vector3 LocalToWorldDir(Matrix4x4 localToWorldMatrix, Vector3 localDir)
    {
        return localToWorldMatrix.MultiplyVector(localDir);
    }


    //旋转向目标方向, 忽略y轴
    public static void RotaToPos(this Transform transform, Vector3 wordlPos, float lerp = 1)
    {
        wordlPos.y = transform.position.y; //保持同一高度
        Quaternion rotation = Quaternion.LookRotation(wordlPos - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, lerp);
    }

    public static void RotateY(this Transform transform, float angle)
    {
        transform.Rotate(Vector3.up, angle);
    }

    public static void RotateYMax(this Transform transform, float targetAngleInDegrees, float maxAngle)
    {
        // 将目标角度从度转换为弧度  
        float targetAngleInRadians = targetAngleInDegrees * Mathf.Deg2Rad;
        // 获取当前物体的Y轴旋转角度（以弧度为单位）  
        float currentAngleInRadians = transform.eulerAngles.y;
        // 将当前角度也转换为0-360度范围内的等效角度（以度为单位），以便计算差值  
        float currentAngleInDegrees = (currentAngleInRadians + 360f) % 360f;

        // 计算从当前角度到目标角度的差值（以度为单位）  
        float angleDifference = Mathf.Abs(targetAngleInDegrees - currentAngleInDegrees);

        // 确定旋转方向（顺时针或逆时针）  
        bool clockwise = targetAngleInDegrees > currentAngleInDegrees;

        // 如果差值大于100度，则限制旋转量为100度或-100度（取决于旋转方向）  
        float limitedAngleDifference = Mathf.Min(angleDifference, maxAngle);
        if (!clockwise) limitedAngleDifference = -limitedAngleDifference; // 逆时针旋转  

        // 计算旋转后的目标角度（以度为单位）  
        float newTargetAngleInDegrees = currentAngleInDegrees + limitedAngleDifference;

        // 将新的目标角度限制在0-360度范围内  
        newTargetAngleInDegrees = (newTargetAngleInDegrees + 360f) % 360f;

        // 将新的目标角度从度转换为弧度，并设置物体的Y轴旋转  
        float newTargetAngleInRadians = newTargetAngleInDegrees * Mathf.Deg2Rad;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, newTargetAngleInRadians, transform.eulerAngles.z);
    }



    #endregion

    #region 曲线
    //二阶贝塞尔
    public static Vector3 GetBezierPoint2(Vector3 begin, Vector3 end, Vector3 handle, float t)
    {
        float pow = Mathf.Pow(1 - t, 2);
        float x = pow * begin.x + 2 * t * (1 - t) * handle.x + t * t * end.x;
        float y = pow * begin.y + 2 * t * (1 - t) * handle.y + t * t * end.y;
        float z = pow * begin.z + 2 * t * (1 - t) * handle.z + t * t * end.z;
        return new Vector3(x, y, z);
    }
    //求导
    public static Vector3 GetBezierPoint2_Speed(Vector3 begin, Vector3 end, Vector3 handle, float t)
    {
        float pow_s = 2 * t - 2;
        float x = pow_s * begin.x + (2 - 4 * t) * handle.x + 2 * t * end.x;
        float y = pow_s * begin.y + (2 - 4 * t) * handle.y + 2 * t * end.y;
        float z = pow_s * begin.z + (2 - 4 * t) * handle.z + 2 * t * end.z;
        return new Vector3(x, y, z);
    }
    //获得尽量平滑的Handle点
    public static Vector3 GetAutoHandle(Vector3 A, Vector3 B, Vector3 C, float rate = 0.8f)
    {
        Vector3 AB = B - A;
        Vector3 BC = C - B;

        float angle = Vector3.Angle(AB, BC);


        //获得垂直向量
        Vector3 normalVector = Vector3.Cross(AB, BC);

        Vector3 panleVector = Vector3.Cross(AB, normalVector).normalized;


        Debug.Log($" dot {Vector3.Dot(panleVector, BC)} {angle} {Mathf.Sin(angle)}");

        if (Vector3.Dot(panleVector, BC) > 0)
        {
            panleVector = -panleVector;
        }


        //根据AB 与 BC的夹角, 越大handle点离AB中点M 越远
        float distance = Mathf.Sin(angle * Mathf.Deg2Rad) * AB.magnitude * rate;

        return panleVector * distance + (B + A) / 2;
    }

    //计算平面内向量 左右关系, 和视角有关系, unity中默认用y
    private static bool CheckCrossProduct(Vector3 a, Vector3 b)
    {
        // 计算叉积
        Vector3 crossProduct = Vector3.Cross(a, b);
        return crossProduct.y > 0;
    }


    //三阶段贝塞尔
    public static Vector3 GetBezierPoint3(float time, Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent)
    {
        float t = time;
        float u = 1f - t;
        float t2 = t * t;
        float u2 = u * u;
        float u3 = u2 * u;
        float t3 = t2 * t;

        Vector3 result =
            (u3) * startPosition +
            (3f * u2 * t) * startTangent +
            (3f * u * t2) * endTangent +
            (t3) * endPosition;

        return result;
    }


    public static Vector3 LinearVec3(Vector3 start, Vector3 end, float t)
    {
        end -= start;
        return end * t + start;
    }
    #endregion
}


//世界坐标转换相关
public static class WorldScreenHelper
{
    public static Vector2 WorldToAnchorPos(Vector3 position, RectTransform canvasRectTransform)
    {
        Vector3 screenPoint3 = Camera.main.WorldToScreenPoint(position);//世界坐标转换为屏幕坐标
        if (screenPoint3.z < 0)
        {
            //背面
            screenPoint3 = -screenPoint3;
        }
        Vector2 screenPoint = screenPoint3;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        screenPoint -= screenSize / 2;//将屏幕坐标变换为以屏幕中心为原点
        Vector2 anchorPos = screenPoint / screenSize * canvasRectTransform.sizeDelta;//缩放得到UGUI坐标
        return anchorPos;
    }
}