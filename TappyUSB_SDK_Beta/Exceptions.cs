using System;

namespace TapTrack.TappyUSB.Exceptions
{
    class LcsException : Exception
    {
        public LcsException(string message) : base(message)
        {

        }
    }

    class LackOfDataException : Exception
    {
        public LackOfDataException(string message) : base(message)
        {

        }
    }

    class DcsException : Exception
    {
        public DcsException(string message) : base(message)
        {

        }
    }
}
