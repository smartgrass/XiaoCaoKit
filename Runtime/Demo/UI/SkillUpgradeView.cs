using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace XiaoCao
{

    public class SkillUpgradeView: MonoBehaviour{
		public TMP_Text nameText;
		public TMP_Text desText;
		public Transform itemsParent;

		private List<GameObject> itemCells = new List<GameObject>();
        private AssetPool itemCellPool;
		private bool isInit;


		public void Init()
		{
			if (!isInit)
			{
				isInit = true;
				itemCellPool = new AssetPool(UIPrefabMgr.Inst.itemCellPrefab);
			}
		}


        public void ShowSkill(int skillIndex){
			Init();
            gameObject.SetActive(true);
			//TODO 本地化

			//TODO get合成材料

			List<Item> list = new List<Item>();
			foreach (var item in list)
			{
				var cellGo = itemCellPool.Get();
				ItemCell cell = cellGo.GetComponent<ItemCell>();
				cell.SetItem(item,true);

            }
		}

		void Update()
		{
            if (Input.GetMouseButtonDown(0)) // 假设我们检测鼠标左键点击
            {
                Vector2 mousePosition = Input.mousePosition;
                if (UIClickDetector.IsPointerOverTarget(mousePosition,(transform as RectTransform).rect))
                {
                    // 点击发生在UI元素上
                    Debug.Log("Clicked on the UI");
                }
                else
                {
                    // 点击未发生在UI元素上
                    Hide();
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(transform.GetComponent<RectTransform>(), eventData.position, eventData.pressEventCamera))
            {
                Hide();
            }
        }

        public void Hide()
		{
            gameObject.SetActive(false);
		}

	}


    public static class UIClickDetector
    {
        public static bool IsPointerOverGameObject(Vector2 screenPosition)
        {
            PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
            pointerEventData.position = screenPosition;


            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerEventData, results);

            return results.Count > 0;
        }        
        
        public static Vector2 ToCamvasPos(Canvas canvas,Vector2 mousePosition)
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePosition);//屏幕坐标转换世界坐标
            Vector2 uiPos = canvas.transform.InverseTransformPoint(worldPos);//世界坐标转换位本地坐标


            return uiPos;
        }

        public static bool IsPointerOverTarget(Vector2 mousePosition, Rect rect)
        {
            var screenPosition =ToCamvasPos(UIMgr.Inst.topCanvas, mousePosition);
            return rect.Contains(screenPosition);
        }
    }

}