using ResourceFramework;
using UnityEngine;
using UnityEngine.UI;

public class TestUI : MonoBehaviour
{
    private string[] m_Backgrounds = new string[]
    {
        "Assets/AssetBundle/Background/Background_01.jpg",
        "Assets/AssetBundle/Background/Background_02.jpg",
    };


    private string[] m_Bears = new string[]
    {
        "Assets/AssetBundle/Atlas/Bear/Bear_1.png",
        "Assets/AssetBundle/Atlas/Bear/Bear_2.png",
        "Assets/AssetBundle/Atlas/Bear/Bear_3.png",
        "Assets/AssetBundle/Atlas/Bear/Bear_4.png",
        "Assets/AssetBundle/Atlas/Bear/Bear_5.png",
        "Assets/AssetBundle/Atlas/Bear/Bear_6.png",
        "Assets/AssetBundle/Atlas/Bear/Bear_7.png",
        "Assets/AssetBundle/Atlas/Bear/Bear_8.png",
    };

    private string[] m_Icons = new string[]
    {
        "Assets/AssetBundle/Icon/INV_Weapon_Shortblade_40.png",
        "Assets/AssetBundle/Icon/INV_Weapon_Shortblade_41.png",
        "Assets/AssetBundle/Icon/INV_Weapon_Shortblade_42.png",
        "Assets/AssetBundle/Icon/INV_Weapon_Shortblade_43.png",
        "Assets/AssetBundle/Icon/INV_Weapon_Shortblade_44.png",
        "Assets/AssetBundle/Icon/INV_Weapon_Shortblade_45.png",
        "Assets/AssetBundle/Icon/INV_Weapon_Shortblade_46.png",
        "Assets/AssetBundle/Icon/INV_Weapon_Shortblade_47.png",
        "Assets/AssetBundle/Icon/INV_Weapon_Shortblade_48.png",
        "Assets/AssetBundle/Icon/INV_Weapon_Shortblade_49.png",
    };

    private string m_ModelUrl = "Assets/AssetBundle/Model/GeBuLin/GeBuLin.prefab";
    [SerializeField]
    private Transform m_ModelRoot;
    private GameObject m_ModelGO;
    private AResource m_ModelResource;

    [SerializeField]
    private RawImage m_RawImage_Background = null;
    [SerializeField]
    private Image m_Image_Bear = null;
    [SerializeField]
    private RawImage m_RawImage_Icon = null;

    private int m_BackgourndIndex = -1;
    private int m_BearIndex = -1;
    private int m_IconIndex = -1;


    // Use this for initialization
    void Start()
    {
        m_BackgourndIndex = -1;
        m_BearIndex = -1;
        m_IconIndex = -1;
    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// 切换熊的sprite
    /// </summary>
    public void OnChangeBackground()
    {
        if (m_Backgrounds.Length == 0)
            return;

        m_BackgourndIndex = ++m_BackgourndIndex % m_Backgrounds.Length;

        string backgroundUrl = m_Backgrounds[m_BackgourndIndex];

        //同步加载熊的sprite
        AResource resource = ResourceManager.instance.Load(backgroundUrl, false, 0);
        m_RawImage_Background.texture = resource.asset as Texture;
    }

    /// <summary>
    /// 切换熊的sprite
    /// </summary>
    public void OnChangeBear()
    {
        if (m_Bears.Length == 0)
            return;

        m_BearIndex = ++m_BearIndex % m_Bears.Length;

        string bearUrl = m_Bears[m_BearIndex];

        //同步加载熊的sprite
        AResource resource = ResourceManager.instance.Load(bearUrl, false, 0);
        m_Image_Bear.sprite = resource.asset as Sprite;
    }

    /// <summary>
    /// 切换道具图标
    /// </summary>
    public void OnChangeIcon()
    {
        if (m_Icons.Length == 0)
            return;

        m_IconIndex = ++m_IconIndex % m_Bears.Length;
        string iconUrl = m_Icons[m_IconIndex];

        //同步加载icon
        AResource resource = ResourceManager.instance.Load(iconUrl, false, 0);
        m_RawImage_Icon.texture = resource.asset as Texture;
    }

    /// <summary>
    /// 加载模型
    /// </summary>
    public void OnLoadModel()
    {
        if (m_ModelResource != null)
            return;

        //同步加载熊的sprite
        m_ModelResource = ResourceManager.instance.Load(m_ModelUrl, false, 0);
        m_ModelGO = Instantiate(m_ModelResource.asset, m_ModelRoot, false) as GameObject;
        m_ModelGO.transform.eulerAngles = new Vector3(0, 180, 0);
    }

    /// <summary>
    /// 卸载模型
    /// </summary>
    public void OnUnloadModel()
    {
        if (m_ModelResource == null)
            return;

        ResourceManager.instance.Unload(m_ModelResource);
        m_ModelResource = null;
        if (m_ModelGO)
        {
            Destroy(m_ModelGO);
            m_ModelGO = null;
        }

        Resources.UnloadUnusedAssets();
    }
}
