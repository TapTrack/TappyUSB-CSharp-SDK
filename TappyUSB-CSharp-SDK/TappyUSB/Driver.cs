using System;
using System.Collections.Generic;
using System.IO.Ports;
using TapTrack.TappyUSB.Exceptions;
using System.Diagnostics;
using System.Threading;
using TapTrack.TappyUSB.Ndef;
using System.Text;

namespace TapTrack.TappyUSB
{
    /// <summary>
    /// The different type of errors callbacks can receive
    /// </summary>
    public enum TappyError
    {
        Nack,
        Dcs,
        Lcs,
        Application,
        Hardware
    }

    /// <summary>
    /// The type of content that can be written using <c></c>WriteContentToTag</c>
    /// </summary>
    public enum ContentType
    {
        Uri = 0x01,
        Text = 0x02,
        Vcard = 0x04,
        Empty = 0x99
    }

    /// <summary>
    /// The Driver class is used to communicate(send commands and receive data) with the TappyUSB.
    /// </summary>
    public class Driver
    {
        public delegate void Callback(params byte[] data);
        public delegate void CallbackError(TappyError code, params byte[] data);

        private class CallbackSet
        {
            private Callback successHandler;
            private Callback ackHandler;
            private CallbackError errorHandler;

            public CallbackSet(Callback successHandler, Callback ackHandler = null, CallbackError errorHandler = null)
            {
                this.successHandler = successHandler;
                this.ackHandler = ackHandler;
                this.errorHandler = errorHandler;
            }

            public Callback SuccessHandler
            {
                get { return successHandler; }
                set { successHandler = value; }
            }

            public Callback AckHandler
            {
                get { return ackHandler; }
                set { ackHandler = value; }
            }

            public CallbackError ErrorHandler
            {
                get { return errorHandler; }
                set { errorHandler = value; }
            }
        }

        /// <summary>
        /// Command codes that can be sent to the TappyUSB
        /// </summary>
        private class CMD
        {
            public const byte ADD_CONTENT = 0x02;
            public const byte EMULATE = 0x03;
            public const byte READ_UID = 0x07;
            public const byte STOP = 0x27;
            public const byte WRITE_TAG = 0x08;
            public const byte READ_NDEF = 0x26;
            public const byte WRITE_NDEF = 0x29;
            public const byte DETECT_TYPE_B = 0x2A;
            public const byte TRANSCEIVE = 0x2B;
            public const byte TRANSCEIVE_COUNT = 0x2C;
            public const byte DETECT_TYPE_B_AFI = 0x2D;
            public const byte WRITE_VCARD = 0x80;
            public const byte LOCK_TAG = 0x13;
            public const byte WRITE_BLOCK = 0x28;
        }

        private List<byte> buffer;
        private SerialPort port;
        private SerialDataReceivedEventHandler responseHandler;

        private Callback ackCb;
        private Callback validResponseFrameCb;
        private CallbackError lcsErrorResponseCb;
        private CallbackError dcsErrorResponseCb;
        private CallbackError appErrorCb;
        private CallbackError nackCb;

        /// <summary>
        /// Create a new instance to communicate with the TappyUSB
        /// </summary>
        /// <param name="portName">If portName is omitted then you must connect later using the Connect method</param>
        public Driver(string portName = null)
        {
            buffer = new List<byte>();

            port = new SerialPort();

            responseHandler = new SerialDataReceivedEventHandler(DataReceivedHandler);
            port.DataReceived += responseHandler;

            port.WriteTimeout = SerialPort.InfiniteTimeout;     // Tappy will handle timeouts not the OS
            port.ReadTimeout = SerialPort.InfiniteTimeout;

            port.BaudRate = 115200;
            port.RtsEnable = false;

            if (portName != null)
                Connect(portName);
        }

        /// <summary>
        /// Ping the current port the driver is connected to, this command will stop other commands that were sent
        /// </summary>
        /// <returns>True if the TappyUSB replied, false otherwise</returns>
        public bool Ping()
        {
            bool responded = false;
            AutoResetEvent receieve = new AutoResetEvent(false);

            Callback ack = (byte[] data) =>
            {
                responded = true;
                receieve.Set();
            };

            Stop(ackHandler: ack);
            receieve.WaitOne(100);
            if (responded)
                return true;

            return false;
        }

        /// <summary>
        /// Will disconnect from the current port and attempt to connect to the port specified (portName). Will only connect if it is a TappyUSB
        /// </summary>
        /// <param name="portName">Port to connect to</param>
        /// <returns>True if connection was successful, false otherwise</returns>
        public bool Connect(string portName)
        {
            try
            {
                if (port.IsOpen)
                    port.Close();
                port.PortName = portName;
                port.Open();
                return Ping();
            }
            catch
            {
                return false;
            }
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            Frame respFrame = null;

            if (ReadFromPort() == 0)
                return;


            for (int i = 0; i < buffer.Count - 5; i++)
            {
                if (buffer[i] == 0x00 && buffer[i + 1] == 0xFF)
                {
                    respFrame = null;
                    try
                    {
                        respFrame = GetFrame(i);
                        buffer.RemoveRange(0, i + respFrame.Length);

                        if (respFrame != null)
                            Execute(respFrame);

                        i = 0;
                    }
                    catch (LcsException exc)
                    {
                        Debug.WriteLine(exc.Message);
                        buffer.RemoveRange(0, i + 5);
                        i = 0;
                    }
                    catch (LackOfDataException exc)
                    {
                        Debug.WriteLine(exc.Message);
                        Debug.WriteLine("Trying to read from port");
                        if (ReadFromPort() > 0)
                            i -= 1;
                    }
                    catch (Exception exc)
                    {
                        Debug.WriteLine(exc.Message);
                    }
                }
            }
        }

        private void Execute(Frame respFrame)
        {
            if (respFrame.DcsError())
            {
                Debug.WriteLine("DCS error");
                dcsErrorResponseCb?.Invoke(TappyError.Dcs, respFrame);
            }
            else if (respFrame.IsACK())
            {
                Debug.WriteLine("   ACK");
                ackCb?.Invoke(respFrame);
            }
            else if (respFrame.IsNACK())
            {
                Debug.WriteLine("   NACK");
                nackCb?.Invoke(TappyError.Nack);
            }
            else
            {
                if (respFrame.IsAppError())
                {
                    byte[] data = respFrame.GetData();
                    Debug.WriteLine("   Error: " + AppErrorLookUp(data[2]));
                    appErrorCb?.Invoke(TappyError.Application, data);
                }
                else
                {
                    Debug.WriteLine("   Success");
                    validResponseFrameCb?.Invoke(respFrame.GetData());
                }
            }
        }

        private Frame GetFrame(int start)
        {
            byte lenM = 0;
            byte lenL = 0;
            byte LCS = 0;
            int offset = start + 5;
            byte[] data;
            Frame result = null;

            lenM = buffer[start + 2];
            lenL = buffer[start + 3];
            LCS = buffer[start + 4];

            if (buffer.Count - start - 5 < lenM * 256 + lenL)
                throw new LackOfDataException("Length of frame is greater than buffer length");

            if ((byte)(LCS + lenM + lenL) != 0)
                throw new LcsException("Length ceck sum is not equal to 0");

            data = new byte[lenM * 256 + lenL];

            for (int i = 0; i < lenM * 256 + lenL; i++)
                data[i] = buffer[i + offset];

            result = Frame.Construct(lenM, lenL, LCS, data, buffer[start + 5 + lenM * 256 + lenL]);
            return result;
        }

        /// <summary>
        /// Read the unique ID from a type A tag
        /// </summary>
        /// 
        /// <example>
        /// Simple example of how to read a unique identifier from tag
        /// <code language="cs">
        /// using System;
        /// using TapTrack.TappyUSB;
        /// 
        /// namespace TapTrack.TappyUSB.Example
        /// {
        ///     class ReadUIDExample
        ///     { 
        ///         static void Main(string[] args)
        ///         {
        ///             Driver tappyDriver = new Driver();
        ///         
        ///             tappyDriver.AutoDetect();                   // Automatically connect to the first TappyUSB the driver finds
        ///         
        ///             tappyDriver.ReadUID(0, SuccessCallback);    // Send the ReadUID command with no timeout. SuccessCallback is run when a valid tag is detected
        ///             Console.WriteLine("Please tap tag");
        ///             Console.ReadKey();                          // Stop the program from exiting
        ///         }
        ///         
        ///         public static void SuccessCallback(byte[] data)
        ///         {
        ///             Tag tag = new Tag(data);
        ///         
        ///             string uid = BitConverter.ToString(tag.UID);    // Convert byte array to a string of hexadecimal characters
        ///         
        ///             Console.WriteLine("UID: {0}", uid);
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        /// 
        /// <param name="timeout">The max time the TappyUSB will wait for a tag. 0 = infinite timeout</param>
        /// <param name="successHandler">Method to be called when a successful read occurs</param>
        /// <param name="ackHandler">Method to be called when an ACK is received</param>
        /// <param name="errorHandler">Method to be called when an application, NACK, DCS, or LCS error occurs</param>
        public void ReadUID(byte timeout, Callback successHandler, Callback ackHandler = null, CallbackError errorHandler = null)
        {
            Debug.WriteLine("Starting Command: Read");
            Send(Frame.ConstructCommand(Driver.CMD.READ_UID, timeout), new CallbackSet(successHandler, ackHandler, errorHandler));
        }

        private void AddContent(byte index, ContentType type, byte uriCode, byte[] content, Callback successHandler, Callback ackHandler = null, CallbackError errorHandler = null)
        {
            Debug.WriteLine("Starting Command: Add Content");
            List<byte> payload = new List<byte>();
            payload.Add(index);
            payload.Add((byte)type);
            payload.Add(uriCode);
            payload.AddRange(content);
            Send(Frame.ConstructCommand(Driver.CMD.ADD_CONTENT, payload.ToArray()), new CallbackSet(successHandler, ackHandler, errorHandler));
        }

        private void AddContent(byte index, ContentType type, string content, Callback successHandler, Callback ackHandler = null, CallbackError errorHandler = null)
        {
            byte uriCode;
            byte[] contentBytes;

            if (type == ContentType.Uri)
            {
                Uri temp = new Uri(content);
                uriCode = temp.Scheme;
                contentBytes = Encoding.UTF8.GetBytes(temp.Path);
            }
            else
            {
                uriCode = 0x00;
                contentBytes = Encoding.UTF8.GetBytes(content);
            }

            Debug.WriteLine("Starting Command: Add Content");
            List<byte> payload = new List<byte>();
            payload.Add(index);
            payload.Add((byte)type);
            payload.Add(uriCode);
            payload.AddRange(contentBytes);
            Send(Frame.ConstructCommand(Driver.CMD.ADD_CONTENT, payload.ToArray()), new CallbackSet(successHandler, ackHandler, errorHandler));
        }

        /// <summary>
        /// <para>The TappyUSB acts as a tag with URL, Text, or VCard content</para>
        /// <note type="important">
        ///     <list type="bullet">
        ///         <item>
        ///             This command is only supported on certain firmware versions. 
        ///             Contact TapTrack to check if this command is available on the version of firmware currently on your
        ///             device.
        ///         </item>
        ///         <item>
        ///             The emulation of the TappyUSB may not be detected by all phones
        ///         </item>
        ///     </list>
        /// </note>
        /// </summary>
        /// <param name="type">Type of content that will be emulated. Either a Uri, text, or Vcard</param>
        /// <param name="content"></param>
        /// <param name="interruptable">If true the TappyUSB can not be interrupted by a stop command while emulating, if false the TappyUSB can be interrupted</param>
        /// <param name="maxScanCount">The maximum number of scans of the emulated content. When the max number of scans is reached the TappyUSB will stop emulating</param>
        /// <param name="timeout"></param>
        /// <param name="ackHandler">Method to be called when an ACK is received</param>
        /// <param name="errorHandler">Method to be called when an application, NACK, DCS, or LCS error occurs</param>
        public void Emulate(ContentType type, string content, bool interruptable, byte maxScanCount, short timeout, Callback ackHandler = null, CallbackError errorHandler = null)
        {
            Debug.WriteLine("Starting Command: Emulate");
            byte[] timeoutByte = BitConverter.GetBytes(timeout);
            Callback ack = (byte[] data) => Send(Frame.ConstructCommand(Driver.CMD.EMULATE, 0, Convert.ToByte(interruptable), maxScanCount, timeoutByte[1], timeoutByte[0]), new CallbackSet(null, ackHandler, errorHandler));

            AddContent(0, type, content, null, ack);
        }

        private void Write(byte index, bool willLock, Callback successHandler, Callback ackHandler = null, CallbackError errorHandler = null)
        {
            Debug.WriteLine("Starting Command: Writing to Tag");
            byte[] payload = { index, Convert.ToByte(willLock), 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            Send(Frame.ConstructCommand(Driver.CMD.WRITE_TAG, payload), new CallbackSet(successHandler, ackHandler, errorHandler));
        }

        /// <summary>
        /// Will write text, uri, VCard, or empty content to a NFC tag
        /// </summary>
        /// <example>
        ///     <para>
        ///         How to write text to a tag
        ///     </para>
        ///     <code language="cs">
        ///         using System;
        ///         using TapTrack.TappyUSB;
        ///         
        ///         namespace TapTrack.TappyUSB.Example
        ///         {
        ///             class WriteTextExample
        ///             {
        ///                 static void Main(string[] args)
        ///                 {
        ///                     Driver tappyDriver = new Driver();
        ///         
        ///                     tappyDriver.AutoDetect();                   // Automatically connect to the first TappyUSB the driver finds
        ///         
        ///                     // Write the text "Hello world!" to a tag that will not a locked after writing
        ///                     tappyDriver.WriteContentToTag(ContentType.Text, "Hello world!", false, null);
        ///         
        ///                     Console.WriteLine("Please tap tag");
        ///                     Console.ReadKey();                          // Stop the program from exiting
        ///                 }
        ///             }
        ///         }   
        ///     </code>
        ///     <para>
        ///         Writing a URL to a tag
        ///     </para>
        ///     <code language="cs">
        ///         using System;
        ///         using TapTrack.TappyUSB;
        ///         
        ///         namespace TapTrack.TappyUSB.Example
        ///         {
        ///             class WriteUrlExample
        ///             {
        ///                 static void Main(string[] args)
        ///                 {
        ///                     Driver tappyDriver = new Driver();
        ///         
        ///                     tappyDriver.AutoDetect();                   // Automatically connect to the first TappyUSB the driver finds
        ///         
        ///                     // Write the url "http://google.ca" to a tag that will not a locked after writing
        ///                     tappyDriver.WriteContentToTag(ContentType.Uri, "http://google.ca", false, null);
        ///         
        ///                     Console.WriteLine("Please tap tag");
        ///                     Console.ReadKey();                          // Stop the program from exiting
        ///                 }
        ///             }
        ///         }   
        ///     </code>
        /// </example>
        /// <param name="type">Type of content that will be sent</param>
        /// <param name="content">The data to be written to the tag</param>
        /// <param name="willLock">If true the Tappy will lock the tag after writing, preventing further writes to the tag</param>
        /// <param name="successHandler">Method to be called when a success occurs</param>
        /// <param name="ackHandler">Method to be called when an ACK is received</param>
        /// <param name="errorHandler">Method to be called when an application, NACK, DCS, or LCS error occurs</param>
        public void WriteContentToTag(ContentType type, string content, bool willLock, Callback successHandler, Callback ackHandler = null, CallbackError errorHandler = null)
        {
            Callback ack = (byte[] data) => Write(0, willLock, successHandler, ackHandler, errorHandler);

            AddContent(0, type, content, null, ack, errorHandler);
        }

        /// <summary>
        /// Will write text, uri, VCard, or empty content to a NFC tag. Instructs the tappy to continue to write after a tag is written
        /// </summary>
        /// <param name="type">Type of content that will be sent</param>
        /// <param name="content">The data to be written to the tag</param>
        /// <param name="willLock">If true the Tappy will lock the tag after writing, preventing further writes to the tag</param>
        /// <param name="successHandler">Method to be called when a success occurs</param>
        /// <param name="ackHandler">Method to be called when an ACK is received</param>
        /// <param name="errorHandler">Method to be called when an application, NACK, DCS, or LCS error occurs</param>
        public void WriteContentToTagMultTimes(ContentType type, string content, bool willLock, Callback successHandler, Callback ackHandler = null, CallbackError errorHandler = null)
        {
            Callback write = null;
            write = (byte[] data) =>
            {
                Thread.Sleep(500);
                Write(0, willLock, successHandler + write, ackHandler, errorHandler);
            };

            WriteContentToTag(type, content, willLock, successHandler + write, ackHandler, errorHandler);
        }

        /// <summary>
        /// Detect a type 4B tag
        /// </summary>
        /// <param name="timeout">The max time the TappyUSB will wait for a tag. 0 = infinite timeout</param>
        /// <param name="successHandler">Method to be called when a success occurs</param>
        /// <param name="ackHandler">Method to be called when an ACK is received</param>
        /// <param name="errorHandler">Method to be called when an application, NACK, DCS, or LCS error occurs</param>
        public void DetectTypeB(byte timeout, Callback successHandler, Callback ackHandler = null, CallbackError errorHandler = null)
        {
            Debug.WriteLine("Starting Command: Detect Type B");
            Send(Frame.ConstructCommand(Driver.CMD.DETECT_TYPE_B, timeout), new CallbackSet(successHandler, ackHandler, errorHandler));
        }

        /// <summary>
        /// Detect a type B tag only if the AFI of the tag matches the AFI given
        /// </summary>
        /// <param name="timeout">The max time the TappyUSB will wait for a tag. 0 = infinite timeout</param>
        /// <param name="AFI">aplication family identifier of the tag to be detected</param>
        /// <param name="successHandler">Method to be called when a success occurs</param>
        /// <param name="ackHandler">Method to be called when an ACK is received</param>
        /// <param name="errorHandler">Method to be called when an application, NACK, DCS, or LCS error occurs</param>
        public void DetectTypeB(byte timeout, byte AFI, Callback successHandler, Callback ackHandler = null, CallbackError errorHandler = null)
        {
            Debug.WriteLine("Starting Command: Detect Type B");
            Send(Frame.ConstructCommand(Driver.CMD.DETECT_TYPE_B_AFI, timeout, AFI), new CallbackSet(successHandler, ackHandler, errorHandler));
        }

        /// <summary>
        /// Transceive bytes to a tag
        /// </summary>
        /// <param name="content">The array of bytes to be sent</param>
        /// <param name="successHandler">Method to be called when a success occurs</param>
        /// <param name="ackHandler">Method to be called when an ACK is received</param>
        /// <param name="errorHandler">Method to be called when an application, NACK, DCS, or LCS error occurs</param>
        public void Transceive(byte[] content, Callback successHandler, Callback ackHandler = null, CallbackError errorHandler = null)
        {
            if (content.Length == 0)
                throw new ArgumentException("Data sent must be greater than 0");
            Debug.WriteLine("Starting Command: Transceive");
            Send(Frame.ConstructCommand(Driver.CMD.TRANSCEIVE, content), new CallbackSet(successHandler, ackHandler, errorHandler));
        }

        /// <summary>
        /// Transceive bytes to a tag with a counter
        /// </summary>
        /// <param name="content">The array of bytes to be sent</param>
        /// <param name="counter">A 4 byte identifier of this transceive</param>
        /// <param name="successHandler">Method to be called when a success occurs</param>
        /// <param name="ackHandler">Method to be called when an ACK is received</param>
        /// <param name="errorHandler">Method to be called when an application, NACK, DCS, or LCS error occurs</param>
        public void Transceive(byte[] content, byte[] counter, Callback successHandler, Callback ackHandler = null, CallbackError errorHandler = null)
        {
            if (content.Length == 0)
                throw new ArgumentException("Data sent must be greater than 0");
            if (counter.Length != 4)
                throw new ArgumentException("counter sent must be length of 4");
            Debug.WriteLine("Starting Command: Transceive");
            List<byte> payload = new List<byte>(content);
            payload.AddRange(counter);
            Send(Frame.ConstructCommand(Driver.CMD.TRANSCEIVE_COUNT, payload.ToArray()), new CallbackSet(successHandler, ackHandler, errorHandler));
        }

        /// <summary>
        /// Write an NDEF message to a NFC tag
        /// </summary>
        /// 
        /// <example>
        /// <para>
        ///     Example of how to write a Ndef message with a single text record
        /// </para>
        /// 
        /// 
        /// <code language="cs">
        ///      using System;
        ///      using TapTrack.TappyUSB.Ndef;
        ///      
        ///      namespace TapTrack.TappyUSB.Example
        ///      {
        ///          class WriteNdefExample
        ///          {
        ///              static void Main(string[] args)
        ///              {
        ///                  Driver tappyDriver = new Driver();
        ///                  TextRecordPayload payload = new TextRecordPayload("en", "Hello world!");   // Ndef message with one text record
        ///      
        ///                  tappyDriver.AutoDetect();                                                  // Automatically connect to the first TappyUSB the driver finds
        ///      
        ///                  tappyDriver.WriteNdef(0, false, new NdefMessage(payload), null);           // Send the WriteNdef command with no timeout and no callbacks
        ///      
        ///                  Console.WriteLine("Please tap tag");
        ///                  Console.ReadKey();                                                         // Stop the program from exiting
        ///              }
        ///          }
        ///      }
        /// </code>
        /// <para>
        ///     How to write a multi-Ndef record to a tag
        /// </para>
        /// <code language="cs">
        ///    using System;
        ///    using System.Collections.Generic;
        ///    using TapTrack.TappyUSB.Ndef;
        ///    
        ///    namespace TapTrack.TappyUSB.Example
        ///    {
        ///        class WriteNdefExample
        ///        {
        ///            static void Main(string[] args)
        ///            {
        ///                Driver tappyDriver = new Driver();
        ///    
        ///                List&lt;RecordPayload&gt; payload = new List&lt;RecordPayload&gt;();
        ///    
        ///                TextRecordPayload textRecord = new TextRecordPayload("en", "Hello world!");
        ///                UriRecordPayload uriRecord = new UriRecordPayload("http://www.taptrack.com");
        ///    
        ///                payload.Add(textRecord);
        ///                payload.Add(uriRecord);
        ///    
        ///                tappyDriver.AutoDetect();
        ///    
        ///                tappyDriver.WriteNdef(0, false, new NdefMessage(payload.ToArray()), null);
        ///    
        ///                Console.WriteLine("Please tap tag");
        ///                Console.ReadKey();
        ///            }
        ///        }
        ///    }
        /// </code>
        /// 
        /// </example>
        /// 
        /// <param name="timeout">The max time the TappyUSB will wait for a tag. 0 = infinite timeout</param>
        /// <param name="willLock">Determine whether to lock to tag after writing, locking prevents further writing to the tag</param>
        /// <param name="message">Ndef message to be sent</param>
        /// <param name="successHandler">Method to be called when a success occurs</param>
        /// <param name="ackHandler">Method to be called when an ACK is received</param>
        /// <param name="errorHandler">Method to be called when an application, NACK, DCS, or LCS error occurs</param>
        public void WriteNdef(byte timeout, bool willLock, NdefMessage message, Callback successHandler, Callback ackHandler = null, CallbackError errorHandler = null)
        {
            Debug.WriteLine("Starting Command: Write Ndef");
            List<byte> data = new List<byte>();
            data.Add(timeout);
            data.Add(Convert.ToByte(willLock));
            data.AddRange(message.GetByteArray());
            Send(Frame.ConstructCommand(Driver.CMD.WRITE_NDEF, data.ToArray()), new CallbackSet(successHandler, ackHandler, errorHandler));
        }

        /// <summary>
        /// Will continuously write Ndef messages to tags that are presented to the TappyUSB
        /// </summary>
        /// <param name="timeout">The max time the TappyUSB will wait for a tag. 0 = infinite timeout</param>
        /// <param name="willLock">Determine whether to lock to tag after writing, locking prevents further writing to the tag</param>
        /// <param name="message">Ndef message to sent</param>
        /// <param name="successHandler">Method to be called when a successful write occurs</param>
        /// <param name="ackHandler">Method to be called when an ACK is received</param>
        /// <param name="errorHandler">Method to be called when an application, NACK, DCS, or LCS error occurs</param>
        public void WriteNdefMultTimes(byte timeout, bool willLock, NdefMessage message, Callback successHandler, Callback ackHandler = null, CallbackError errorHandler = null)
        {
            Callback write = null;
            write = (byte[] data) =>
            {
                Thread.Sleep(500);
                WriteNdef(timeout, willLock, message, successHandler + write, ackHandler, errorHandler);
            };

            WriteNdef(timeout, willLock, message, successHandler + write, ackHandler, errorHandler);
        }

        /// <summary>
        /// Read an Ndef message from a tag
        /// </summary>
        /// <param name="timeout">The max time the TappyUSB will wait for a tag. 0 = infinite timeout</param>
        /// <param name="successHandler">Method to be called when a successful read occurs</param>
        /// <param name="ackHandler">Method to be called when an ACK is received</param>
        /// <param name="errorHandler">Method to be called when an application, NACK, DCS, or LCS error occurs</param>
        public void ReadNdef(byte timeout, Callback successHandler, Callback ackHandler = null, CallbackError errorHandler = null)
        {
            Debug.WriteLine("Starting Command: Read Ndef");

            Send(Frame.ConstructCommand(Driver.CMD.READ_NDEF, timeout), new CallbackSet(successHandler, ackHandler, errorHandler));
        }

        /// <summary>
        /// Will write VCard formatted info to an NFC tag
        /// </summary>
        /// <param name="info">The data to be written</param>
        /// <param name="willLock">if true, the tag will be locked after writing</param>
        /// <param name="successHandler">Method to be called when a successful write occurs</param>
        /// <param name="ackHandler">Method to be called when an ACK is received</param>
        /// <param name="errorHandler">Method to be called when an application, NACK, DCS, or LCS error occurs</param>
        public void WriteVCard(VCard info, bool willLock, Callback successHandler, Callback ackHandler = null, CallbackError errorHandler = null)
        {
            Debug.WriteLine("Starting Command: Write VCard");
            Callback ack = (byte[] data) => Write(0, willLock, successHandler, ackHandler, errorHandler);

            AddContent(0, ContentType.Vcard, 0x00, info.ToByteArray(), null, ack, errorHandler);
        }

        /// <summary>
        /// Locks a tag to prevent further writing to the tag
        /// </summary>
        /// <param name="timeout">The max time the TappyUSB will wait for a tag. 0 = infinite timeout</param>
        /// <param name="successHandler">Method to be called when a successful lock occurs</param>
        /// <param name="ackHandler">Method to be called when an ACK is received</param>
        /// <param name="errorHandler">Method to be called when an application, NACK, DCS, or LCS error occurs</param>
        public void LockCard(byte timeout, Callback successHandler, Callback ackHandler = null, CallbackError errorHandler = null)
        {
            Debug.WriteLine("Starting Command: Locking card");

            Send(Frame.ConstructCommand(Driver.CMD.LOCK_TAG, timeout), new CallbackSet(successHandler, ackHandler, errorHandler));
        }

        /// <summary>
        /// Will write a set of bytes to a block of memory in a tag
        /// </summary>
        /// <param name="timeout">The max time the TappyUSB will wait for a tag. 0 = infinite timeout</param>
        /// <param name="willLock">if true, the tag will be locked after writing</param>
        /// <param name="startBlock">block in the memory of the tag to start writing</param>
        /// <param name="typeOfTag">Only accepts 0x07</param>
        /// <param name="data">array of bytes to write to the tag</param>
        /// <param name="successHandler">Method to be called when a successful write occurs</param>
        /// <param name="ackHandler">Method to be called when an ACK is received</param>
        /// <param name="errorHandler">Method to be called when an application, NACK, DCS, or LCS error occurs</param>
        public void WriteBlock(byte timeout, bool willLock, byte startBlock, byte typeOfTag, byte[] data, Callback successHandler, Callback ackHandler = null, CallbackError errorHandler = null)
        {
            List<byte> payload = new List<byte>();
            payload.Add(timeout);
            payload.Add(startBlock);
            payload.Add(typeOfTag);
            payload.Add(Convert.ToByte(willLock));
            payload.Add(0);
            payload.AddRange(data);
            Send(Frame.ConstructCommand(Driver.CMD.WRITE_BLOCK, payload.ToArray()), new CallbackSet(successHandler, ackHandler, errorHandler));
        }

        private void UnSafeSend(byte[] frame)
        {
            try
            {
                Debug.Write("   Sending: ");
                port.Write(frame, 0, frame.Length);
                foreach (byte b in frame)
                {
                    Debug.Write(string.Format("{0:X}", b).PadLeft(2, '0') + " ");
                }
                Debug.WriteLine("");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private void Send(Frame frame, CallbackSet cbSet)
        {
            if (!port.IsOpen)
                cbSet.ErrorHandler?.Invoke(TappyError.Hardware);

            Callback flushAndSend = (byte[] data) =>
            {
                Flush();
                ackCb = cbSet.AckHandler;
                nackCb = cbSet.ErrorHandler;
                lcsErrorResponseCb = cbSet.ErrorHandler;
                dcsErrorResponseCb = cbSet.ErrorHandler;
                validResponseFrameCb = cbSet.SuccessHandler;
                appErrorCb = cbSet.ErrorHandler;
                UnSafeSend(frame);
            };

            validResponseFrameCb = ackCb = flushAndSend;
            dcsErrorResponseCb = lcsErrorResponseCb = nackCb = appErrorCb = (TappyError errorCode, byte[] data) =>
            {
                if (data.Length > 0 && data[0] == Driver.CMD.STOP)
                    Debug.WriteLine("Unrecogized command for stop, this Tappy probably predates stop");
                flushAndSend();
            };
            Stop();
        }

        /// <summary>
        /// Will clear the serial port buffer and the driver buffer
        /// </summary>
        private void Flush()
        {
            try
            {
                Debug.WriteLine("   Flushing");
                buffer.Clear();
                port.DiscardInBuffer();
                port.DiscardOutBuffer();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                appErrorCb?.Invoke(TappyError.Hardware);
            }

        }

        /// <summary>
        /// Will clear all current callbacks that are set
        /// </summary>
        private void Clear()
        {
            validResponseFrameCb = null;
            ackCb = null;
            nackCb = null;
            lcsErrorResponseCb = null;
            dcsErrorResponseCb = null;
            appErrorCb = null;
        }

        /// <summary>
        /// Will issue a stop command to the TappyUSB. This will stop the operations the TappyUSB is currently doing
        /// </summary>
        /// <param name="ackHandler"></param>
        /// <param name="errorHandler"></param>
        public void Stop(Callback ackHandler = null, CallbackError errorHandler = null)
        {
            ackCb = ackHandler ?? ackCb;
            nackCb = errorHandler ?? nackCb;
            lcsErrorResponseCb = errorHandler ?? lcsErrorResponseCb;
            dcsErrorResponseCb = errorHandler ?? dcsErrorResponseCb;
            appErrorCb = errorHandler ?? appErrorCb;

            Debug.WriteLine("   Stopping");
            byte[] stopInstruction = Frame.ConstructCommand(Driver.CMD.STOP);
            UnSafeSend(stopInstruction);
        }

        /// <summary>
        /// Will read all bytes from the buffer
        /// </summary>
        /// <returns>The number of bytes read</returns>
        private int ReadFromPort()
        {
            try
            {
                byte temp;
                int count = 0;

                Debug.Write("   received: ");

                while (port.BytesToRead > 0)
                {
                    temp = (byte)port.ReadByte();
                    buffer.Add(temp);
                    count++;
                    Debug.Write(string.Format("{0:X}", temp).PadLeft(2, '0') + " ");
                }

                Debug.WriteLine("");
                return count;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Will connect to the first TappyUSB the driver finds
        /// </summary>
        /// <returns>True if the method was able to connect, false otherwise</returns>
        public bool AutoDetect()
        {
            foreach (string name in SerialPort.GetPortNames())
            {
                if (Connect(name))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Disconnects from the current device the driver is connected to
        /// </summary>
        public void Disconnect()
        {
            port.Close();
        }

        /// <summary>
        /// Get the current port name which this driver is connected to
        /// </summary>
        public string PortName
        {
            get
            {
                return port.PortName;
            }
        }

        /// <summary>
        /// Will resolve an application error code to human readable text
        /// </summary>
        /// <param name="errorCode">Application error code from the TappyUSB</param>
        /// <returns>Human readable error</returns>
        public string AppErrorLookUp(byte errorCode)
        {
            if (errorCode == 0x01)
                return "Invalid content index number";
            else if (errorCode == 0x02)
                return "Invalid content type";
            else if (errorCode == 0x03)
                return "NDEF message too big to fit on this tag";
            else if (errorCode == 0x04)
                return "Not all 11 vCard field lengths were received";
            else if (errorCode == 0x05)
                return "The length of the instruction does not correspond with the field lengths that were passed";
            else if (errorCode == 0x06)
                return "Comma expected – this occurs if there was not a comma found where there should be one based on the field name lengths that were passed";
            else if (errorCode == 0x07)
                return "Field parsing error";
            else if (errorCode == 0x08)
                return "Problem detecting tag";
            else if (errorCode == 0x09)
                return "Unrecognized command";
            else if (errorCode == 0x0B)
                return "Content index not populated with content";
            else if (errorCode == 0x0C)
                return "NDEF formatting error – the OTP bytes are already set to non-NDEF";
            else if (errorCode == 0x0D)
                return "Tag is locked, cannot write to the tag";
            else if (errorCode == 0x0E)
                return "Unsupported tag technology";
            else if (errorCode == 0x0F)
                return "Problem locking the tag (if this error is produced, it means the tag was written to successfully)";
            else if (errorCode == 0x10)
                return "Timeout – tag not detected";
            else if (errorCode == 0x11)
                return "Problem formatting Mifare  Classic Application Directory (MAD)";
            else if (errorCode == 0x12)
                return "Problem writing Mifare Classic sector  trailer";
            else if (errorCode == 0x13)
                return "Problem writing to Mifare Classic NDEF data secotrs";
            else if (errorCode == 0x14)
                return "Incorrect random number size (it means you did not pass precisely 8 random bytes)";
            else if (errorCode == 0x27)
                return "Incorrect number of parameters";
            else if (errorCode == 0x29)
                return "Problem formatting Type 1 tag as NDEF";
            else if (errorCode == 0x37)
                return "No NDEF data found";
            else if (errorCode == 0x38)
                return "Unrecognized NDEF version";
            else if (errorCode == 0x39)
                return "Error reading NDEF data";
            else if (errorCode == 0x3A)
                return "ERR_NTAG21X_INCORRECT_UID_LENGTH_FOR_PWD_DIVERSIFICATION";
            else if (errorCode == 0x3B)
                return "ERR_NTAG21X_ERROR_READING_SERIAL_NUMBER";
            else if (errorCode == 0x3C)
                return "ERR_TYPE2_BLK_ENCODING_DATA_NOT_A_MULTIPLE_OF_4";
            else if (errorCode == 0x3D)
                return "ERR_TYPE2_BLK_ENCODING_CANNOT_WRITE_TO_MANUFACTURER_BLK";
            else if (errorCode == 0x3E)
                return "ERR_TYPE2_BLK_ENCODING_BLOCKS_OVERLAP_LOCK_AND_CONFIG_PAGES";
            else if (errorCode == 0x3F)
                return "ERR_TYPE2_BLK_ENCODING_NOT_ALL_BLKS_ENCODED";
            else if (errorCode == 0x40)
                return "ERR_TYPE2_BLK_ENCODING_UNKNOWN_ERROR";
            else if (errorCode == 0x41)
                return "Tag not present";
            else if (errorCode == 0x42)
                return "Transceive error";
            else if (errorCode == 0xFC)
                return "Unknown error";
            else
                return "Invalid error code";
        }
    }
}