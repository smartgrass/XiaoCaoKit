using DG.Tweening;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCaoKit;

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

        public TextMeshProUGUI lvText;
        
        private Transform _targetTf;
        private Vector3 _offset;


        private void Awake()
        {
            //血条用"宽度"表达进度(和 ExpBar 一致): Filled 类型的 fillAmount 会再乘一次, 统一置满
            PrepareFillImage(fillImage);
            PrepareFillImage(fillImage2);
        }

        /// <summary>
        /// 宽度填充的前置处理:
        /// 1. Filled 类型会按 fillAmount 再裁一次, 统一置满;
        /// 2. 有九宫格边框的图切成 Sliced, 宽度收缩时两侧圆角不会变形(Filled/Simple 是整张拉伸);
        /// 3. 把 Fill 归成左锚点 + 水平零偏移。
        /// </summary>
        private static void PrepareFillImage(Image img)
        {
            img.fillAmount = 1f;

            if (img.type != Image.Type.Sliced && img.sprite != null && img.sprite.border != Vector4.zero)
            {
                img.type = Image.Type.Sliced;
            }

            UITool.NormalizeFillRect(img);
        }

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
            _targetTf = tf;
            _offset = offset;
        }

        public void SetFollowRole(Role role)
        {
            float h = role.idRole.cc.height;

            _targetTf = role.idRole.transform;

            //_offset = Vector3.zero;
            //transform.SetParent(role.gameObject.transform, false);
            _offset = Vector3.up * h + role.idRole.hpBarOffset;
        }

        /// <summary>
        /// 刷新血条上的等级文本。
        /// </summary>
        public void UpdateLvText(int lv)
        {
            if (lvText == null)
            {
                return;
            }

            lvText.text = $"Lv {lv}";
        }
        

        public void UpdatePosition()
        {
            transform.position = _targetTf.TransformPoint(_offset);
            //Watch Cam
            Vector3 dir = -CameraMgr.Main.transform.position + transform.position;
            transform.rotation = Quaternion.LookRotation(dir);
        }

        public void UpdateHealthBar(float healthPercentage)
        {
            float ratio = Mathf.Clamp01(healthPercentage);

            //按宽度填充, 图本身保持画满
            UITool.SetFillWidth(fillImage, ratio);

            if (ratio > 2f / 3f)
            {
                fillImage.color = barColors[0];
            }
            else if (ratio > 1f / 3f)
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
            //护甲条同样按宽度填充, 与主血条保持一致
            UITool.SetFillWidth(fillImage2, percentage);
        }



    }

}
