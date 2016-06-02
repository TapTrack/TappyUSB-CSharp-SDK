using System.Collections.Generic;
using System.Text;

namespace TapTrack.TappyUSB
{
    public class Record
    {
        private Header messageHeader;
        private List<byte> payload;

        public Record(Header messageHeader, List<byte> data, string language = "")
        {
            payload = new List<byte>();
            if (!language.Equals(""))
            {
                payload.Add((byte)language.Length);
                payload.AddRange(Encoding.UTF8.GetBytes(language));
            }
            payload.AddRange(data);
            this.messageHeader = messageHeader;
        }

        // Returns the contents of the header first then the contents of the payload data
        public IEnumerable<byte> RecordGetByteArray()
        {
            foreach (byte b in messageHeader.GetByteArray())
                yield return b;

            foreach (byte b in payload)
                yield return b;
        }
    }
}
