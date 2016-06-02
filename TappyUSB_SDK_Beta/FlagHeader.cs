using System;

namespace TapTrack.TappyUSB
{
    class FlagHeader
    {
        byte flags;

        public FlagHeader(byte flags)
        {
            this.flags = flags;
        }

        public FlagHeader(bool mb, bool me, bool chuckFlag, bool isShort, bool il, byte tnf)
        {
            flags = (byte)(Convert.ToByte(mb) * 0x80 + Convert.ToByte(me) * 0x40 + Convert.ToByte(chuckFlag) * 0x20 + Convert.ToByte(isShort) * 0x10 + Convert.ToByte(il) * 0x08 + tnf);
        }

        public bool GetMb()
        {
            if ((flags & 0x80) == 0x80)
                return true;
            return false;
        }

        public bool GetMe()
        {
            if ((flags & 0x40) == 0x40)
                return true;
            return false;
        }

        public bool GetChunk()
        {
            if ((flags & 0x20) == 0x20)
                return true;
            return false;
        }

        public bool GetSr()
        {
            if ((flags & 0x10) == 0x10)
                return true;
            return false;
        }

        public bool GetIl()
        {
            if ((flags & 0x08) == 0x08)
                return true;
            return false;
        }

        public byte GetTnf()
        {
            return (byte)(flags & 0x07);
        }
    }
}
