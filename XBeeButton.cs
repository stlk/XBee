using System.Threading;

namespace STLK
{
    public delegate void XBeeButtonEventHandler(bool buttonHeld);

    class XBeeButton
    {
        private readonly ushort _portIndex;
        private Timer _pressLenght;

        public event XBeeButtonEventHandler ButtonHandler;

        public XBeeButton(ushort portIndex)
        {
            _portIndex = portIndex;
        }

        public void StatusReceived(byte status)
        {
            bool state = (((status >> _portIndex) & 1) == 1);

            if (state)
            {
                if (_pressLenght != null)
                {
                    _pressLenght.Dispose();
                    _pressLenght = null;

                    if (ButtonHandler != null)
                        ButtonHandler(false); // Button was held for less than 2 seconds
                }
            }
            else
            {
                if (_pressLenght == null)
                    _pressLenght = new Timer(ButtonHeld, null, 2000, 0);
            }
        }

        private void ButtonHeld(object state)
        {
            _pressLenght.Dispose();
            _pressLenght = null;

            if (ButtonHandler != null)
                ButtonHandler(true); // Button was held for 2 seconds
        }
    }
}
