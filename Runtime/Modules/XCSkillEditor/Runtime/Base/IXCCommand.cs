using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 给技能做自定义操作, 比如输入检测
    /// </summary>
    public interface IXCCommand
    {
        public XCTask task { get; set; }
        public XCCommondEvent curEvent { get; set; }

        public RoleType roleType { get; }

        public void OnTrigger();

        public void OnUpdate(int frame, float timeSinceTrigger);

        public void OnFinish(bool hasTrigger);
        void Init(BaseMsg baseMsg);
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
