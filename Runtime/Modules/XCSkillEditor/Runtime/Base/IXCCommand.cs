using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UnityEngine;

namespace XiaoCao
{
    /* 复制模板
    internal class XCCommand_XXXX : IXCCommand
    {
        public XCTask task { get; set; }
        public XCCommondEvent curEvent { get; set; }
        public bool IsTargetRoleType(RoleType roleType)
        {
            return true;
        }

        private Player0 Player0 => task.Info.role as Player0;

        public float minSwitchTime = 0.8f;
        public void Init(BaseMsg baseMsg){ }

        public void OnFinish(bool hasTrigger){ }

        public void OnTrigger(){}

        public void OnUpdate(int frame, float timeSinceTrigger) {}
    }
    */

    /// <summary>
    /// 给技能做自定义操作, 比如输入检测, 模板见上方注释
    /// </summary>
    public interface IXCCommand
    {
        public XCTask task { get; set; }
        public XCCommondEvent curEvent { get; set; }
        public void OnTrigger();
        public void OnUpdate(int frame, float timeSinceTrigger);
        public void OnFinish(bool hasTrigger);
        void Init(BaseMsg baseMsg);
        bool IsTargetRoleType(RoleType roleType);
    }




    public class XCCommandBinder : Singleton<XCCommandBinder>
    {
        private Dictionary<string, Type> commandTypeDic;

        public IXCCommand GetCommand(string name)
        {
            if (commandTypeDic.TryGetValue(name, out Type commandType))
            {
                var commandInstance = Activator.CreateInstance(commandType) as IXCCommand;
                return commandInstance;
            }
            return null;
        }



        protected override void Init()
        {
            commandTypeDic = GetAllCommandTypes();
        }

        public Dictionary<string, Type> GetAllCommandTypes()
        {
            // Find all types in the assembly that inherit from Command
            var typeDic = this.GetType().Assembly.GetTypes()
                .Where(type => typeof(IXCCommand).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                .ToDictionary(type => type.Name, type => type);

            return typeDic;
        }

    }
}
