

using System.ComponentModel;
using System.Runtime.Serialization;
using DotNetOpenAuth.OpenId;

namespace System{	
	public class  Uri: ISerializable{
        public string _AbsoluteUri;
		
		public Uri(string val){
			this._AbsoluteUri = val;
		}
		
        public string AbsoluteUri {
            get { return this._AbsoluteUri; }
        }
        
		public static bool operator ==(Uri uri1,Uri uri2) {
            if (uri1._AbsoluteUri!=uri2._AbsoluteUri) 
                return false;
            return true;
        }
        public static bool operator !=(Uri uri1, Uri uri2)
        {
            if (uri1._AbsoluteUri != uri2._AbsoluteUri)
                return true;
            return false;
        }
		public void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext){}
	}
}