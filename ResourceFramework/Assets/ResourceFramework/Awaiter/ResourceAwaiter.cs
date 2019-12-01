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
            taskCompletionSource = new TaskCompletionSource<AResource>();
        }

        public TaskCompletionSource<AResource> taskCompletionSource { get; private set; }

        public void SetResult(AResource resource)
        {
            taskCompletionSource.SetResult(resource);
        }
    }
}
