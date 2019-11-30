using UnityEngine;
using ResourceFramework;
using System.IO;
using System.Threading.Tasks;

public class Demo : MonoBehaviour
{
    private const string BUNDLE_FOLDER_NAME = "bundle";
    private string PrefixPath { get; set; }
    private string Platform { get; set; }

    // Use this for initialization
    private void Start()
    {
        Platform = GetPlatform();
        PrefixPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build")).Replace("\\", "/");
        PrefixPath += $"/{Platform}/{BUNDLE_FOLDER_NAME}";
        ResourceManager.instance.Initialize(GetFileUrl, 0);

        Initialize();
    }

    private async void Initialize()
    {
        Task<AResource> uiRootTask = ResourceManager.instance.Load("Assets/AssetBundle/UI/UIRoot.prefab", false, 0);
        await uiRootTask;
        AResource uiRootResource = uiRootTask.Result;
        GameObject uiRoot = Instantiate(uiRootResource.asset) as GameObject;
        uiRoot.name = uiRootResource.asset.name;

        Transform uiParent = GameObject.Find("Canvas").transform;

        Task<AResource> testUITask = ResourceManager.instance.Load("Assets/AssetBundle/UI/TestUI.prefab", false, 0);
        await testUITask;
        AResource testUIResource = testUITask.Result;
        GameObject testUIGO = Instantiate(testUIResource.asset, uiParent, false) as GameObject;
        testUIGO.name = testUIResource.asset.name;
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

    // Update is called once per frame
    void Update()
    {
        ResourceManager.instance.Update();
    }

    private void LateUpdate()
    {
        ResourceManager.instance.LateUpdate();
    }
}
