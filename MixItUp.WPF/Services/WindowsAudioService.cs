﻿using MixItUp.Base;
using MixItUp.Base.Services;
using MixItUp.Base.Util;
using NAudio.Wave;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MixItUp.WPF.Services
{
    public class WindowsAudioService : IAudioService
    {
        public Task Play(string filePath, int volume, int deviceNumber = -1)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                if (File.Exists(filePath))
                {
                    Task.Run(async () =>
                    {
                        float floatVolume = MathHelper.Clamp(volume, 0, 100) / 100.0f;

                        if (deviceNumber < 0 && !string.IsNullOrEmpty(ChannelSession.Settings.DefaultAudioOutput))
                        {
                            deviceNumber = await ChannelSession.Services.AudioService.GetOutputDevice(ChannelSession.Settings.DefaultAudioOutput);
                        }

                        using (AudioFileReader audioFileReader = new AudioFileReader(filePath))
                        {
                            audioFileReader.Volume = floatVolume;
                            using (WaveOutEvent outputDevice = (deviceNumber < 0) ? new WaveOutEvent() : new WaveOutEvent() { DeviceNumber = deviceNumber })
                            {
                                outputDevice.Init(audioFileReader);
                                outputDevice.Play();

                                while (outputDevice.PlaybackState == PlaybackState.Playing)
                                {
                                    await Task.Delay(500);
                                }
                            }
                        }
                    });
                }
                else if (filePath.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                {
                    Task.Run(async () =>
                    {
                        float floatVolume = MathHelper.Clamp(volume, 0, 100) / 100.0f;

                        if (deviceNumber < 0 && !string.IsNullOrEmpty(ChannelSession.Settings.DefaultAudioOutput))
                        {
                            deviceNumber = await ChannelSession.Services.AudioService.GetOutputDevice(ChannelSession.Settings.DefaultAudioOutput);
                        }

                        using (var mediaReader = new MediaFoundationReader(filePath))
                        {
                            using (WaveOutEvent outputDevice = (deviceNumber < 0) ? new WaveOutEvent() : new WaveOutEvent() { DeviceNumber = deviceNumber })
                            {
                                outputDevice.Volume = floatVolume;
                                outputDevice.Init(mediaReader);
                                outputDevice.Play();

                                while (outputDevice.PlaybackState == PlaybackState.Playing)
                                {
                                    await Task.Delay(500);
                                }
                            }
                        }
                    });
                }
            }
            return Task.FromResult(0);
        }

        public Task<Dictionary<int, string>> GetOutputDevices()
        {
            Dictionary<int, string> outputDevices = new Dictionary<int, string>();
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                try
                {
                    WaveOutCapabilities capabilities = WaveOut.GetCapabilities(i);
                    outputDevices[i] = capabilities.ProductName;
                }
                catch (Exception ex) { Logger.Log(ex); }
            }
            return Task.FromResult(outputDevices);
        }

        public async Task<int> GetOutputDevice(string deviceName)
        {
            Dictionary<int, string> audioOutputDevices = await ChannelSession.Services.AudioService.GetOutputDevices();
            foreach (var kvp in audioOutputDevices)
            {
                if (kvp.Value.Equals(deviceName))
                {
                    return kvp.Key;
                }
            }
            return -1;
        }
    }
}
