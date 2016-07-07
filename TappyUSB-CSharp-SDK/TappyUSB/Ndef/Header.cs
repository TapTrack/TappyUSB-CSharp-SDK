using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TapTrack.TappyUSB.Ndef
{
    public class Header
    {
        private List<byte> data;
        FlagHeader flags;

        /// <summary>
        /// Assumes IL = 1
        /// </summary>
        /// <param name="mb"></param>
        /// <param name="me"></param>
        /// <param name="chuckFlag"></param>
        /// <param name="isShort"></param>
        /// <param name="tnf"></param>
        /// <param name="payLoadLength"></param>
        /// <param name="type"></param>
        /// <param name="idLength"></param>
        /// <param name="id"></param>
        public Header(bool mb, bool me, bool chuckFlag, bool isShort, TypeNameField tnf, int payLoadLength, string type, int idLength, byte id)
        {
            data = new List<byte>();
            data.Add((byte)(Convert.ToByte(mb) * 0x80 + Convert.ToByte(me) * 0x40 + Convert.ToByte(chuckFlag) * 0x20 + Convert.ToByte(isShort) * 0x10 + (byte)tnf));
            data.Add((byte)type.Length);

            if (isShort)
                data.Add((byte)payLoadLength);
            else
            {
                byte[] temp = BitConverter.GetBytes(payLoadLength);
                Array.Reverse(temp);
                data.AddRange(temp);
            }

            data.Add((byte)idLength);

            data.AddRange(Encoding.UTF8.GetBytes(type));

            data.Add(id);
        }

        /// <summary>
        /// Assumes IL = 0
        /// </summary>
        /// <param name="mb"></param>
        /// <param name="me"></param>
        /// <param name="chuckFlag"></param>
        /// <param name="isShort"></param>
        /// <param name="tnf"></param>
        /// <param name="payLoadLength"></param>
        /// <param name="type"></param>
        public Header(bool mb, bool me, bool chuckFlag, bool isShort, TypeNameField tnf, int payLoadLength, string type)
        {
            data = new List<byte>();
            data.Add((byte)(Convert.ToByte(mb) * 0x80 + Convert.ToByte(me) * 0x40 + Convert.ToByte(chuckFlag) * 0x20 + Convert.ToByte(isShort) * 0x10 + (byte)tnf));

            data.Add((byte)type.Length);

            if (isShort)
                data.Add((byte)payLoadLength);
            else
            {
                byte[] temp = BitConverter.GetBytes(payLoadLength);
                Array.Reverse(temp);
                data.AddRange(temp);
            }

            data.AddRange(Encoding.UTF8.GetBytes(type));
        }



        // Gets the contents of the header
        public byte[] GetByteArray()
        {
            return data.ToArray<byte>();
        }
    }
}
