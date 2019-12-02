using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ResourceFramework
{
    internal abstract class ABundleAsync : ABundle
    {
        internal abstract bool Update();
    }
}