namespace ResourceFramework
{
    /// <summary>
    /// 控制ab粒度
    /// </summary>
    public enum EBundleType
    {
        /// <summary>
        /// 以文件作为ab名字（最小粒度）
        /// </summary>
        File,

        /// <summary>
        /// 以目录作为ab的名字
        /// </summary>
        Directory,

        /// <summary>
        /// 以最上的
        /// </summary>
        All
    }
}
