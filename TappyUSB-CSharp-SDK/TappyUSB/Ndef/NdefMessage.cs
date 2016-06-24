using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TapTrack.TappyUSB.Ndef
{
    /// <summary>
    /// Helper class to construct NDEF messages to sent over the TappyUSB.
    /// </summary>
    public class NdefMessage
    {
        private List<Record> msg;

        /// <summary>
        /// The header for the record payload will be generated for you
        /// </summary>
        /// <param name="payload">Payload to be sent</param>
        public NdefMessage(RecordPayload payload)
        {
            msg = new List<Record>();
            AddRecordPayloads(payload);
        }

        /// <summary>
        /// The header for each record payload will be generated for you
        /// </summary>
        /// <param name="payload">Payload to be sent</param>
        public NdefMessage(RecordPayload[] payload)
        {
            msg = new List<Record>();
            AddRecordPayloads(payload);
        }

        public NdefMessage(Record record)
        {
            msg = new List<Record>();
            msg.Add(record);
        }

        public NdefMessage(Record[] records)
        {
            msg = new List<Record>();
            msg.AddRange(records);
        }

        private void AddRecordPayloads(params RecordPayload[] payload)
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

                header = new Header(begin, end, false, payload[i].IsShort(), false, payload[i].Tnf, payload[i].Length, payload[i].NdefType);

                msg.Add(new Record(header, payload[i]));
            }
        }

        /// <summary>
        /// Get the bytes representing the header
        /// </summary>
        /// <returns></returns>
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