using System.IO.Ports;
using Microsoft.SPOT.Hardware;
using System.Threading;

namespace STLK
{
    public delegate void PortStatusReceived(byte status);

    public struct PortSample
    {
        public byte Sample;
        public bool Result;
    }

    public class XBee
    {
        private AutoResetEvent stopWaitHandle = new AutoResetEvent(false);

        public event PortStatusReceived StatusReceived;

        private readonly SerialPort _uart;
        private readonly ApiFrameBuilder _frameBuilder;
        private byte _lastReadSample;

        private byte[] _txDataBase = new byte[] {
            0x7E,// start byte
            0x0, // high part of length (always 0)
            0x0, // low part of length (the number of bytes
                 // that follow, not including checksum)
            0x0, // frame type
            0x1, // frame id, set to zero for no reply
            // ID of recipient, or use 0xFFFF for broadcast 
            0x0,
            0x0,
            0x0,
            0x0,
            0x0,
            0x0,
            0xFF,
            0xFF,
            // 16 bit of recipient or 0xFFFE if unknown
            0xFF,
            0xFE
        };

        public XBee()
        {
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

            SendRemoteAtCommand(command, stateByte);
        }

        public void SendRemoteAtCommand(XBeeCommand command)
        {
            SendRemoteAtCommand(command, 0x0);
        }

        public void SendRemoteAtCommand(XBeeCommand command, byte value)
        {
            int txDataLenght;
            if (value == 0x0)
                txDataLenght = 4;
            else
                txDataLenght = 5;

            byte[] txDataContent = new byte[txDataLenght];
            txDataContent[0] = 0x02; // appply changes on remote

            ushort commandUShort = (ushort)command;
            txDataContent[1] = (byte)(commandUShort >> 8);
            txDataContent[2] = (byte)commandUShort;

            if (value != 0x0)
                txDataContent[3] = value;

            byte[] txData = Utility.CombineArrays(_txDataBase, txDataContent);
            txData[2] = (byte)(txData.Length - 4);
            txData[3] = (byte)ApiFrameName.RemoteCommandRequest;

            CalculateChecksum(txData);

            _uart.Flush();
            _uart.Write(txData, 0, txData.Length);

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

        private void ReceivedApiFrame(byte[] frame)
        {
            switch (frame[0]) // frame identifier
            {
                case (byte)ApiFrameName.ZigBeeIODataSampleRxIndicator:

                    if (frame.Length == 18)
                    {
                        if (StatusReceived != null)
                            StatusReceived(frame[17]);
                    }
                    break;
                case (byte)ApiFrameName.RemoteCommandResponse:

                    if (frame.Length == 21)
                    {
                        _lastReadSample = frame[20];
                        stopWaitHandle.Set();
                    }
                    break;
                default:
                    break;
            }

            //string res = string.Empty;
            //foreach (byte b in frame)
            //{
            //    res += b.ToString() + " ";
            //}

            //Debug.Print(res);
        }

        public void SendTransmitRequest(byte[] data)
        {
            //txDataContent[0] = 0x0 // broadcast radius
            //txDataContent[1] = 0x0 // options

            byte[] txDataContent = new byte[data.Length + 3];
            for (int i = 0; i < data.Length; i++)
                txDataContent[i + 2] = data[i];

            byte[] txData = Utility.CombineArrays(_txDataBase, txDataContent);
            txData[2] = (byte)(txData.Length - 4);
            txData[3] = (byte)ApiFrameName.ZigBeeTransmitRequest;

            CalculateChecksum(txData);

            _uart.Flush();
            _uart.Write(txData, 0, txData.Length);

        }

        public PortSample GetSample()
        {
            SendRemoteAtCommand(XBeeCommand.ForceSample);
            bool receivedSignal = stopWaitHandle.WaitOne(500, false);

            return new PortSample { Sample = _lastReadSample, Result = receivedSignal };
        }

        /// <summary>
        /// Calculates checksum and inserts it to the last field
        /// </summary>
        /// <param name="data"></param>
        private void CalculateChecksum(byte[] data)
        {
            byte checkSum = 0xFF;
            for (int i = 3; i < data.Length - 1; i++)
            {
                checkSum -= data[i];
            }

            data[data.Length - 1] = checkSum;
        }

    }
}
