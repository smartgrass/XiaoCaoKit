using UnityEngine;
using UnityEngine.UI;

public class UVScroller : MonoBehaviour
{
    public RawImage rawImage; // 引用RawImage组件  
    public Vector2 uvScrollVector = new Vector2(0.1f, 0f); // UV移动向量，x为水平滚动速度，y为垂直滚动速度  
   
    void Start()
    {
        if (rawImage == null)
            rawImage = GetComponent<RawImage>();
    }

    void Update()
    {
        var uvOffset = uvScrollVector * Time.deltaTime;

        var getRect = rawImage.uvRect;
        getRect.x += uvOffset.x;
        getRect.y += uvOffset.y;
        rawImage.uvRect = getRect;
    }
}
