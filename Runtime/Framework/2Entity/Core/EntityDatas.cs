using UnityEngine;

public interface IEntityData { }

public interface IComponentData : IEntityData
{
}
public interface ISharedComponentData : IEntityData
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
