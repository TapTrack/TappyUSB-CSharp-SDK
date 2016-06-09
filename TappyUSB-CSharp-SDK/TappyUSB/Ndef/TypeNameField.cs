using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TapTrack.TappyUSB.Ndef
{
    public enum TypeNameField : byte
    {
        Empty,
        NfcForumWellKnown,
        Media,
        AbsoluteUri,
        NfcForumExternal,
        Unknown,
        Unchanged
    }
}
