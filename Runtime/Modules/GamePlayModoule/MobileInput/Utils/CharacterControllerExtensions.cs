using UnityEngine;

namespace MFPC.Utils
{
    public static class CharacterControllerExtensions
    {
        public static void Transfer(this CharacterController @this, Vector3 target)
        {
            @this.gameObject.transform.position = target;
            Physics.SyncTransforms();
        }
        
        public static Vector3 GetUnderPosition(this CharacterController @this)
        {
            var bounds = @this.bounds;
            return new Vector3(bounds.center.x,
                bounds.min.y,
                bounds.center.z);
        }
    }
}