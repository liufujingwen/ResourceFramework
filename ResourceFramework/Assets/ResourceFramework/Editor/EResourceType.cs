

namespace ResourceFramework
{
    /// <summary>
    /// 引用类型
    /// </summary>
    public enum EResourceType
    {
        /// <summary>
        /// 在打包设置中分析到的资源
        /// </summary>
        Direct = 1,

        /// <summary>
        /// 依赖资源
        /// </summary>
        Dependency = 2,

        /// <summary>
        /// 生成的文件
        /// </summary>
        Ganerate = 3,
    }
}