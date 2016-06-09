using System.Collections.Generic;
using System.Text;

namespace TapTrack.TappyUSB.Ndef
{
    public class RecordPayload
    {
        protected List<byte> payload;

        protected RecordPayload()
        {
            this.payload = new List<byte>();
        }

        public RecordPayload(List<byte> data)
        {
            this.payload = data;
        }

        //public static Record ConstructTextRecord(string language, string text)
        //{

        //    Header header = new Header(false, false, false, IsShort(payload), false, TypeNameField.NfcForumWellKnown, payload.Count, "T");

        //    return new Record(header, payload);
        //}

        //public static Record ConstructUriRecord(string uri)
        //{
        //    List<byte> payload = new List<byte>();
        //    string localUri = string.Copy(uri);
        //    byte uriCode = Uri.RemoveScheme(ref localUri);

        //    payload.Add(uriCode);
        //    payload.AddRange(Encoding.UTF8.GetBytes(localUri));

        //    Header header = new Header(false, false, false, IsShort(payload), false, TypeNameField.NfcForumWellKnown, payload.Count, "U");

        //    return new Record(header, payload);
        //}

        public bool IsShort()
        {
            if (payload.Count < 255)
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
