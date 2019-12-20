using UnityEngine;

namespace ResourceFramework
{
    public interface IResource
    {
        string url { get; }
        Object GetAsset();
        T GetAsset<T>() where T : Object;
        //
        // 摘要:
        //     Clones the object original and returns the clone.
        //
        // 参数:
        //   position:
        //     Position for the new object.
        //
        //   rotation:
        //     Orientation of the new object.
        //
        //   parent:
        //     Parent that will be assigned to the new object.
        //
        //   instantiateInWorldSpace:
        //     Pass true when assigning a parent Object to maintain the world position of the
        //     Object, instead of setting its position relative to the new parent. Pass false
        //     to set the Object's position relative to its new parent.
        //
        // 返回结果:
        //     The instantiated clone.
        GameObject Instantiate();
        //
        // 摘要:
        //     Clones the object original and returns the clone.
        //
        // 参数:
        //   position:
        //     Position for the new object.
        //
        //   rotation:
        //     Orientation of the new object.
        //
        //   parent:
        //     Parent that will be assigned to the new object.
        //
        //   instantiateInWorldSpace:
        //     Pass true when assigning a parent Object to maintain the world position of the
        //     Object, instead of setting its position relative to the new parent. Pass false
        //     to set the Object's position relative to its new parent.
        //
        // 返回结果:
        //     The instantiated clone.
        GameObject Instantiate(Vector3 position, Quaternion rotation);
        //
        // 摘要:
        //     Clones the object original and returns the clone.
        //
        // 参数:
        //   position:
        //     Position for the new object.
        //
        //   rotation:
        //     Orientation of the new object.
        //
        //   parent:
        //     Parent that will be assigned to the new object.
        //
        //   instantiateInWorldSpace:
        //     Pass true when assigning a parent Object to maintain the world position of the
        //     Object, instead of setting its position relative to the new parent. Pass false
        //     to set the Object's position relative to its new parent.
        //
        // 返回结果:
        //     The instantiated clone.
        GameObject Instantiate(Transform parent, bool instantiateInWorldSpace);
    }
}