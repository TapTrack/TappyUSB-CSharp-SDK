using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TapTrack.TappyUSB.Ndef
{
    public class UriRecordPayload : RecordPayload
    {
        public UriRecordPayload(string uri) : base()
        {
            Uri data = new Uri(uri);

            this.payload.Add(data.Scheme);
            this.payload.AddRange(Encoding.UTF8.GetBytes(data.Path));
        }

        public static string Parse(byte[] data)
        {
            try
            {
                string scheme = Uri.STRING_LOOKUP[data[0]];
                return scheme + Encoding.UTF8.GetString(data, 1, data.Length - 1);
            }
            catch
            {
                return Encoding.UTF8.GetString(data, 1, data.Length - 1);
            }
        }

        public override string NdefType
        {
            get
            {
                return "U";
            }
        }

        public override TypeNameField Tnf
        {
            get
            {
                return TypeNameField.NfcForumWellKnown;
            }
        }
    }
}
