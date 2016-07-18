using System;
using System.Text;

namespace TapTrack.Classic
{
    internal class Frame
    {
        private byte[] frame;
        private int dataCount;
        private static readonly byte[] ACK = { 0x00, 0xFF, 0x00, 0x02, 0xFE, 0x00, 0xFF, 0x01 };
        private static readonly byte[] NACK = { 0x00, 0xFF, 0x00, 0x02, 0xFE, 0xFF, 0xFF, 0x02 };

        private Frame(byte[] frame, int dataCount)
        {
            this.frame = frame;
            this.dataCount = dataCount;
        }

        public static Frame Construct(byte lenM, byte lenL, byte LCS, byte[] data, byte DCS)
        {
            byte[] frame = new byte[6 + data.Length];
            frame[0] = 0x00;
            frame[1] = 0xFF;
            frame[2] = lenM;
            frame[3] = lenL;
            frame[4] = LCS;
            Array.Copy(data, 0, frame, 5, data.Length);
            frame[frame.Length - 1] = DCS;
            return new Frame(frame, data.Length);
        }

        protected static Frame _Construct(byte[] payLoadData)
        {
            byte lenM = (byte)(payLoadData.Length / 256);
            byte lenL = (byte)(payLoadData.Length - 256 * lenM);
            return Construct(lenM, lenL, CalLcs(lenM, lenL), payLoadData, CalDcs(payLoadData));
        }

        public static Frame ConstructCommand(byte command, params byte[] parameters)
        {
            byte[] payLoad = new byte[parameters.Length + 1];
            payLoad[0] = command;
            Array.Copy(parameters, 0, payLoad, 1, parameters.Length);
            return _Construct(payLoad);
        }

        public static Frame ConstructCommand(byte command, byte[] parameters, string url)
        {
            byte[] payLoad = new byte[parameters.Length + url.Length + 1];
            payLoad[0] = command;
            Array.Copy(parameters, 0, payLoad, 1, parameters.Length);
            Array.Copy(Encoding.UTF8.GetBytes(url), 0, payLoad, parameters.Length + 1, url.Length);
            return _Construct(payLoad);
        }

        public int Length { get { return frame.Length; } }

        public byte Lcs { get { return frame[4]; } }

        public byte LenM { get { return frame[2]; } }

        public byte LenL { get { return frame[3]; } }

        public byte Dcs { get { return frame[frame.Length - 1]; } }

        public int DataCount { get { return dataCount; } }

        public static byte CalLcs(byte lenM, byte lenL)
        {
            return (byte)(0 - lenM - lenL);
        }

        public byte[] GetData()
        {
            byte[] data = new byte[dataCount];
            Array.Copy(frame, 5, data, 0, dataCount);
            return data;
        }

        public static byte CalDcs(byte[] data)
        {
            byte DCS = 0;

            for (int i = 0; i < data.Length; i++)
                DCS -= data[i];

            return DCS;
        }

        public bool LcsError()
        {
            if (Lcs + LenM + LenL != 0)
                return true;
            return false;
        }

        public bool DcsError()
        {
            byte checkSum = 0;
            for (int i = 5; i < frame.Length; i++)
                checkSum = (byte)(checkSum + frame[i]);
            if (checkSum == 0)
                return false;
            return true;
        }

        public bool isValidFrame()
        {
            if (frame[0] != 0x00 || frame[1] != 0xFF)
                return false;
            if (LcsError() || DcsError())
                return false;

            return true;
        }

        /// <summary>
        /// Check if the frame is a ACK frame
        /// </summary>
        /// <returns>True if this frame is an ACK frame, false otherwise</returns>
        public bool IsACK()
        {
            if (Length != 8)
                return false;

            for (int i = 5; i < ACK.Length; i++)
            {
                if (ACK[i] != frame[i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Check if the frame is a NACK frame
        /// </summary>
        /// <returns>True if this frame is an NACK frame, false otherwise</returns>
        public bool IsNACK()
        {
            if (Length != 8)
                return false;

            for (int i = 5; i < NACK.Length; i++)
            {
                if (NACK[i] != frame[i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Check if the frame is an application error frame
        /// </summary>
        /// <returns>True if this frame is an application error frame, false otherwise</returns>
        public bool IsAppError()
        {
            if (Length != 10)
                return false;
            if (frame[5] != 0x7F)
                return false;
            return true;
        }

        /// <summary>
        /// Cast a Frame to a byte[]
        /// </summary>
        /// <param name="f"></param>
        static public implicit operator byte[] (Frame f)
        {
            return f.frame;
        }
    }
}
