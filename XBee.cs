using System.IO.Ports;
using Microsoft.SPOT.Hardware;
using System.Threading;

namespace STLK
{
    public delegate void PortStatusReceived(byte status);
    public delegate void XBeeCallback(ApiFrame frame);

    public class XBee
    {
        public event PortStatusReceived StatusReceived;

        private const int apiIdentifier = 0;
        private const int frameIdentifier = 1;

        private object _syncObject = new object();
        private byte _nextFrameID;

        private readonly SerialPort _uart;
        private readonly ApiFrameBuilder _frameBuilder;
        private ApiFrame[] _frameBuffer;

        private readonly byte[] _txDataBase = new byte[] {
            0x0, // frame type
            0x1, // frame id, set to zero for no reply
            // ID of recipient, or use 0xFFFF for broadcast 
            0x0,
            0x0,
            0x0,
            0x0,
            0x0,
            0x0,
            0x0,
            0x0,
            // 16 bit of recipient or 0xFFFE if unknown
            0xFF,
            0xFE
        };

        public XBee()
        {
            _frameBuffer = new ApiFrame[10];
            _nextFrameID = 1;
            _frameBuilder = new ApiFrameBuilder();

            _uart = new SerialPort("COM1", 9800, Parity.None, 8, StopBits.One);
            _uart.DataReceived += DataReceived;
            _uart.Open();
        }

        public void SetDigitalOutput(XBeeCommand command, bool state)
        {
            byte stateByte;

            if (state)
                stateByte = 0x5;
            else
                stateByte = 0x4;

            SendRemoteAtCommand(command, null, stateByte);
        }

        public void SendRemoteAtCommand(XBeeCommand command, XBeeCallback callback)
        {
            SendRemoteAtCommand(command, callback, 0x0);
        }

        public byte SendRemoteAtCommand(XBeeCommand command, XBeeCallback callback, byte value)
        {
            ApiFrame frame = new ApiFrame();
            byte frameID = GetNextFrameID();

            int txDataLenght;
            if (value == 0x0)
                txDataLenght = 3;
            else
                txDataLenght = 4;

            byte[] txDataContent = new byte[txDataLenght];
            txDataContent[0] = 0x02; // appply changes on remote

            ushort commandUShort = (ushort)command;
            txDataContent[1] = (byte)(commandUShort >> 8);
            txDataContent[2] = (byte)commandUShort;

            if (value != 0x0)
                txDataContent[3] = value;

            byte[] txData = Utility.CombineArrays(_txDataBase, txDataContent);
            txData[0] = (byte)ApiFrameName.RemoteCommandRequest;
            txData[1] = frameID;

            frame.FrameData = txData;
            frame.Callback = callback;

            return SendApiFrame(frame);

        }

        public ApiFrame SendRemoteAtCommandSync(XBeeCommand command)
        {
            AutoResetEvent stopWaitHandle = new AutoResetEvent(false);
            ApiFrame receivedFrame = null;

            SendRemoteAtCommand(command, result =>
            {
                receivedFrame = result;
                stopWaitHandle.Set();
            });
            stopWaitHandle.WaitOne(500, false);

            return receivedFrame;
        }

        public byte SendAtCommand(XBeeCommand command)
        {
            return SendAtCommand(command, null, 0, 0);
        }

        public byte SendAtCommand(XBeeCommand command, XBeeCallback callback)
        {
            return SendAtCommand(command, callback, 0, 0);
        }

        public byte SendAtCommand(XBeeCommand command, XBeeCallback callback, uint value, int size)
        {
            ApiFrame frame = new ApiFrame();
            byte frameID = GetNextFrameID();
            byte[] buffer = new byte[4 + size];

            buffer[apiIdentifier] = (byte)ApiFrameName.ATCommand;
            buffer[frameIdentifier] = frameID;
            ushort commandUShort = (ushort)command;
            buffer[2] = (byte)(commandUShort >> 8);
            buffer[3] = (byte)commandUShort;

            if (size > 0)
            {
                Utility.InsertValueIntoArray(buffer, 4, size, value);
            }
            frame.FrameData = buffer;
            frame.Callback = callback;

            return SendApiFrame(frame);
        }

        public byte SendData(ulong destinationSerialNumber, ushort destinationAddress, byte[] data, XBeeCallback callback)
        {
            ApiFrame frame = new ApiFrame();
            byte[] buffer = new byte[2 + sizeof(ulong) + sizeof(ushort) + 2];
            buffer[apiIdentifier] = (byte)ApiFrameName.ZigBeeTransmitRequest;
            buffer[frameIdentifier] = GetNextFrameID();

            if (destinationAddress == 0)
            {
                // only the coordinator has a destination address of 0, and to get this frame to
                // the coordinator, we need to set the serial number to 0 as well no matter what
                // was passed in.
                destinationSerialNumber = 0;
            }

            Utility.InsertValueIntoArray(buffer, 2, sizeof(uint), (uint)(destinationSerialNumber >> 32));
            Utility.InsertValueIntoArray(buffer, 6, sizeof(uint), (uint)(destinationSerialNumber & 0xFFFFFFFF));
            Utility.InsertValueIntoArray(buffer, 10, sizeof(ushort), destinationAddress);
            Utility.InsertValueIntoArray(buffer, 12, sizeof(ushort), 0);
            frame.FrameData = Utility.CombineArrays(buffer, data);
            frame.Callback = callback;

            return SendApiFrame(frame);
        }

        internal byte SendApiFrame(ApiFrame frame)
        {
            byte frameID = frame.FrameData[frameIdentifier];
            _frameBuffer[frameID] = frame;
            var buffer = frame.Serialize();
            _uart.Write(buffer, 0, frame.Length + 4);
            return frameID;
        }

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_uart.BytesToRead <= 0)
            {
                return;
            }

            byte[] buffer = new byte[1];

            while (_uart.BytesToRead > 0)
            {
                _uart.Read(buffer, 0, 1);
                _frameBuilder.Append(buffer[0]);
                if (_frameBuilder.IsComplete)
                {
                    // got a frame, do something
                    ReceivedApiFrame(_frameBuilder.GetApiFrame());
                    _frameBuilder.Reset();
                }
            }

        }

        private void ReceivedApiFrame(ApiFrame frame)
        {
            switch (frame.FrameData[apiIdentifier])
            {
                case (byte)ApiFrameName.ZigBeeIODataSampleRxIndicator:
                    if (frame.Length == 18)
                    {
                        if (StatusReceived != null)
                            StatusReceived(frame.FrameData[17]);
                    }
                    break;
                case (byte)ApiFrameName.ATCommandResponse:
                    ReceivedATCommandResponse(frame);
                    break;
                case (byte)ApiFrameName.RemoteCommandResponse:
                    ReceivedATCommandResponse(frame);
                    break;
                default:
                    break;
            }

        }

        internal void ReceivedATCommandResponse(ApiFrame frame)
        {
            byte frameID = frame.FrameData[frameIdentifier];
            XBeeCommand command = (XBeeCommand)(frame.FrameData[2] << 8 | frame.FrameData[3]);
            ApiFrame sentFrame = _frameBuffer[frameID];

            if (sentFrame != null && sentFrame.Callback != null)
            {
                sentFrame.Callback(frame);
                if (!(command == XBeeCommand.NodeDiscover && frame.Length > 0))
                {
                    _frameBuffer[frameID] = null;
                }
            }

        }

        private byte GetNextFrameID()
        {
            lock (_syncObject)
            {
                _nextFrameID++;
                if (_nextFrameID > 9)
                {
                    _nextFrameID = 2;
                }
                return _nextFrameID;
            }
        }

    }
}
