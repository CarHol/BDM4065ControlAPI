using System;
using System.ComponentModel.Design;
using System.IO.Ports;
using System.Threading;

namespace PhilipsSerial
{
    // Monitor parent class
    public class Monitor
    {
        private byte[] msgHeader = new byte[] { 0xA6, 0x01, 0x00, 0x00, 0x00 };

        private SerialPort comPort;

        private PowerState powerState = PowerState.Off;
        private InputSourceNumber currentSource = InputSourceNumber.VGA;
        private byte screenBrightness = 0;
        private byte screenContrast = 0;
        private byte screenSharpness = 0;
        private PictureFormat pictureFormat;
        private byte volume = 0;
        private string serialNumber;
        private byte temperature = 0;

        public Monitor(SerialPort port)
        {

            this.comPort = port;

            comPort.BaudRate = 9600;
            comPort.DataBits = 8;
            comPort.Parity = Parity.None;
            comPort.StopBits = StopBits.One;
            comPort.Handshake = Handshake.None;
            comPort.ReadTimeout = 100;

            GetSerialNumber();
            //Console.WriteLine("Serial number: " + this.serialNumber);

        }

        private enum MessageSet : byte
        {
            SerialCodeGet = 0x15,
            PowerStateSet = 0x18,
            PowerStateGet = 0x19,
            TemperatureSensorGet = 0x2F,
            VideoParametersSet = 0x32,
            VideoParametersGet = 0x33,
            PictureFormatSet = 0x3A,
            PictureFormatGet = 0x3B,
            PictureInPictureSet = 0x3C,
            VolumeSet = 0x44,
            VolumeGet = 0x45,
            PictureInPictureSourceGet = 0x85,
            InputSourceSet = 0xAC,
            CurrentSourceGet = 0xAD,
        }

        private enum PowerState : byte
        {
            Off = 0x01,
            On = 0x02,
        }

        private enum PIP_Position : byte
        {
            BottomLeft = 0x00,
            TopLeft = 0x01,
            TopRight = 0x02,
            BottomRight = 0x03
        }

        private enum InputSourceType : byte
        {
            Video = 0x01,
            SVideo = 0x02,
            DVDHD = 0x03,
            RGBHV = 0x04,
            VGA = 0x05,
            HDMI = 0x06,
            DVI = 0x07,
            CardOPS = 0x08,
            DisplayPort = 0x09
        }

        private enum InputSourceNumber : byte
        {
            VGA = 0x00,
            DVI = 0x01,
            HDMI = 0x02,
            MHLHDMI2 = 0x03,
            DP = 0x04,
            miniDP = 0x05,
        }

        private enum PictureFormat
        {
            FULL = 0x00,
            NORMAL = 0x01,
            DYNAMIC = 0x02,
            CUSTOM = 0x03,
            REAL = 0x04,
        }

        private int SendMessage(byte[] msgData, ref byte[] msgReport)
        {
            msgReport = null;

            byte[] msg = new byte[this.msgHeader.Length + msgData.Length];

            System.Buffer.BlockCopy(this.msgHeader, 0, msg, 0, this.msgHeader.Length);

            System.Buffer.BlockCopy(msgData, 0, msg, this.msgHeader.Length, msgData.Length);

            msg[msg.Length - 1] = this.CheckSum(msg);

            try
            {
                this.comPort.Open();

                this.comPort.Write(msg, 0, msg.Length);

                Thread.Sleep(200);

                if (this.comPort.BytesToRead > 0)
                {
                    byte[] responseMsg = new byte[this.comPort.BytesToRead];

                    this.comPort.Read(responseMsg, 0, this.comPort.BytesToRead);

                    if (this.CheckSum(responseMsg) == responseMsg[responseMsg.Length - 1])
                    {
                        msgReport = new byte[responseMsg[4] - 2];

                        System.Buffer.BlockCopy(responseMsg, 6, msgReport, 0, responseMsg[4] - 2);

                        return 0;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else
                {
                    return 2;
                }
            }
            finally
            {
                this.comPort.Close();
            }
        }
        
        // Sets input to HDMI
        public void setInputHDMI()
        {
            SetInputSource(InputSourceType.HDMI, InputSourceNumber.HDMI);
        }
        
        // Sets input to MHL
        public void setInputMHL()
        {
            SetInputSource(InputSourceType.HDMI, InputSourceNumber.MHLHDMI2);
        }
        
        // Returns serial number
        public string getSerialNumber()
        {
            int value = GetSerialNumber();
            return serialNumber;
        }
        
        // Sets input to VGA
        public void setInputVGA()
        {
            SetInputSource(InputSourceType.VGA, InputSourceNumber.VGA);
        }
        
        // Sets input to Displayport
        public void setinputDP()
        {
            SetInputSource(InputSourceType.DisplayPort, InputSourceNumber.DP);
        }

        // Sets input to Mini Displayport
        public void setInputMiniDP()
        {
            SetInputSource(InputSourceType.DisplayPort, InputSourceNumber.miniDP);
        }

        public bool isTurnedOn()
        {
            return powerState == PowerState.On;
        }

        // Toggles monitor sleep
        public void togglePower()
        {
            GetPowerState();
            PowerState stateToSet = powerState == PowerState.On ? PowerState.Off : PowerState.On;
            SetPowerState(stateToSet);
        }

        // Turns the monitor off
        public void setPowerOff()
        {
            SetPowerState(PowerState.Off);
        }
        
        // Turns the monitor on
        public void setPowerOn()
        {
            SetPowerState(PowerState.On);
        }

        // Sets the volume
        // TODO: Why doesn't this work?
        public void setVolume(int value)
        {
            SetVolume((byte) value);
        }

        // Gets current input as string
        // TODO: Currently returns VGA whe input is DP, why?
        public string getCurrentInput()
        {
            int value = GetCurrentSource();
            return currentSource.ToString();
        }
        
        
        private byte CheckSum(byte[] msg)
        {
            byte hashValue = 0;

            for (int i = 0; i < msg.Length - 1; i++)
            {
                hashValue ^= msg[i];
            }

            return hashValue;
        }

        private int SetInputSource(InputSourceType sourceType, InputSourceNumber sourceNumber)
        {
            byte[] msgData = new byte[] 
            { 
                0x07, 
                0x01, 
                (byte)MessageSet.InputSourceSet, 
                (byte)sourceType, 
                (byte)sourceNumber, 
                0x01, 
                0x00, 
                0x00
            };

            byte[] responseData = null;

            if (this.SendMessage(msgData, ref responseData) == 0)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        private int GetCurrentSource()
        {
            byte[] msgData = new byte[] 
            { 
                0x03, 
                0x01, 
                (byte)MessageSet.CurrentSourceGet, 
                0x00
            };

            byte[] msgReport = null;

            if (this.SendMessage(msgData, ref msgReport) == 0)
            {
                this.currentSource = (InputSourceNumber)msgReport[2];

                return 0;
            }
            else
            {
                return 1;
            }
        }

        private int GetSerialNumber()
        {
            byte[] msgData = new byte[] 
            { 
                0x03, 
                0x01, 
                (byte)MessageSet.SerialCodeGet, 
                0x00
            };

            byte[] msgReport = null;

            if (this.SendMessage(msgData, ref msgReport) == 0)
            {
                this.serialNumber = System.Text.Encoding.UTF8.GetString(msgReport, 1, 14);

                return 0;
            }
            else
            {
                return 1;
            }
        }

        private int GetVolume()
        {
            byte[] msgData = new byte[] 
            { 
                0x03, 
                0x01, 
                (byte)MessageSet.VolumeGet, 
                0x00
            };

            byte[] msgReport = null;

            if (this.SendMessage(msgData, ref msgReport) == 0)
            {
                this.volume = msgReport[1];

                return 0;
            }
            else
            {
                return 1;
            }
        }

        private int GetPowerState()
        {
            byte[] msgData = new byte[] 
            { 
                0x03, 
                0x01, 
                (byte)MessageSet.PowerStateGet, 
                0x00
            };

            byte[] msgReport = null;

            if (this.SendMessage(msgData, ref msgReport) == 0)
            {
                this.powerState = (PowerState)msgReport[1];

                return 0;
            }
            else
            {
                return 1;
            }
        }

        private int GetVideoParameters()
        {
            byte[] msgData = new byte[] 
            { 
                0x03, 
                0x01, 
                (byte)MessageSet.VideoParametersGet, 
                0x00
            };

            byte[] msgReport = null;

            if (this.SendMessage(msgData, ref msgReport) == 0)
            {
                if (msgReport[0] == (byte)MessageSet.VideoParametersGet)
                {
                    this.screenBrightness = msgReport[1];
                    this.screenContrast = msgReport[3];
                    this.screenSharpness = msgReport[5];

                    return 0;
                }
                else
                {
                    return 2;
                }
            }
            else
            {
                return 1;
            }
        }

        private int GetPictureFormat()
        {
            byte[] msgData = new byte[] 
            { 
                0x03, 
                0x01, 
                (byte)MessageSet.PictureFormatGet, 
                0x00
            };

            byte[] msgReport = null;

            if (this.SendMessage(msgData, ref msgReport) == 0)
            {
                if (msgReport[0] == (byte)MessageSet.PictureFormatSet)
                {
                    this.pictureFormat = (PictureFormat)(msgReport[1] & 0x03);

                    return 0;
                }
                else
                {
                    return 2;
                }
            }
            else
            {
                return 1;
            }
        }

        private int GetTemperatureSensor()
        {
            byte[] msgData = new byte[] 
            { 
                0x03, 
                0x01, 
                (byte)MessageSet.TemperatureSensorGet, 
                0x00
            };

            byte[] msgReport = null;

            if (this.SendMessage(msgData, ref msgReport) == 0)
            {
                if (msgReport[0] == (byte)MessageSet.TemperatureSensorGet)
                {
                    this.temperature = msgReport[1];

                    return 0;
                }
                else
                {
                    return 2;
                }
            }
            else
            {
                return 1;
            }
        }

        private InputSourceNumber GetPiPSource()
        {
            byte[] msgData = new byte[] 
            { 
                0x03, 
                0x01, 
                (byte)MessageSet.PictureInPictureSourceGet, 
                0x00
            };

            byte[] msgReport = null;

            if (this.SendMessage(msgData, ref msgReport) == 0)
            {
                if (msgReport[0] == (byte)MessageSet.PictureInPictureSourceGet)
                {
                    return (InputSourceNumber)msgReport[2];
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        private int SetPowerState(PowerState powerState)
        {
            byte[] msgData = new byte[] 
            { 
                0x04, 
                0x01, 
                (byte)MessageSet.PowerStateSet, 
                (byte)powerState, 
                0x00
            };

            byte[] responseData = null;

            if (this.SendMessage(msgData, ref responseData) == 0)
            {
                Thread.Sleep(250);

                return 0;
            }
            else
            {
                return 1;
            }
        }

        private int SetPictureFormat(PictureFormat pictureFormat)
        {
            byte[] msgData = new byte[] 
            { 
                0x04, 
                0x01, 
                (byte)MessageSet.PictureFormatSet, 
                (byte)pictureFormat, 
                0x00
            };

            byte[] responseData = null;

            if (this.SendMessage(msgData, ref responseData) == 0)
            {
                Thread.Sleep(250);

                return 0;
            }
            else
            {
                return 1;
            }
        }

        private int SetVolume(byte volume)
        {
            byte[] msgData = new byte[] 
            { 
                0x04, 
                0x01, 
                (byte)MessageSet.VolumeSet, 
                volume, 
                0x00
            };

            byte[] responseData = null;

            if (this.SendMessage(msgData, ref responseData) == 0)
            {
                Thread.Sleep(250);

                return 0;
            }
            else
            {
                return 1;
            }
        }
    }
}