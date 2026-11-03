using System;
using System.Collections.Generic;
using UnityEngine;

public class ActiveGameObjectExec : DelayMonoExecute
{
    public bool isActive = true;

    public List<GameObject> list;

    public override void ExecuteOnTime()
    {
        int length = list.Count;
        for (int i = 0; i < length; i++)
        {
            if (list[i])
            {
                list[i].SetActive(isActive);
            }

        }
    }

}