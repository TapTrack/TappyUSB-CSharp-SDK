using System;

namespace TapTrack.Classic
{
    public class NfcTag
    {
        private byte typeOfTag;
        private byte[] uid;

        public NfcTag(byte typeOfTag, byte[] uid)
        {
            this.typeOfTag = typeOfTag;
            this.uid = uid;
        }

        public NfcTag(byte[] uid, byte typeOfTag)
        {
            this.typeOfTag = typeOfTag;
            this.uid = uid;
        }

        public static NfcTag FromReadUidResp(byte[] frameData)
        {
            byte typeOfTag = frameData[0];
            byte[] uid = new byte[frameData[1]];
            Array.Copy(frameData, 2, uid, 0, frameData[1]);

            return new NfcTag(typeOfTag, uid);
        }

        public static NfcTag FromWriteNdefResp(byte[] frameData)
        {
            byte typeOfTag = frameData[0];
            byte[] uid = new byte[frameData.Length - 1];
            Array.Copy(frameData, 1, uid, 0, uid.Length);

            return new NfcTag(typeOfTag, uid);
        }

        public byte TypeOfTag { get { return typeOfTag; } }

        public byte[] UID { get { return uid; } }

        public int Length { get { return uid.Length; } }

        public static string TypeLookUp(byte tagType)
        {
            if (tagType == 0x01)
            {
                return "Mifare Ultralight";
            }
            else if (tagType == 0x02)
            {
                return "NTAG 203";
            }
            else if (tagType == 0x03)
            {
                return "Mifare Ultralight C";
            }
            else if (tagType == 0x04)
            {
                return "Mifare Classic Standard - 1k";
            }
            else if (tagType == 0x05)
            {
                return "Mifare Classic Standard - 4k";
            }
            else if (tagType == 0x06)
            {
                return "Mifare DESFire EV1 2k";
            }
            else if (tagType == 0x07)
            {
                return "Generic NFC Forum Type 2 tag";
            }
            else if (tagType == 0x08)
            {
                return "Mifare Plus 2k CL2";
            }
            else if (tagType == 0x09)
            {
                return "Mifare Plus 4k CL2";
            }
            else if (tagType == 0x0A)
            {
                return "Mifare Mini";
            }
            else if (tagType == 0x0B)
            {
                return "Generic NFC Forum Type 4 tag";
            }
            else if (tagType == 0x0C)
            {
                return "Mifare DESFire EV1 4k";
            }
            else if (tagType == 0x0D)
            {
                return "Mifare DESFire EV1 8k";
            }
            else if (tagType == 0x0E)
            {
                return "Mifare DESFire - Unspecified model and capacity";
            }
            else if (tagType == 0x0F)
            {
                return "Topaz 512";
            }
            else if (tagType == 0x10)
            {
                return "NTAG 210";
            }
            else if (tagType == 0x11)
            {
                return "NTAG 212";
            }
            else if (tagType == 0x12)
            {
                return "NTAG 213";
            }
            else if (tagType == 0x13)
            {
                return "NTAG 215";
            }
            else if (tagType == 0x14)
            {
                return "NTAG 216";
            }
            else
            {
                return "Unknown tag";
            }
        }
    }
}
