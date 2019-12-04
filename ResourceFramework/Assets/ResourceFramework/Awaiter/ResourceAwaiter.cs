using System;

namespace ResourceFramework
{
    public class ResourceAwaiter : IAwaiter<IResource>, IAwaitable<ResourceAwaiter, IResource>
    {
        public bool IsCompleted { get; private set; }
        public IResource result { get; private set; }
        private Action m_Continuation;

        public IResource GetResult()
        {
            return result;
        }

        public ResourceAwaiter GetAwaiter()
        {
            return this;
        }

        public void OnCompleted(Action continuation)
        {
            if (IsCompleted)
            {
                continuation.Invoke();
            }
            else
            {
                m_Continuation += continuation;
            }
        }

        internal void SetResult(IResource result)
        {
            IsCompleted = true;
            this.result = result;
            Action tempCallback = m_Continuation;
            m_Continuation = null;
            tempCallback.Invoke();
        }
    }
}