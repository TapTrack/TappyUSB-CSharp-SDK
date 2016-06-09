using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TapTrack.TappyUSB.Ndef
{
    /// <summary>
    /// Helper class to construct NDEF messages to sent over the TappyUSB.
    /// <note type="note">
    ///     The only types that are supported are:
    ///     <list type = "bullet" >
    ///         <item>
    ///             Text
    ///         </item>
    ///         <item>
    ///             URI
    ///         </item>
    ///     </list>
    /// </note>
    /// 
    /// </summary>
    public class NdefMessage
    {
        private List<Record> msg;

        public NdefMessage(RecordPayload payload)
        {
            msg = new List<Record>();
            AddRecords(payload);
        }

        public NdefMessage(RecordPayload[] payload)
        {
            msg = new List<Record>();
            AddRecords(payload);
        }

        private void AddRecords(params RecordPayload[] payload)
        {
            Header header;
            bool begin;
            bool end;

            for (int i = 0; i < payload.Length; i++)
            {
                if (i == 0)
                    begin = true;
                else
                    begin = false;
                if (i == payload.Length - 1)
                    end = true;
                else
                    end = false;

                if (payload[i] is TextRecordPayload)
                    header = new Header(begin, end, false, payload[i].IsShort(), false, TypeNameField.NfcForumWellKnown, payload[i].Length, "T");
                else
                    header = new Header(begin, end, false, payload[i].IsShort(), false, TypeNameField.NfcForumWellKnown, payload[i].Length, "U");

                msg.Add(new Record(header, payload[i]));
            }
        }

        // Returns the contents of all the messages
        public IEnumerable<byte> GetByteArray()
        {
            for (int i = 0; i < msg.Count; i++)
            {
                foreach (byte b in msg[i].GetByteArray())
                    yield return b;
            }
        }
    }
}