﻿using ECommon.Components;
using ECommon.Logging;
using ECommon.Utilities;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ENode.Lock.Redis
{
    public class RedLockQueueService : ILockService
    {
        #region Private Variables

        private BlockingCollection<(string lockKey, TaskCompletionSource<bool> tcs, Func<Task> action)> _executingQueue = new BlockingCollection<(string lockKey, TaskCompletionSource<bool> tcs, Func<Task> action)>();
        private TimeSpan _holdDurationTimeSpan = TimeSpan.FromSeconds(30);
        private string _keyPrefix;
        private ILogger _logger;
        private IDatabase _redisDatabase;
        private RedisOptions _redisOptions;
        private RedisProvider _redisProvider;
        private TimeSpan _timeOutTimeSpan = TimeSpan.FromSeconds(300);

        #endregion Private Variables

        #region Ctor

        public RedLockQueueService()
        {
            Task.Factory.StartNew(ProcessWorkAsync);
        }

        #endregion Ctor

        #region Public Methods

        public void AddLockKey(string lockKey)
        {
            throw new NotImplementedException("There is no need to add lock key when use redis lock service.");
        }

        public void ExecuteInLock(string lockKey, Action action)
        {
            var tcs = new TaskCompletionSource<bool>();

            _executingQueue.Add((
                lockKey,
                tcs,
                () =>
                {
                    action();
                    return Task.CompletedTask;
                }
            ));

            tcs.Task.Wait();
        }

        public async Task ExecuteInLockAsync(string lockKey, Action action)
        {
            var tcs = new TaskCompletionSource<bool>();

            _executingQueue.Add((
                lockKey,
                tcs,
                () =>
                {
                    action();
                    return Task.CompletedTask;
                }
            ));

            await tcs.Task;
        }

        public async Task ExecuteInLockAsync(string lockKey, Func<Task> action)
        {
            var tcs = new TaskCompletionSource<bool>();

            _executingQueue.Add((
                lockKey,
                tcs,
                async () =>
                {
                    await action();
                }
            ));

            await tcs.Task;
        }

        public async Task ExecuteInLockAsync(string lockKey, Action<object> action, object state)
        {
            var tcs = new TaskCompletionSource<bool>();

            _executingQueue.Add((
                lockKey,
                tcs,
                () =>
                {
                    action(state);
                    return Task.CompletedTask;
                }
            ));

            await tcs.Task;
        }

        public async Task ExecuteInLockAsync(string lockKey, Func<object, Task<object>> action, object state)
        {
            var tcs = new TaskCompletionSource<bool>();

            _executingQueue.Add((
                lockKey,
                tcs,
                async () =>
                {
                    await action(state);
                }
            ));

            await tcs.Task;
        }

        public RedLockQueueService Initialize(
            RedisOptions redisOptions,
            string keyPrefix = "default",
            TimeSpan? timeOut = null,
            TimeSpan? holdDuration = null
            )
        {
            _redisOptions = redisOptions;
            _keyPrefix = keyPrefix;

            Ensure.NotNull(_redisOptions, "redisOptions");
            Ensure.NotNull(_redisOptions.ConnectionString, "redisOptions.ConnectionString");
            Ensure.Positive(_redisOptions.DatabaseId, "redisOptions.DatabaseId");
            Ensure.NotNull(_keyPrefix, "keyPrefix");

            if (timeOut.HasValue)
            {
                _timeOutTimeSpan = timeOut.Value;
            }

            if (holdDuration.HasValue)
            {
                _holdDurationTimeSpan = holdDuration.Value;
            }

            _logger = ObjectContainer.Resolve<ILoggerFactory>().Create(GetType().FullName);

            _redisProvider = new RedisProvider(_redisOptions);

            _redisDatabase = _redisProvider.GetDatabase();

            return this;
        }

        #endregion Public Methods

        #region Private Methods

        private RedisKey GetRedisKey(string key)
        {
            return $"enode:lock:{_keyPrefix}:{key}";
        }

        private async Task ProcessWorkAsync()
        {
            foreach (var item in _executingQueue.GetConsumingEnumerable())
            {
                var redisLock = await RedLock.AcquireAsync(_redisDatabase, GetRedisKey(item.lockKey), _timeOutTimeSpan, _holdDurationTimeSpan);
                try
                {
                    await item.action();
                }
                finally
                {
                    await redisLock.DisposeAsync();
                }
                item.tcs.SetResult(true);
            }
        }

        #endregion Private Methods
    }
}