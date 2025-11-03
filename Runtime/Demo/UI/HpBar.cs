using DG.Tweening;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    public class HpBar : MonoBehaviour
    {
        public Image fillImage;

        public Image fillImage2;

        public Image barImgSlow;

        //public ColorSettingSo settingSo;

        public Color[] barColors = new Color[3];

        //Color Setting
        public static Color White = Color.white;


        private Transform _targetTF;
        private Vector3 _offset;


        public void InitColor(Role role)
        {
            //&& !role.HasTag(RoleTagCommon.MainPlayer);
            bool isBlue = role.team == XCSetting.PlayerTeam; 
            SetBarColors(isBlue);
        }

        public void SetBarColors(bool isBlue)
        {
            fillImage.color = White;
            ColorSettingSo settingSo = UIPrefabSo.Inst.hpBarColorSettingSo;
            var colorArray = settingSo.values;
            int startIndex = 0;

            //友方单位
            if (isBlue)
            {
                startIndex = 3;
            }
            barColors = new[] { colorArray[startIndex], colorArray[startIndex + 1], colorArray[startIndex + 2] };
        }

        public void SetFollow(Transform tf, Vector3 offset)
        {
            _targetTF = tf;
            _offset = offset;
        }

        public void SetFollowRole(Role role)
        {
            float h = role.idRole.cc.height;

            _targetTF = role.idRole.transform;

            //_offset = Vector3.zero;
            //transform.SetParent(role.gameObject.transform, false);
            _offset = Vector3.up * h + role.idRole.hpBarOffset;
        }

        public void UpdatePostion()
        {
            transform.position = _targetTF.TransformPoint(_offset);
            //Watch Cam
            Vector3 dir = -CameraMgr.Main.transform.position + transform.position;
            transform.rotation = Quaternion.LookRotation(dir);
        }

        public void UpdateHealthBar(float healthPercentage)
        {
            if (healthPercentage <= 0)
            {
                gameObject.SetActive(false);
                return;
            }

            fillImage.fillAmount = healthPercentage;

            if (healthPercentage > 2f / 3f)
            {
                fillImage.color = barColors[0];
            }
            else if (healthPercentage > 1 / 3f)
            {
                fillImage.color = barColors[1];
            }
            else
            {
                fillImage.color = barColors[2];
            }
        }

        public void UpdateArmorBar(float percentage)
        {
            fillImage2.fillAmount = percentage;
        }



    }

}
