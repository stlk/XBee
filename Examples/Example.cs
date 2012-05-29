using System;
using Microsoft.SPOT;

namespace STLK
{
    class Example
    {
        private static XBee _xBee;
        private static XBeeButton _button;

        public void Initialize()
        {
            _button = new XBeeButton(1);
            _button.ButtonHandler += _button_ButtonHandler;

            _xBee = new XBee();
            _xBee.StatusReceived += _button.StatusReceived;
        }

        /// <summary>
        /// Example of synchronous command with 500ms timeout
        /// </summary>
        /// <returns></returns>
        public int GetRssiOfLastReceivedFrame()
        {
            ApiFrame rssiFrame = _xBee.SendRemoteAtCommandSync(0x0013A2004086DA07, XBeeCommand.ReceivedSignalStrength);

            if (rssiFrame != null && rssiFrame.FrameData.Length == 16)
            {
                return rssiFrame.FrameData[15];
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Example of asynchronous command
        /// </summary>
        public void GetRssiOfLastReceivedFrameAsync()
        {
            _xBee.SendRemoteAtCommand(0x0013A2004086DA07, XBeeCommand.ReceivedSignalStrength, (frame) =>
            {
                if (frame.FrameData.Length != 16)
                {
                    return;
                }

                int rssi = frame.FrameData[15];
                // do something with received value

            });
        }

        /// <summary>
        /// Example of Button class
        /// </summary>
        /// <param name="buttonHeld"></param>
        /// <param name="ports"></param>
        void _button_ButtonHandler(bool buttonHeld, byte ports)
        {
            if (buttonHeld)
            {

            }
            else
            {
                // read state of output port and negate it
                _xBee.SetDigitalOutput(0x0013A2004086DA07, XBeeCommand.ADio2Configuration, (((ports >> 2) & 1) == 0));
            }
        }
    }
}
