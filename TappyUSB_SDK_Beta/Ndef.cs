using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TapTrack.TappyUSB
{
    public class Ndef
    {
        private List<Record> msg;

        // Constructor for Single Record Ndef
        public Ndef(string language, string payload, string type, byte id = 0)
        {
            msg = new List<Record>();
            AddRecord(payload, type, true, true, language);
        }

        // Constructor for Multi Record Ndef
        public Ndef(string language, string[] payload, string[] type, byte id = 0)
        {
            if (payload.Length != type.Length)
                throw new ArgumentException("payload.Length must equal type.Length");
            msg = new List<Record>();
            bool isBegin = true;
            bool isEnd = false;
            for (int i = 0; i < payload.Length; i++)
            {
                if (i > 0)
                    isBegin = false;
                if (i == payload.Length - 1)
                    isEnd = true;
                AddRecord(payload[i], type[i], isBegin, isEnd, (type[i].Equals("U")) ? "" : language);
            }
        }

        private void AddRecord(string payload, string type, bool isBegin, bool isEnd, string language = "", byte id = 0)
        {
            if (payload.Length > 65533 - language.Length)
                throw new ArgumentException("The payload length must be less than or equal to 65533-lengthOfLanguageCode");

            if (language.Length > 63)
                throw new ArgumentException("The length of the language code has to be less than or equal 63 characters");

            List<byte> payloadByte = new List<byte>();
            byte uriCode = 0;
            int length = 0;

            if (type.Equals("U"))
            {
                uriCode = Uri.RemoveScheme(ref payload);
                payloadByte.Add(uriCode);
            }
            else if(type.Equals("T"))
            {
                length += language.Length + 1;
            }

            payloadByte.AddRange(Encoding.UTF8.GetBytes(payload));
            length += payloadByte.Count();
            bool isShort = (payload.Length < 255 - language.Length) ? true : false;
            Header header = new Header(isBegin, isEnd, false, isShort, false, 0x01, length, type);
            msg.Add(new Record(header, payloadByte, language));
        }

        // Returns the contents of all the messages
        public IEnumerable<byte> GetByteArray()
        {
            for (int i = 0; i < msg.Count; i++)
            {
                foreach (byte b in msg[i].RecordGetByteArray())
                    yield return b;
            }
        }
    }
}