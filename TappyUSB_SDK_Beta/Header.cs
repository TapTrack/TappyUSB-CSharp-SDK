using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TapTrack.TappyUSB
{

    public class Header
    {
        private List<byte> data;

        public Header(bool mb, bool me, bool chuckFlag, bool isShort, bool il, byte tnf, int payLoadLength, string type, int idLength = 0, byte id = 0)
        {
            data = new List<byte>();
            data.Add((byte)(Convert.ToByte(mb) * 0x80 + Convert.ToByte(me) * 0x40 + Convert.ToByte(chuckFlag) * 0x20 + Convert.ToByte(isShort) * 0x10 + Convert.ToByte(il) * 0x08 + tnf));
            data.Add((byte)type.Length);

            if (isShort)
                data.Add((byte)payLoadLength);
            else
            {
                byte[] temp = BitConverter.GetBytes(payLoadLength);
                Array.Reverse(temp);
                data.AddRange(temp);
            }

            if (il)
                data.Add((byte)idLength);

            data.AddRange(Encoding.UTF8.GetBytes(type));

            if (il)
                data.Add(id);
        }

        // Gets the contents of the header
        public byte[] GetByteArray()
        {
            return data.ToArray<byte>();
        }
    }
}
