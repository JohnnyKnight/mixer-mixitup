﻿using MixItUp.Base.Model.Serial;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;

namespace MixItUp.Base.Services.External
{
    public interface ISerialService
    {
        Task<IEnumerable<string>> GetCurrentPortNames();

        Task SendMessage(SerialDeviceModel serialDevice, string message);
    }

    public class SerialService : ISerialService
    {
        public Task<IEnumerable<string>> GetCurrentPortNames()
        {
            return Task.FromResult<IEnumerable<string>>(SerialPort.GetPortNames().Distinct().OrderBy(x => x).ToList());
        }

        public async Task SendMessage(SerialDeviceModel serialDevice, string message)
        {
            await Task.Run(() =>
            {
                SerialPort myPort = new SerialPort(serialDevice.PortName, serialDevice.BaudRate);

                if (myPort.IsOpen == false)
                {
                    myPort.DtrEnable = serialDevice.DTREnabled;
                    myPort.RtsEnable = serialDevice.RTSEnabled;
                    myPort.Open();
                }

                myPort.WriteLine(message);

                myPort.Close();
            });
        }
    }
}
