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
        public TextRecordPayload(string language, string text) : base()
        {
            this.payload.Add((byte)language.Length);
            this.payload.AddRange(Encoding.UTF8.GetBytes(language));
            this.payload.AddRange(Encoding.UTF8.GetBytes(text));
        }

        public static string Parse(byte[] data)
        {
            bool isUTF16 = (data[0] & 0x80) == 0x80;
            int langLength = data[0] & 0x3F;

            if (data.Length < langLength + 1)
                throw new InvalidOperationException("Ndef message is invalid");

            if (isUTF16)
                return Encoding.Unicode.GetString(data, langLength + 1, data.Length - langLength - 1);
            else
                return Encoding.UTF8.GetString(data, langLength + 1, data.Length - langLength - 1);
        }

        public override string NdefType
        {
            get
            {
                return "T";
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
