using System.IO.Ports;
using Microsoft.SPOT.Hardware;
using System.Threading;

namespace STLK
{
    public delegate void PortStatusReceived(byte status);
    public delegate void FrameReceivedEventHandler(byte[] data);
    public delegate void AnalogStatusReceived(int[] readings);
    public delegate void XBeeCallback(ApiFrame frame);

    public class XBee
    {
        public event PortStatusReceived StatusReceived;
        public event AnalogStatusReceived AnalogStatusReceived;
        public event FrameReceivedEventHandler FrameReceived;

        private const int ApiIdentifier = 0;
        private const int FrameIdentifier = 1;

        private object _syncObject = new object();
        private byte _nextFrameId;

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
            _nextFrameId = 1;
            _frameBuilder = new ApiFrameBuilder();

            _uart = new SerialPort("COM1", 9800, Parity.None, 8, StopBits.One);
            _uart.DataReceived += DataReceived;
            _uart.Open();
        }

        public void SetDigitalOutput(ulong destinationSerialNumber, XBeeCommand command, bool state)
        {
            byte stateByte;

            if (state)
                stateByte = 0x5;
            else
                stateByte = 0x4;

            SendRemoteAtCommand(destinationSerialNumber, command, null, stateByte);
        }

        public void SendRemoteAtCommand(ulong destinationSerialNumber, XBeeCommand command, XBeeCallback callback)
        {
            SendRemoteAtCommand(destinationSerialNumber, command, callback, 0x0);
        }

        public byte SendRemoteAtCommand(ulong destinationSerialNumber, XBeeCommand command, XBeeCallback callback, byte value)
        {
            ApiFrame frame = new ApiFrame();
            byte frameId = GetNextFrameId();

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
            txData[1] = frameId;

            for (int i = 0; i < 8; i++)
            {
                txData[9 - i] = (byte)(destinationSerialNumber >> (8 * i));
            }

            frame.FrameData = txData;
            frame.Callback = callback;

            return SendApiFrame(frame);

        }

        public ApiFrame SendRemoteAtCommandSync(ulong destinationSerialNumber, XBeeCommand command)
        {
            AutoResetEvent stopWaitHandle = new AutoResetEvent(false);
            ApiFrame receivedFrame = null;

            SendRemoteAtCommand(destinationSerialNumber, command, result =>
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
            byte frameId = GetNextFrameId();
            byte[] buffer = new byte[4 + size];

            buffer[ApiIdentifier] = (byte)ApiFrameName.ATCommand;
            buffer[FrameIdentifier] = frameId;
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
            byte frameId = GetNextFrameId();

            byte[] txDataContent = new byte[2];
            txDataContent[0] = 0x0;
            txDataContent[1] = 0x0;
            txDataContent = Utility.CombineArrays(txDataContent, data);

            byte[] txData = Utility.CombineArrays(_txDataBase, txDataContent);
            txData[0] = (byte)ApiFrameName.ZigBeeTransmitRequest;
            txData[1] = frameId;

            for (int i = 0; i < 8; i++)
            {
                txData[9 - i] = (byte)(destinationSerialNumber >> (8 * i));
            }

            frame.FrameData = txData;
            frame.Callback = callback;

            return SendApiFrame(frame);
        }

        internal byte SendApiFrame(ApiFrame frame)
        {
            byte frameId = frame.FrameData[FrameIdentifier];
            _frameBuffer[frameId] = frame;
            var buffer = frame.Serialize();
            _uart.Write(buffer, 0, frame.Length + 4);
            return frameId;
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
            switch (frame.FrameData[ApiIdentifier])
            {
                case (byte)ApiFrameName.ZigBeeIODataSampleRxIndicator:
                    if (frame.Length >= 18)
                    {
                        int analogSampleIndex = 16;
                        int digitalChannelMask = (frame.FrameData[13] << 8) | frame.FrameData[14];
                        if (digitalChannelMask > 0)
                        {
                            if (StatusReceived != null)
                            {
                                StatusReceived(frame.FrameData[17]);
                            }
                            analogSampleIndex = 18;
                        }

                        if (AnalogStatusReceived != null)
                        {
                            int[] analogSamples = new int[4];
                            int analogChannelMask = frame.FrameData[15];
                            for (int i = 0; i < 4; i++)
                            {
                                if ((analogChannelMask >> i) == 1)
                                {
                                    analogSamples[i] = (frame.FrameData[analogSampleIndex] << 8) | frame.FrameData[analogSampleIndex + 1];
                                    analogSampleIndex += 2;
                                }
                                else
                                {
                                    analogSamples[i] = -1;
                                }
                            }
                            AnalogStatusReceived(analogSamples);
                        }
                    }
                    break;
                case (byte)ApiFrameName.ATCommandResponse:
                    ReceivedAtCommandResponse(frame);
                    break;
                case (byte)ApiFrameName.RemoteCommandResponse:
                    ReceivedAtCommandResponse(frame);
                    break;
                case (byte)ApiFrameName.ZigBeeReceivePacket:
                    ReceivedZigBeePacket(frame);
                    break;
            }

        }

        internal void ReceivedAtCommandResponse(ApiFrame frame)
        {
            byte frameID = frame.FrameData[FrameIdentifier];
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

        internal void ReceivedZigBeePacket(ApiFrame frame)
        {
            byte[] data = Utility.ExtractRangeFromArray(frame.FrameData, 12, frame.Length - 12);

            if (FrameReceived != null)
                FrameReceived(data);
        }

        private byte GetNextFrameId()
        {
            lock (_syncObject)
            {
                _nextFrameId++;
                if (_nextFrameId > 9)
                {
                    _nextFrameId = 0;
                }
                return _nextFrameId;
            }
        }

    }
}
