using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TapTrack.Classic.Ndef
{
    public class RecordData : RecordPayload
    {
        private string type;
        private TypeNameField tnf;

        public RecordData(string type, TypeNameField tnf, string content, byte[] id) : base()
        {
            this.type = type;
            this.tnf = tnf;
            Content = content;
            Id = id;
        }

        public string Content
        {
            get;
            set;
        }

        public byte[] Id
        {
            get;
            set;
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
