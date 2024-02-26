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

        public void OnTrigger();

        public void OnUpdate();

        public void OnFinish(bool hasTrigger);

    }


    public class CommandFinder : Singleton<CommandFinder>
    {
        private Dictionary<string, Type> commandTypes;

        public IXCCommand GetCommand(string name)
        {
            if (commandTypes.TryGetValue(name, out Type commandType))
            {
                var commandInstance = Activator.CreateInstance(commandType) as IXCCommand;
                return commandInstance;
            }
            return null;
        }
        protected override void Init()
        {
            commandTypes = GetAllCommandTypes();
        }

        private Dictionary<string, Type> GetAllCommandTypes()
        {
            // Find all types in the assembly that inherit from Command
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IXCCommand).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                .ToDictionary(type => type.Name, type => type);

            return types;
        }

    }
}
