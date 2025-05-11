using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Test_Hit : MonoBehaviour
{
    public Circle Circle;
    public Rectangle Rectangle;
    public Sector Sector;

    // Update is called once per frame
    void Update()
    {
        // Sector hit test.
        var circlePos = Circle.transform.position.ToXZ();
        var circleRadius = Circle.Radius;
        var sectorPos = Sector.transform.position.ToXZ();
        var sectorDir = Sector.transform.forward.ToXZ();
        var sectorTheta = Sector.Angle / 2f * Mathf.Deg2Rad;
        var sectorHit =
            MathRangeHitTool.IsSectorDiskIntersect(sectorPos, sectorDir, sectorTheta, Sector.Radius, circlePos, circleRadius);
        Sector.IsHit = sectorHit;

        // OBB hit test.
        var obbPos = Rectangle.transform.position.ToXZ();
        var obbSize = new Vector2(Rectangle.Width, Rectangle.Height);
        
        // var isObbHit = MathUtils.IsAabbDiskIntersect(obbPos, obbSize, circlePos, circleRadius);
        var isObbHit =
            MathRangeHitTool.IsObbDiskIntersect(obbPos, obbSize, Rectangle.transform.right.ToXZ(), circlePos, circleRadius);

        Rectangle.IsHit = isObbHit;
        
        Circle.IsHit = sectorHit || isObbHit;
    }
}
