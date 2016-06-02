using System;

namespace TapTrack.TappyUSB
{
    public class Tag
    {
        private byte typeOfTag;
        private byte[] serialNumber;

        public Tag(byte[] frameData)
        {
            typeOfTag = frameData[0];
            serialNumber = new byte[frameData[1]];
            Array.Copy(frameData, 2, serialNumber, 0, frameData[1]);
        }

        public Tag(byte typeOfTag, byte[] serialNumber)
        {
            this.typeOfTag = typeOfTag;
            this.serialNumber = serialNumber;
        }

        public Tag(byte[] serialNumber, byte typeOfTag)
        {
            this.typeOfTag = typeOfTag;
            this.serialNumber = serialNumber;
        }

        public byte TypeOfTag { get { return typeOfTag; } }

        public byte[] SerialNumber { get { return serialNumber; } }

        public int Length { get { return serialNumber.Length; } }
    }
}
