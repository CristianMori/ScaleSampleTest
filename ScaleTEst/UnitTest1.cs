using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ScaleTest;

namespace SampleUnitTest
{
    [TestClass]
    public class UnitTest1
    {
        ScaleTest.MetterToledo meter;

        [TestMethod]
        public void CreateScaleObject()
        {
            meter=new MetterToledo();
            Assert.IsNotNull(meter);
        }

        [TestMethod]
        public void SetConfiguration()
        {
            CreateScaleObject();
            Boolean configOk=meter.SetConfiguration("COM3,9600,8,N,1");
            Assert.IsTrue(configOk);
        }

        [TestMethod]
        public void ConnectToSampleDeviceAndDisconnect()
        {
            SetConfiguration();
            bool ConnectionOK = meter.Connect();
            Assert.IsTrue(meter.IsConnected);
            Assert.IsTrue(ConnectionOK);
            meter.Disconnect();
        }

        [TestMethod]
        public void ConnectAndReceiveWeight()
        {
            SetConfiguration();
            meter.ResetStatistics();            
            bool ConnectionOK = meter.Connect();
            Assert.IsTrue(meter.IsConnected);
            Assert.IsTrue(ConnectionOK);
            Double dbl  = meter.GetWeight();
            meter.Disconnect();
        }

    }
}
