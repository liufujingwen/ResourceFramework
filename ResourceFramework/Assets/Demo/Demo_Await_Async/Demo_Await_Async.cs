using UnityEngine;
using ResourceFramework;
using System.IO;
using System.Threading.Tasks;

public class Demo_Await_Async : MonoBehaviour
{
    private const string BUNDLE_FOLDER_NAME = "bundle";
    private string PrefixPath { get; set; }
    private string Platform { get; set; }

    private void Start()
    {
        Platform = GetPlatform();
        PrefixPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build")).Replace("\\", "/");
        PrefixPath += $"/{Platform}/{BUNDLE_FOLDER_NAME}";
        ResourceManager.instance.Initialize(GetFileUrl, false, 0);

        Initialize();
    }

    private async void Initialize()
    {
        Task<IResource> uiResource = ResourceManager.instance.LoadTask("Assets/AssetBundle/UI/UIRoot.prefab");
        await uiResource;
        uiResource.Result.Instantiate();

        Transform uiParent = GameObject.Find("Canvas").transform;

        Task<IResource> testResource = ResourceManager.instance.LoadTask("Assets/AssetBundle/UI/TestUI.prefab");
        await testResource;
        testResource.Result.Instantiate(uiParent, false);
    }

    private string GetPlatform()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.WindowsPlayer:
                return "windows";
            case RuntimePlatform.Android:
                return "android";
            case RuntimePlatform.IPhonePlayer:
                return "ios";
            default:
                throw new System.Exception($"未支持的平台:{Application.platform}");
        }
    }

    private string GetFileUrl(string assetUrl)
    {
        return $"{PrefixPath}/{assetUrl}";
    }

    private void Update()
    {
        ResourceManager.instance.Update();
    }

    private void LateUpdate()
    {
        ResourceManager.instance.LateUpdate();
    }
}
