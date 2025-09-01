using System;
using System.Collections.Generic;

namespace XiaoCao
{
    [Serializable]
    public class StoryProgress
    {
        // 已完成的剧情ID列表
        public HashSet<string> completedStoryIds = new HashSet<string>();
        
        // 当前正在进行的剧情ID
        public string currentStoryId = "";
        
        // 剧情变量，用于存储剧情中的各种状态
        public Dictionary<string, int> storyVariables = new Dictionary<string, int>();
        
        // 添加已完成的剧情
        public void AddCompletedStory(string storyId)
        {
            completedStoryIds.Add(storyId);
        }
        
        // 检查剧情是否已完成
        public bool IsStoryCompleted(string storyId)
        {
            return completedStoryIds.Contains(storyId);
        }
        
        // 设置当前剧情
        public void SetCurrentStory(string storyId)
        {
            currentStoryId = storyId;
        }
        
        // 设置剧情变量
        public void SetStoryVariable(string variableName, int value)
        {
            storyVariables[variableName] = value;
        }
        
        // 获取剧情变量
        public int GetStoryVariable(string variableName, int defaultValue = 0)
        {
            if (storyVariables.TryGetValue(variableName, out int value))
            {
                return value;
            }
            return defaultValue;
        }
    }
    
    
    /* 示例
     // 记录完成剧情
    PlayerSaveData.LocalSavaData.storyProgress.AddCompletedStory("story_001");

    // 检查剧情是否已完成
    bool isCompleted = PlayerSaveData.LocalSavaData.storyProgress.IsStoryCompleted("story_001");

    // 设置当前剧情
    PlayerSaveData.LocalSavaData.storyProgress.SetCurrentStory("story_002");

    // 设置剧情变量
    PlayerSaveData.LocalSavaData.storyProgress.SetStoryVariable("npc_friendship", 50);

    // 获取剧情变量
    int friendship = PlayerSaveData.LocalSavaData.storyProgress.GetStoryVariable("npc_friendship");
     */
}