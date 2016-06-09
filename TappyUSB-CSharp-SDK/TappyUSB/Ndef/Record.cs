using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TapTrack.TappyUSB.Ndef
{
    public class Record
    {
        Header recordHeader;
        RecordPayload payload;

        public Record(Header recordHeader, RecordPayload payload)
        {
            this.recordHeader = recordHeader;
            this.payload = payload;
        }

        public IEnumerable<byte> GetByteArray()
        {
            foreach (byte b in recordHeader.GetByteArray())
                yield return b;

            foreach (byte b in payload.GetByteArray())
                yield return b;
        }
    }
}
