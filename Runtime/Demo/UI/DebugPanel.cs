#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using XiaoCao;
using Debug = UnityEngine.Debug;

public class DebugPanel : SettingPanel
{
	public override UIPanelType panelType => UIPanelType.DebugPanel;



    public const string DebugGUI_IsShow = "DebugGUI/IsShow";

	private const string DebugGUI_IsOtherShowing = "DebugGUI/IsOtherShowing";

	public override void Init(){
		if (IsInited)
		{
			return;
		}
		base.Init();
        
		gameObject.SetActive(false);
		Prefabs.gameObject.SetActive(false); 
		IsInited = true;
	}

	#region  Editor
	private void Awake()
	{
#if UNITY_EDITOR
		EditorApplication.pauseStateChanged += PauseStateChanged;
#endif
	}

	private void OnDestroy()
	{
#if UNITY_EDITOR
		EditorApplication.pauseStateChanged -= PauseStateChanged;
#endif
	}
    
    
#if UNITY_EDITOR
	private void PauseStateChanged(PauseState state)
	{
		if (state == PauseState.Paused)
		{
			Debug.Log("--- Pause all movement");
			foreach (var item in RoleMgr.Inst.roleDic)
			{
				item.Value.roleData.movement.isMovingThisFrame = false;
			} 
		}
	}
#endif
	#endregion
    
	void OnOpenBtn()
	{
		// isShowPanel = !isShowPanel;
		// if (isShowPanel)
		// {
		//     Show();
		// }
		// else
		// {
		//     Hide();
		// }
		// DebugGUI_IsOtherShowing.SetKeyBool(isShowPanel);
	}
}