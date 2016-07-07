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
        byte[] id;
        List<RecordData> payloadEncoded;
        List<string> type;
        List<TypeNameField> tnf;
        FlagHeader flags;

        public NdefParser(byte[] data)
        {
            tokens = new Queue<byte>(data);
            payloadEncoded = new List<RecordData>();
            type = new List<string>();
            tnf = new List<TypeNameField>();
            NdefMessage();
        }

        private void NdefMessage()
        {
            bool run = true;
            while (tokens.Count > 0 && run)
            {
                StringBuilder temp = new StringBuilder();

                flags = new FlagHeader(Next());
                tnf.Add(flags.GetTnf());

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

            id = idBytes ;
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
            else if (type.Last().Equals("T"))
                parser = TextRecordPayload.Parse;
            else
                parser = (byte[] data) => { return BitConverter.ToString(data); };

            payloadEncoded.Add(new RecordData(type.Last(), tnf.Last(), parser(content), id));
        }

        public List<RecordData> GetPayLoad()
        {
            return payloadEncoded;
        }

        private byte Next()
        {
            return tokens.Dequeue();
        }
    }
}
