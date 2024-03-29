﻿using DG.Tweening;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    public class HpBar : MonoBehaviour
    {
        public Image fillImage;

        public Image barImgSlow;

        private RectTransform healthBarRectTransform;

        public float referenceDistance = 15;

        public Vector3 offSet;

        private Camera mainCamera => CameraMgr.Main;


        //Color Setting
        public static Color PlayerColor = new Color(0,0.3f,1);
        public static Color EnemyColor = new Color(1,0,0);

        

        private void Start()
        {
            // 获取血条的RectTransform组件
            healthBarRectTransform = transform as RectTransform;
          
        }


        public void Init(RoleType roleType)
        {
            if (roleType == RoleType.Player)
            {
                fillImage.color = PlayerColor;
            }
            else
            {
                fillImage.color = EnemyColor;
            }

        }

        public void UpdatePostion(GameObject gameObject)
        {
            if (gameObject == null) return;
            // 获取目标的世界坐标
            Vector3 targetWorldPosition = gameObject.transform.position;

            // 将目标的世界坐标转换为屏幕坐标
            Vector3 targetScreenPosition = mainCamera.WorldToScreenPoint(targetWorldPosition);

            // 将血条的屏幕坐标设置为目标的屏幕坐标
            healthBarRectTransform.position = targetScreenPosition;

            // 计算缩放因子
            float scaleFactor = CalculateScaleFactor(mainCamera.fieldOfView, targetWorldPosition, mainCamera.transform.position);

            // 根据缩放因子调整血条的大小
            healthBarRectTransform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
        }

        public void UpdateHealthBar(float healthPercentage)
        {
            fillImage.fillAmount = healthPercentage;
        }

        // 根据视场角和目标到相机的距离计算缩放因子
        private float CalculateScaleFactor(float cameraFOV, Vector3 targetPosition, Vector3 camPos)
        {
            // 获取目标到相机的距离
            float distanceToCamera = Vector3.Distance(targetPosition, camPos);

            // 计算相机的透视缩放因子
            float perspectiveScale = referenceDistance / Mathf.Tan(Mathf.Deg2Rad * (cameraFOV * 0.5f));

            // 计算实际的缩放因子
            float scaleFactor = perspectiveScale / distanceToCamera;

            return scaleFactor;
        }

    }

}
