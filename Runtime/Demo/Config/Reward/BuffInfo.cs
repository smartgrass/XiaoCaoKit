using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;

namespace XiaoCao
{
    [Serializable]
    public class BuffInfo : PowerModel
    {
        public EBuff buff;

        public float[] AddInfo;
    }
    public class BuffGroup
    {
        public int level; //星级

        public List<BuffInfo> buffs; //词条

        public int GetMaxBuffCount => level + 1; // 0级1个词条, 1级两个词条
    }

    public static class BuffGroupHelper
    {
        //随机抽取
        public static BuffGroup GetBuffGroup(int level, List<BuffInfo> buffs)
        {
            BuffGroup group = new BuffGroup();

            group.buffs = RandomHelper.GetRandomList<BuffInfo>(buffs, level);

            return group;
        }
    }


}
