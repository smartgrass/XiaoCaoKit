using UnityEngine;

public interface IEntityData { }

public interface IData : IEntityData
{
}
public interface IShareData : IEntityData
{

}

//[System.Serializable]
//public struct RenderMesh : ISharedComponentData
//{
//    public Mesh mesh;
//    public Material material;
//    public ShadowCastingMode castShadows;
//    public bool receiveShadows;
//}
