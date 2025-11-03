using System;
using TEngine;
using UnityEngine;
using XiaoCao;

namespace XiaoCaoKit.Runtime.Demo.UI
{
    public class CoinBarUI : MonoBehaviour
    {
        public ItemCell coinCell;

        private void Start()
        {
            GameEvent.AddEventListener<int>(EGameEvent.OnCoinChange.Int(), OnCoinChange);
            OnCoinChange(0);
        }

        private void OnDestroy()
        {
            GameEvent.RemoveEventListener<int>(EGameEvent.OnCoinChange.Int(), OnCoinChange);
        }

        //type暂无
        private void OnCoinChange(int type)
        {
            coinCell.SetNum(PlayerSaveData.LocalSavaData.coin);
        }
    }
}