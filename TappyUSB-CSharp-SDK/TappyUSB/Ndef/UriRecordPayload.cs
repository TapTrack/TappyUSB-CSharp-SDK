using System;
using System.Collections.Generic;
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
    }
}
