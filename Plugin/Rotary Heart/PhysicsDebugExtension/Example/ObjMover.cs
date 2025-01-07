using UnityEngine;

public class ObjMover : MonoBehaviour
{
    public Vector3 startPosition;
    public Vector3 endPosition;
    public bool move = false;

    private int direction = 6;

    // Update is called once per frame
    void Update()
    {
        if (!move)
            return;
        
        if (direction == 6)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, endPosition, 4 * Time.deltaTime);

            if (Vector3.Distance(transform.localPosition, endPosition) < 0.1f)
                direction = 4;
        }
        else
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, startPosition, 4 * Time.deltaTime);

            if (Vector3.Distance(transform.localPosition, startPosition) < 0.1f)
                direction = 6;
        }
    }
}
