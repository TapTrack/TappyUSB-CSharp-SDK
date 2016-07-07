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
        private string language;

        public TextRecordPayload(string language, string text) : base()
        {
            this.language = language;
            this.payload.Add((byte)Language.Length);
            this.payload.AddRange(Encoding.UTF8.GetBytes(Language));
            this.payload.AddRange(Encoding.UTF8.GetBytes(text));

            this.language = language;
        }

        public string Language
        {
            get
            {
                return language;
            }
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
