using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TapTrack.TappyUSB.Ndef
{
    public class NdefParser
    {
        Queue<byte> tokens;
        int typeLen;
        int idLen;
        uint payLoadLen;
        string id;
        List<string> payloadEncoded;
        List<string> type;
        FlagHeader flags;

        public NdefParser(byte[] data)
        {
            tokens = new Queue<byte>(data);
            payloadEncoded = new List<string>();
            type = new List<string>();
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

                if (flags.GetShort())
                {
                    payLoadLen = Next();
                }
                else
                {
                    byte[] tempLen = new byte[4];

                    for (int i = 0; i < tempLen.Length; i++)
                        tempLen[i] = Next();

                    Array.Reverse(tempLen);

                    payLoadLen = BitConverter.ToUInt32(tempLen, 0);
                }

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
            byte[] content = new byte[payLoadLen];
            Func<byte[], string> parser;

            for (int i = 0; i < payLoadLen; i++)
            {
                content[i] = Next();
            }
                

            if (type.Last().Equals("U"))
                parser = UriRecordPayload.Parse;
            else
                parser = TextRecordPayload.Parse;

            payloadEncoded.Add(parser(content));
        }

        public List<string> GetPayLoad()
        {
            return payloadEncoded;
        }

        private byte Next()
        {
            return tokens.Dequeue();
        }
    }
}
