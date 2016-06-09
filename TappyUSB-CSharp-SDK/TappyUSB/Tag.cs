using System;

namespace TapTrack.TappyUSB
{
    public class Tag
    {
        private byte typeOfTag;
        private byte[] uid;

        public Tag(byte[] frameData)
        {
            typeOfTag = frameData[0];
            uid = new byte[frameData[1]];
            Array.Copy(frameData, 2, uid, 0, frameData[1]);
        }

        public Tag(byte typeOfTag, byte[] serialNumber)
        {
            this.typeOfTag = typeOfTag;
            this.uid = serialNumber;
        }

        public Tag(byte[] serialNumber, byte typeOfTag)
        {
            this.typeOfTag = typeOfTag;
            this.uid = serialNumber;
        }

        public byte TypeOfTag { get { return typeOfTag; } }

        public byte[] UID { get { return uid; } }

        public int Length { get { return uid.Length; } }
    }
}
