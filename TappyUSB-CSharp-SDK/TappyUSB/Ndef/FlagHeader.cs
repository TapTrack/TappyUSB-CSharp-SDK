using System;

namespace TapTrack.TappyUSB.Ndef
{
    class FlagHeader
    {
        HeaderFlags flags;

        [Flags]
        enum HeaderFlags : byte
        {
            MessageBegin = 0x80,
            MessageEnd = 0x40,
            Chuck = 0x20,
            Short = 0x10,
            Id = 0x08
        }

        public FlagHeader(byte flags)
        {
            this.flags = (HeaderFlags)flags;
        }

        public FlagHeader(bool mb, bool me, bool chuckFlag, bool isShort, bool il, byte tnf)
        {

        }

        public bool GetMb()
        {
            return (flags & HeaderFlags.MessageBegin) == HeaderFlags.MessageBegin;
        }

        public bool GetMe()
        {
            return (flags & HeaderFlags.MessageEnd) == HeaderFlags.MessageEnd;
        }

        public bool GetChunk()
        {
            return (flags & HeaderFlags.Chuck) == HeaderFlags.Chuck;
        }

        public bool GetSr()
        {
            return (flags & HeaderFlags.Short) == HeaderFlags.Short;
        }

        public bool GetIl()
        {
            return (flags & HeaderFlags.Id) == HeaderFlags.Id;
        }

        public byte GetTnf()
        {
            return (byte)((byte)flags & 0x07);
        }
    }
}
