using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Microsoft.LiveLabs.JavaScript.IL2JS;

namespace Microsoft.Csa.SharedObjects
{
    /// <summary>
    /// A basic principal object that can be used by on its own to handle client identity and security identification or
    /// extended to support additional properties containing app specific information. This object can also be replaced
    /// entirely by a app specific object that supplies the basic principal information
    /// </summary>
    [Reflection(ReflectionLevel.Full)]
    public class PrincipalObject : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets the string used to identify the user connecting to the SharedObjects Server
        /// </summary>
        [PrincipalIdentity]
        public string Id
        {
            get;
            set; 
        }

        /// <summary>
        /// Gets or sets the string used to identify the security identifier for this user 
        /// </summary>
        [PrincipalSid]
        public string Sid
        {
            get;
            set;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            var other = obj as PrincipalObject;
            if (other == null)
            {
                return false;
            }
            return other.Id.Equals(this.Id);
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
