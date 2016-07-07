using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TapTrack.TappyUSB.Ndef
{
    public class CustomRecordPayload : RecordPayload
    {
        private string type;
        private TypeNameField tnf;

        public CustomRecordPayload (byte[] data, string ndefType, TypeNameField tnf) : base()
        {
            this.payload.AddRange(data);
            this.type = ndefType;
            this.tnf = tnf;
        }

        public override string NdefType
        {
            get
            {
                return type;
            }
        }

        public override TypeNameField Tnf
        {
            get
            {
                return tnf;
            }
        }
    }
}
