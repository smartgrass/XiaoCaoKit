using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XiaoCao;

public class TestRole : IdComponent
{
    public int skillId;
    [Button]
    void StartSkill()
    {
        this.EntityMsg(EntityMsgType.StartSkill, skillId);
    }

    private void Update()
    {

    }
}
