using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Ecs.RedisTool
{
    public class RedisClientByStaEx : IRedisClient
    {
        private ConnectionMultiplexer mRedisClient;
        private int mDbNum;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connConfig">连接配置，如 127.0.0.1:6379</param>
        /// <param name="dbNum">操作的数据库编号</param>
        public void Build(string connConfig, int dbNum)
        {
            try
            {
                mDbNum = dbNum;
                mRedisClient = ConnectionMultiplexer.Connect(connConfig);
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }

        public RedisClientByStaEx()
        {

        }

        public string GetHashValue(string key, string field)
        {
            try
            {
                if (mRedisClient == null)
                    throw new Exception("Redis客户端为空");

                IDatabase db = mRedisClient.GetDatabase(mDbNum);
                return db.HashGet(key, field);
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }

        public async Task<string> GetHashValueAsync(string key, string field)
        {
            try
            {
                if (mRedisClient == null)
                    throw new Exception("Redis客户端为空");

                IDatabase db = mRedisClient.GetDatabase(mDbNum);
                return await db.HashGetAsync(key, field).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }

        public string GetStringValue(string key)
        {
            try
            {
                if (mRedisClient == null)
                    throw new Exception("Redis客户端为空");

                IDatabase db = mRedisClient.GetDatabase(mDbNum);
                return db.StringGet(key);
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }

        public async Task<string> GetStringValueAsync(string key)
        {
            try
            {
                if (mRedisClient == null)
                    throw new Exception("Redis客户端为空");

                IDatabase db = mRedisClient.GetDatabase(mDbNum);
                return await db.StringGetAsync(key).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }

        public bool Publish(string key, string value)
        {
            try
            {
                if (mRedisClient == null)
                    throw new Exception("Redis客户端为空");

                IDatabase db = mRedisClient.GetDatabase(mDbNum);
                return db.Publish(RedisChannel.Literal(key), value) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }

        public async Task<bool> PublishAsync(string key, string value)
        {
            try
            {
                if (mRedisClient == null)
                    throw new Exception("Redis客户端为空");

                IDatabase db = mRedisClient.GetDatabase(mDbNum);
                return await db.PublishAsync(RedisChannel.Literal(key), value).ConfigureAwait(false) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }

        public void SetHashValue(string key, string field, string value)
        {
            try
            {
                if (mRedisClient == null)
                    throw new Exception("Redis客户端为空");

                IDatabase db = mRedisClient.GetDatabase(mDbNum);
                db.HashSet(key, field, value);
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }

        public async Task SetHashValueAsync(string key, string field, string value)
        {
            try
            {
                if (mRedisClient == null)
                    throw new Exception("Redis客户端为空");

                IDatabase db = mRedisClient.GetDatabase(mDbNum);
                await db.HashSetAsync(key, field, value).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }

        public bool SetStringValue(string key, string value)
        {
            try
            {
                if (mRedisClient == null)
                    throw new Exception("Redis客户端为空");

                IDatabase db = mRedisClient.GetDatabase(mDbNum);
                return db.StringSet(key, value);
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }

        public async Task<bool> SetStringValueAsync(string key, string value)
        {
            try
            {
                if (mRedisClient == null)
                    throw new Exception("Redis客户端为空");

                IDatabase db = mRedisClient.GetDatabase(mDbNum);
                return await db.StringSetAsync(key, value).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }

        public void Subscribe(string key, Action<string, string> handler)
        {
            try
            {
                if (mRedisClient == null)
                    throw new Exception("Redis客户端为空");

                ISubscriber subscriber = mRedisClient.GetSubscriber();
                subscriber.Subscribe(RedisChannel.Literal(key), (channel, value) =>
                {
                    handler.Invoke(channel.ToString(), value.ToString());
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }

        public async Task SubscribeAsync(string key, Action<string, string> handler)
        {
            try
            {
                if (mRedisClient == null)
                    throw new Exception("Redis客户端为空");

                ISubscriber subscriber = mRedisClient.GetSubscriber();
                await subscriber.SubscribeAsync(RedisChannel.Literal(key), (channel, value) =>
                {
                    handler.Invoke(channel.ToString(), value.ToString());
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }

        public string[] GetHashFields(string key)
        {
            try
            {
                if (mRedisClient == null)
                    throw new Exception("Redis客户端为空");

                IDatabase db = mRedisClient.GetDatabase(mDbNum);
                RedisValue[] fields = db.HashKeys(key);
                if (fields.Length == 0)
                    return new string[0];

                string[] result = new string[fields.Length];
                for (int i = 0; i < fields.Length; i++)
                {
                    result[i] = fields[i];
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }

        public void RemoveHashFields(string key, string[] fields)
        {
            try
            {
                if (mRedisClient == null)
                    throw new Exception("Redis客户端为空");

                if (fields.Length == 0)
                    return;

                RedisValue[] redisValues = new RedisValue[fields.Length];
                for (int i = 0; i < redisValues.Length; i++)
                {
                    redisValues[i] = fields[i];
                }

                IDatabase db = mRedisClient.GetDatabase(mDbNum);
                db.HashDelete(key, redisValues);
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }

        public async Task RemoveHashFieldsAsync(string key, string[] fields)
        {
            try
            {
                if (mRedisClient == null)
                    throw new Exception("Redis客户端为空");

                if (fields.Length == 0)
                    return;

                RedisValue[] redisValues = new RedisValue[fields.Length];
                for (int i = 0; i < redisValues.Length; i++)
                {
                    redisValues[i] = fields[i];
                }

                IDatabase db = mRedisClient.GetDatabase(mDbNum);
                await db.HashDeleteAsync(key, redisValues);
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }

        public KeyValuePair<string, string>[] GetAllHashFieldValuePairs(string key)
        {
            try
            {
                if (mRedisClient == null)
                    throw new Exception("Redis客户端为空");

                IDatabase db = mRedisClient.GetDatabase(mDbNum);
                HashEntry[] fields = db.HashGetAll(key);
                if (fields.Length == 0)
                    return new KeyValuePair<string, string>[0];

                KeyValuePair<string, string>[] result = new KeyValuePair<string, string>[fields.Length];
                for (int i = 0; i < fields.Length; i++)
                {
                    KeyValuePair<string, string> kvp = new KeyValuePair<string, string>(fields[i].Name, fields[i].Value);
                    result[i] = kvp;
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }

        public async Task<KeyValuePair<string, string>[]> GetAllHashFieldValuePairsAsync(string key)
        {
            try
            {
                if (mRedisClient == null)
                    throw new Exception("Redis客户端为空");

                IDatabase db = mRedisClient.GetDatabase(mDbNum);
                HashEntry[] fields = await db.HashGetAllAsync(key).ConfigureAwait(false);
                if (fields.Length == 0)
                    return new KeyValuePair<string, string>[0];

                KeyValuePair<string, string>[] result = new KeyValuePair<string, string>[fields.Length];
                for (int i = 0; i < fields.Length; i++)
                {
                    KeyValuePair<string, string> kvp = new KeyValuePair<string, string>(fields[i].Name, fields[i].Value);
                    result[i] = kvp;
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }

        public bool IsKeyExist(string key)
        {
            try
            {
                if (mRedisClient == null)
                    throw new Exception("Redis客户端为空");

                IDatabase db = mRedisClient.GetDatabase(mDbNum);
                return db.KeyExists(key);
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }

        public async Task<bool> IsKeyExistAsync(string key)
        {
            try
            {
                if (mRedisClient == null)
                    throw new Exception("Redis客户端为空");

                IDatabase db = mRedisClient.GetDatabase(mDbNum);
                return await db.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }

        public bool RemoveKey(string key)
        {
            try
            {
                if (mRedisClient == null)
                    throw new Exception("Redis客户端为空");

                IDatabase db = mRedisClient.GetDatabase(mDbNum);
                return db.KeyDelete(key);
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }

        public async Task<bool> RemoveKeyAsync(string key)
        {
            try
            {
                if (mRedisClient == null)
                    throw new Exception("Redis客户端为空");

                IDatabase db = mRedisClient.GetDatabase(mDbNum);
                return await db.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                throw new Exception($"{GetType().FullName}: {ex.Message}");
            }
        }
    }
}
