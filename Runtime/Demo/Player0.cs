using UnityEngine;

namespace XiaoCao
{
    public class Player0 : Player
    {
        public PlayerData0 playerData;
        public override IComponentData data => playerData;

        public void Init(PlayerData0 playerData)
        {
            prefabID = playerData.prefabID;
            this.CreateGameObject();
        }




    }


    public class PlayerData0 : IComponentData
    {
        public int prefabID = 0;
    }

}
