# ResourceFramework
Unity AssetBundle

1、通过BuildSetting.xml控制打包规则、粒度等等

2、支持Bundle、Editor加载模式

3、同步异步无缝切换

4、支持async-await、Unity Coroutine、回调等方式

//使用async-await加载  
private async void Initialize()  
{
	Task<AResource> task = ResourceManager.instance.LoadTask("Assets/AssetBundle/UI/UIRoot.prefab", 0);
	await task;
	GameObject uiRoot = Instantiate(task.Result.asset) as GameObject;
	uiRoot.name = task.Result.asset.name;  
}

//使用协程加载  
private IEnumerator Initialize()  
{
	AResource resource = ResourceManager.instance.Load("Assets/AssetBundle/UI/UIRoot.prefab", true, 0);
	yield return resource;
	GameObject uiRoot = Instantiate(resource.asset) as GameObject;
	uiRoot.name = resource.asset.name;  
}

//使用回调加载  
private void Initialize()  
{
	ResourceManager.instance.LoadWithCallback("Assets/AssetBundle/UI/UIRoot.prefab", true, 0, resource =>
	{
		GameObject uiRoot = Instantiate(resource.asset) as GameObject;
		uiRoot.name = resource.asset.name;  
	});
}

//使用同步加载  
private void Initialize()  
{
	AResource resource = ResourceManager.instance.Load("Assets/AssetBundle/UI/UIRoot.prefab", false, 0);
	GameObject uiRoot = Instantiate(resource.asset) as GameObject;
	uiRoot.name = resource.asset.name;  
}

//同步加载并释放资源  
private void Initialize()  
{
	AResource resource = ResourceManager.instance.Load("Assets/AssetBundle/UI/UIRoot.prefab", false, 0);
	ResourceManager.instance.Unload(resource);  
}

//先异步加载资源，然后同步加载资源，最后释放  
private void Initialize()  
{
	AResource resource1 = ResourceManager.instance.Load("Assets/AssetBundle/UI/UIRoot.prefab", true, 0);
	AResource resource2 = ResourceManager.instance.Load("Assets/AssetBundle/UI/UIRoot.prefab", false, 0);
	ResourceManager.instance.Unload(resource1);
	ResourceManager.instance.Unload(resource2);  
}

//先同步加载资源，然后异步加载资源，最后释放  
private void Initialize()  
{
	AResource resource1 = ResourceManager.instance.Load("Assets/AssetBundle/UI/UIRoot.prefab", false, 0);
	AResource resource2 = ResourceManager.instance.Load("Assets/AssetBundle/UI/UIRoot.prefab", true, 0);
	ResourceManager.instance.Unload(resource1);
	ResourceManager.instance.Unload(resource2);  
}
