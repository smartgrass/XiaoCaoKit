using NaughtyAttributes;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;
public class SoulTrailTween : MonoBehaviour
{
    public float starDuration = 0.5f;
    public float angleSpeed = 1.0f;
    public float radius = 5.0f;
    public float ySpeed = 0.5f;
    public Vector3 endOffset = Vector3.up;
    public UnityEvent endEvnet;
    public Action rewardAct;

    private float timer;
    private float angle;

    public GameObject[] hideObjects;

    [ReadOnly]
    public Vector3 startPoint;
    [ReadOnly]
    public Transform targetTf;

    private Vector3 tempVec3;
    public Vector3 GetEndPoint
    {
        get {
            if (targetTf)
            {
                tempVec3 = targetTf.position + endOffset;
            }
            return tempVec3;
        }
    }


    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void Play()
    {
        foreach (var obj in hideObjects)
        {
            obj.SetActive(true);
        }
        StartCoroutine(MoveAlongCurve());
    }

    IEnumerator MoveAlongCurve()
    {
        float speed = radius * angleSpeed + ySpeed;
        float maxSpeed = speed * 2;
        timer = 0;
        //绕圈加插值
        while (timer < starDuration)
        {
            float t = timer / starDuration;
            angle += angleSpeed * Time.deltaTime;
            // 计算x和z坐标
            float x = startPoint.x + radius * Mathf.Cos(angle);
            float z = startPoint.z + radius * Mathf.Sin(angle);
            Vector3 curPos = transform.position;
            Vector3 targetPos = new Vector3(x, transform.position.y + ySpeed * Time.deltaTime, z);

            float lerpT = Mathf.Max(0.1f, t);
            transform.position = Vector3.Lerp(transform.position, targetPos, lerpT);
            transform.rotation = Quaternion.LookRotation(targetPos - curPos);
            timer += Time.deltaTime;
            yield return null;
        }
        yield return null;

        float distance = Vector3.Distance(transform.position, GetEndPoint);
        while (distance > 0.2f)
        {
            Vector3 dir = transform.forward;
            Vector3 targetDir = GetEndPoint - transform.position;
            Vector3 lerpDir = Vector3.Lerp(dir, targetDir, 0.1f);
            transform.rotation = Quaternion.LookRotation(lerpDir);

            transform.position += transform.forward * speed * Time.deltaTime;

            speed = Mathf.Clamp(speed + Time.deltaTime, 0.1f, maxSpeed);

            distance = Vector3.Distance(transform.position, GetEndPoint);
            yield return null;
        }

        transform.position = GetEndPoint;
        foreach (var obj in hideObjects)
        {
            obj.SetActive(false);
        }

        if (transform.TryGetComponent<IExecute>(out IExecute execute))
        {
            execute.Execute();
        }

        endEvnet?.Invoke();
        rewardAct?.Invoke();
        gameObject.SetActive(false);

    }

}