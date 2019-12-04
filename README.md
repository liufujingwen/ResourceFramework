# ResourceFramework

## 1、通过BuildSetting.xml控制打包规则、粒度等等

## 2、支持Bundle、Editor加载模式

## 3、同步异步无缝切换

## 4、支持async-await、Unity Coroutine、回调等方式

//使用async-await加载  
private async void Initialize1()  
{  
	ResourceAwaiter awaiter = ResourceManager.instance.LoadWithAwaiter("Assets/AssetBundle/UI/UIRoot.prefab");    
        await awaiter;  
        GameObject uiRoot = awaiter.GetResult().Instantiate();    
        uiRoot.name = awaiter.GetResult().GetAsset().name;  
}  

//使用协程加载  
private IEnumerator Initialize2()  
{  
	IResource resource = ResourceManager.instance.Load("Assets/AssetBundle/UI/UIRoot.prefab", true);  
	yield return resource;  
	resource.Instantiate();  
}  

//使用回调加载    
private void Initialize3()  
{  
	ResourceManager.instance.LoadWithCallback("Assets/AssetBundle/UI/UIRoot.prefab", true, resource =>  
	{  
		GameObject uiRoot = resource.Instantiate();  
		uiRoot.name = resource.GetAsset().name;  
	});  
}  

//使用同步加载  
private void Initialize4()  
{  
	IResource resource = ResourceManager.instance.Load("Assets/AssetBundle/UI/UIRoot.prefab", false);  
	GameObject uiRoot = resource.Instantiate();  
	uiRoot.name = resource.GetAsset().name;  
}  

//同步加载并释放资源  
private void Initialize5()  
{  
	IResource resource = ResourceManager.instance.Load("Assets/AssetBundle/UI/UIRoot.prefab", false);  
	ResourceManager.instance.Unload(resource);  
}

//先异步加载资源，然后同步加载资源，最后释放  
private void Initialize6()  
{  
	IResource resource1 = ResourceManager.instance.Load("Assets/AssetBundle/UI/UIRoot.prefab", true);  
	IResource resource2 = ResourceManager.instance.Load("Assets/AssetBundle/UI/UIRoot.prefab", false);  
	ResourceManager.instance.Unload(resource1);  
	ResourceManager.instance.Unload(resource2);  
}  

//先同步加载资源，然后异步加载资源，最后释放  
private void Initialize7()  
{  
	IResource resource1 = ResourceManager.instance.Load("Assets/AssetBundle/UI/UIRoot.prefab", false);  
	IResource resource2 = ResourceManager.instance.Load("Assets/AssetBundle/UI/UIRoot.prefab", true);  
	ResourceManager.instance.Unload(resource1);  
	ResourceManager.instance.Unload(resource2);  
}  
