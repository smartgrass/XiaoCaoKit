using System.Linq;
using NaughtyAttributes;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 编辑器模式检查关卡预制体使用
    /// 检查必要的关卡组件是否存在，如果不存在则创建
    /// </summary>
    public class LevelPrefabChecker : MonoBehaviour
    {
        public Transform startPoint;
        public Transform endPoint;
        public Transform levelAction;

        [Button("检查关卡组件")]
        void Check()
        {
            // 检测endPoint 和 LevelAction 是否存在
            // 检测startPoint是否存在
            // 没有则创建
            CheckOrCreateObject("startPoint", ref startPoint);
            CheckOrCreateObject("endPoint", ref endPoint);
            CheckOrCreateLevelAction();
        }

        [Button("检查关卡通关")]
        void CheckLevelFinish()
        {
            //检查LevelAction中的所有Group中是否有MapMsg为LevelFinish的,没有则输出Error
            if (levelAction.GetComponentsInChildren<EnemyGroupComponent>()
                .Any(x => x.mapMsg == LocalizeKey.LevelFinish))
            {
                Debug.Log("关卡通关已存在");
            }
            else
            {
                Debug.LogError("关卡通关不存在");
            }
        }

        /// <summary>
        /// 检查或创建简单对象
        /// </summary>
        /// <param name="objectName">对象名称</param>
        /// <param name="objRef">对象引用</param>
        private void CheckOrCreateObject(string objectName, ref Transform objRef)
        {
            Transform obj = transform.Find(objectName);
            if (obj == null)
            {
                GameObject newObj = CreateChildObject(objectName);
                objRef = newObj.transform;
                Debug.Log($"已创建 {objectName} 对象");
            }
            else
            {
                objRef = obj;
                Debug.Log($"{objectName} 对象已存在");
            }
        }

        /// <summary>
        /// 检查或创建关卡动作容器对象
        /// </summary>
        private void CheckOrCreateLevelAction()
        {
            Transform levelActionObj = transform.Find("LevelAction");
            if (levelActionObj == null)
            {
                GameObject newLevelActionObj = CreateChildObject("LevelAction");
                levelAction = newLevelActionObj.transform;

                // 添加子对象 Enemys
                CreateChildObject("Enemys", levelAction);

                Debug.Log("已创建 LevelAction 对象及其子对象 Enemys");
            }
            else
            {
                levelAction = levelActionObj;
                Debug.Log("LevelAction 对象已存在");
            }
        }

        /// <summary>
        /// 创建子对象的通用方法
        /// </summary>
        /// <param name="name">对象名称</param>
        /// <param name="parent">父级变换，默认为当前组件的变换</param>
        /// <returns>创建的游戏对象</returns>
        private GameObject CreateChildObject(string name, Transform parent = null)
        {
            if (parent == null)
                parent = transform;

            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            return obj;
        }
    }
}