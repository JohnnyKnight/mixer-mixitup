﻿using MixItUp.Base.Services.External;
using MixItUp.Base.Util;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MixItUp.Base.Services
{
    public abstract class PlatformServiceBase
    {
        public async Task<ExternalServiceResult> AttemptConnect(Func<Task<ExternalServiceResult>> connect, int connectionAttempts = 5)
        {
            ExternalServiceResult result = new ExternalServiceResult();
            for (int i = 0; i < connectionAttempts; i++)
            {
                try
                {
                    result = await connect();
                    if (result.Success)
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
                await Task.Delay(1000);
            }
            return result;
        }

        public async Task RunAsync(Task task, bool logNotFoundException = true) { await AsyncRunner.RunAsync(task, logNotFoundException); }

        public async Task<T> RunAsync<T>(Task<T> task, bool logNotFoundException = true) { return await AsyncRunner.RunAsync(task, logNotFoundException); }

        public async Task RunAsync(Func<Task> task) { await AsyncRunner.RunAsync(task); }

        public async Task<T> RunAsync<T>(Func<Task<T>> task) { return await AsyncRunner.RunAsync(task); }

        public async Task<IEnumerable<T>> RunAsync<T>(Task<IEnumerable<T>> task)
        {
            try
            {
                await task;
                return task.Result;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return ReflectionHelper.CreateInstanceOf<List<T>>();
        }

        public async Task<IDictionary<K,V>> RunAsync<K,V>(Task<IDictionary<K, V>> task)
        {
            try
            {
                await task;
                return task.Result;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return ReflectionHelper.CreateInstanceOf<Dictionary<K,V>>();
        }
    }
}
