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
            GameEvent.AddEventListener<int>(EGameEvent.OnCoinChange.ToInt(), OnCoinChange);
            OnCoinChange(0);
        }

        private void OnDestroy()
        {
            GameEvent.RemoveEventListener<int>(EGameEvent.OnCoinChange.ToInt(), OnCoinChange);
        }

        //type暂无
        private void OnCoinChange(int type)
        {
            coinCell.SetNum(PlayerSaveData.LocalSavaData.Coin);
        }
    }
}