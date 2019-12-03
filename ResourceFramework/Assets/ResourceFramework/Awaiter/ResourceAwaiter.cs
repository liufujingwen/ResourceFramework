using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ResourceFramework
{
    public class ResourceAwaiter
    {
        public string url { get; private set; }

        public ResourceAwaiter(string url)
        {
            this.url = url;
            taskCompletionSource = new TaskCompletionSource<IResource>();
        }

        public TaskCompletionSource<IResource> taskCompletionSource { get; private set; }

        internal void SetResult(IResource resource)
        {
            taskCompletionSource.SetResult(resource);
        }
    }
}
