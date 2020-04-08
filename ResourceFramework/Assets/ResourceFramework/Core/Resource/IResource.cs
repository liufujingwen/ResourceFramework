using UnityEngine;

namespace ResourceFramework
{
    public interface IResource
    {
        string url { get; }
        Object GetAsset();
        T GetAsset<T>() where T : Object;
        GameObject Instantiate();
        GameObject Instantiate(bool autoUnload);
        GameObject Instantiate(Vector3 position, Quaternion rotation);
        GameObject Instantiate(Vector3 position, Quaternion rotation, bool autoUnload);
        GameObject Instantiate(Transform parent, bool instantiateInWorldSpace);
        GameObject Instantiate(Transform parent, bool instantiateInWorldSpace, bool autoUnload);
    }
}