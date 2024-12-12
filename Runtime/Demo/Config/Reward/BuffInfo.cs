using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;

namespace XiaoCao
{
    [Serializable]
    //词条Buff的最基础单位
    public class BuffInfo : PowerModel
    {
        public EBuff buff;

        public float[] addInfo;
    }
    /// <summary>
    /// Buff容器, 可以承载多个词条
    /// </summary>
    public class BuffItem
    {
        public int level; //星级

        public List<BuffInfo> buffs; //词条

        public int GetMaxBuffCount => level + 1; // 0级1个词条, 1级两个词条
    }

    public static class BuffItemHelper
    {
        public static BuffItem GenRandomBuffItem(int level, List<BuffInfo> buffs)
        {
            BuffItem group = new BuffItem();

            group.buffs = RandomHelper.GetRandomList<BuffInfo>(buffs, level);

            return group;
        }
    }
}
