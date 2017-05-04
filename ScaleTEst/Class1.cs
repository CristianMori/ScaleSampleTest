using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
namespace ScaleTest
{
    public class MetterToledo
    {
        private SerialPort Serial=new SerialPort();

        private String CurrentConfiguration = String.Empty;


        public bool SetConfiguration(String configuration)
        {
            if (!IsConnected)
            {
                try
                {
                    String[] parts = configuration.Split(',');
                    Serial.PortName = parts[0];
                    Serial.BaudRate = Int32.Parse(parts[1]);
                    Serial.DataBits = Int32.Parse(parts[2]);
                    switch (parts[3])
                    {
                        case "E":
                            Serial.Parity = Parity.Even;
                            break;
                        case "O":
                            Serial.Parity = Parity.Odd;
                            break;
                        case "N":
                            Serial.Parity = Parity.None;
                            break;
                        default:
                            return false;
                    }
                    switch (parts[4])
                    {
                        case "1":
                            Serial.StopBits = StopBits.One;
                            break;
                        case "2":
                            Serial.StopBits = StopBits.Two;
                            break;
                        case "0":
                            Serial.StopBits = StopBits.None;
                            break;
                        default:
                            return false;
                    }
                }
                catch
                {
                    return false;
                }
                CurrentConfiguration = configuration;
                return true;
            }
            else
                return false;
        }

        public bool IsConnected
        {
            get { return Serial.IsOpen; }
        }

        public bool Connect()
        {
            try
            {
                Serial.NewLine = "\r\n";
                Serial.Open();
                Serial.DataReceived += Serial_DataReceived;                
                return true;
            }
            catch
            {
                Disconnect();
                return false;
            }
        }

        private Int32 receivedBytes = 0;
        public Int32 BytesReceived
        {
            get { return receivedBytes; }
        }

        public void ResetStatistics()
        {
            receivedBytes = 0;
        }

        List<byte[]> ReceivedData = new List<byte[]>();
        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            while (Serial.BytesToRead > 0)
            {
                int bytesNumToRead = Serial.BytesToRead;
                byte[] chunk = new byte[bytesNumToRead];
                Serial.Read(chunk, 0, bytesNumToRead);
                ReceivedData.Add(chunk);
                receivedBytes += chunk.Length;
            }
            ProcessReceivedData();
        }

        public Double Weight
        {
            get { return weight; }
        }

        public Boolean IsStable
        {
            get { return isStable; }
        }

        private Double weight = 0.0;
        private Boolean isStable = false;
        private Boolean weightReceived = false;
        public Double GetWeight()
        {
            if (!IsConnected)
                throw new Exception("Not connected to device");
            weightReceived = false;
            Serial.WriteLine(MeterCommands.RequestWeight);
            DateTime requestTime = DateTime.Now;
            while ((DateTime.Now - requestTime).TotalSeconds < 5 && weightReceived == false) 
                System.Threading.Thread.Sleep(0);
            if (weightReceived)
                return Weight;
            else
            {
                throw new Exception("Timeout waiting the weight");
            }
        }

        private void ProcessReceivedData()
        {
            Int32 totalBytesToProcess = 0;
            foreach (byte[] b in ReceivedData)
                totalBytesToProcess += b.Length;
            byte[] TotalData=new byte[totalBytesToProcess];
            Int32 currentPosition = 0;
            foreach (byte[] b in ReceivedData)
            {
                Buffer.BlockCopy(b, 0, TotalData, currentPosition, b.Length);
                currentPosition += b.Length;
            }
            ReceivedData.Clear();

            Int32 startMsgPos = 0;
            for (Int32 ii=0; ii<TotalData.Length; ii++)
            {
                if (TotalData[ii] == '\n')
                {
                    //"S S XXXXXXX UUU"
                    //"S D XXXXXXX UUU"
                    //"Z A"

                    switch (TotalData[startMsgPos])
                    {

                        case (byte)'S':
                            byte[] msg=new byte[ii-startMsgPos+1];
                            Buffer.BlockCopy(TotalData, startMsgPos, msg,0,msg.Length);
                            String str=ASCIIEncoding.ASCII.GetString(msg);
                            String[] parts = str.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            weight = Double.Parse(parts[2]);
                            if (parts[1] == "S")
                                isStable = true;
                            if (parts[1] == "D")
                                isStable = false;
                            weightReceived = true;
                            break;
                        case (byte)'Z':
                            break;
                    }
                    startMsgPos = ii + 1;
                }
            }
            if (startMsgPos < TotalData.Length)
            {
                byte[] leftoverBytes = new byte[TotalData.Length-startMsgPos];
                Buffer.BlockCopy(TotalData, startMsgPos, leftoverBytes,0, leftoverBytes.Length);
                ReceivedData.Add(leftoverBytes);
            }
        }


        public void Disconnect()
        {
            if (IsConnected)
            {
                Serial.Close();
                Serial.DataReceived -= Serial_DataReceived;
            }
        }

        public String GetConfiguration()
        {
            return CurrentConfiguration;
        }


        internal class MeterCommands
        {
            internal static readonly string RequestWeight = "S";
        }
    }

}
