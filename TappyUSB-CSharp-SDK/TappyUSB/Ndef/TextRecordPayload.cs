using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TapTrack.TappyUSB.Ndef
{
    public class TextRecordPayload : RecordPayload
    {
        public TextRecordPayload (string language, string text) : base()
        {
            this.payload.Add((byte)language.Length);
            this.payload.AddRange(Encoding.UTF8.GetBytes(language));
            this.payload.AddRange(Encoding.UTF8.GetBytes(text));
        }
    }
}
