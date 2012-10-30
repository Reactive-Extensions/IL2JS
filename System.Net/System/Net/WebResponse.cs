using System.IO;

namespace System.Net
{
    public abstract class WebResponse : IDisposable
    {
        //protected WebResponse()
        //{
        //}

        //public virtual void Close()
        //{
        //    throw new NotImplementedException();
        //}

        //public virtual Stream GetResponseStream()
        //{
        //    throw new NotImplementedException();
        //}

        //internal virtual void OnDispose()
        //{
        //}

        //void IDisposable.Dispose()
        //{
        //    try
        //    {
        //        this.Close();
        //        this.OnDispose();
        //    }
        //    catch
        //    {
        //    }
        //}

        //// Properties
        //public virtual long ContentLength
        //{
        //    get
        //    {
        //        throw new NotImplementedException();
        //    }
        //    set
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        //public virtual string ContentType
        //{
        //    get
        //    {
        //        throw new NotImplementedException();
        //    }
        //    set
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        //public virtual bool IsFromCache
        //{
        //    get
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        //public virtual bool IsMutuallyAuthenticated
        //{
        //    get
        //    {
        //        return false;
        //    }
        //}

        //public virtual Uri ResponseUri
        //{
        //    get
        //    {
        //        throw new NotImplementedException();
        //    }
        //}
        #region IDisposable Members

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
