using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TapTrack.TappyUSB
{
    public class NdefParser
    {
        Queue<byte> tokens;
        int typeLen;
        byte tagType;
        int idLen;
        int payLoadLen;
        int tagLen;
        Tag tag;
        string id;
        List<byte[]> payload;
        List<string> payloadEncoded;
        List<string> type;
        FlagHeader flags;

        public NdefParser(byte[] data)
        {
            tokens = new Queue<byte>(data);
            payload = new List<byte[]>();
            payloadEncoded = new List<string>();
            type = new List<string>();
            ProcessTag();
        }

        //public byte GetLangLen()
        //{
        //    return 0;
        //}

        //public bool isUTF8()
        //{
        //    return false;
        //}

        private void ProcessTag()
        {
            tagType = Next();
            tagLen = Next();

            byte[] uid = new byte[tagLen];
            for (int i = 0; tokens.Count > 0 && i < tagLen; i++)
                uid[i] = Next();

            tag = new Tag(tagType, uid);

            NdefMessage();
        }

        private void NdefMessage()
        {
            bool run = true;
            while (tokens.Count > 0 && run)
            {
                StringBuilder temp = new StringBuilder();

                flags = new FlagHeader(Next());

                if (flags.GetMe())
                    run = false;

                typeLen = Next();
                payLoadLen = Next();

                if (flags.GetIl())
                    idLen = Next();

                for (int i = 0; i < typeLen; i++)
                    temp.Append(Convert.ToChar(Next()));

                type.Add(temp.ToString());

                if (flags.GetIl())
                    GetId();

                Payload();
            }
        }

        private void GetId()
        {
            byte[] idBytes = new byte[idLen];

            for (int i = 0; i < idLen; i++)
                idBytes[i] = Next();

            id = new string(Encoding.UTF8.GetChars(idBytes));
        }

        private void Payload()
        {
            byte[] content;
            string scheme = "";
            int length;

            if (type.Last().Equals("U"))
            {
                length = payLoadLen - 1;
                scheme = Uri.STRING_LOOKUP[Next()];
            }
            else
            {
                int langLen = Next();
                length = payLoadLen - langLen - 1;
                for (int i = 0; i < langLen; i++)
                    Next();
            }

            content = new byte[length];

            payload.Add(content);

            for (int i = 0; i < length; i++)
                payload.Last()[i] = Next();

            payloadEncoded.Add(scheme + new string(Encoding.UTF8.GetChars(payload.Last())));
        }

        public List<string> GetPayLoad()
        {
            return payloadEncoded;
        }

        private byte Next()
        {
            return tokens.Dequeue();
        }


        public Tag GetTag()
        {
            return tag;
        }
    }
}
