using System.Collections.Generic;
using System.Text;

namespace TapTrack.TappyUSB.Ndef
{
    public abstract class RecordPayload
    {
        protected List<byte> payload;

        protected RecordPayload()
        {
            this.payload = new List<byte>();
        }

        public abstract string NdefType
        {
            get;
        }

        public abstract TypeNameField Tnf
        {
            get;
        }

        public bool IsShort()
        {
            if (payload.Count < 256)
                return true;
            else
                return false;
        }

        public int Length
        {
            get
            {
                return payload.Count;
            }
        }

        /// <summary>
        /// Returns the contents of the header first then the contents of the payload data
        /// </summary>
        /// <returns></returns>
        public byte[] GetByteArray()
        {
            return payload.ToArray();
        }
    }
}
