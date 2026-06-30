using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ecs.RedisTool
{
    public interface IRedisClient
    {
        public void Build(string connConfig, int dbNum);
        public string GetStringValue(string key);
        public Task<string> GetStringValueAsync(string key);
        public bool SetStringValue(string key, string value);
        public Task<bool> SetStringValueAsync(string key, string value);
        public string GetHashValue(string key, string field);
        public Task<string> GetHashValueAsync(string key, string field);
        public void SetHashValue(string key, string field, string value);
        public Task SetHashValueAsync(string key, string field, string value);
        public void Subscribe(string key, Action<string, string> handler);
        public Task SubscribeAsync(string key, Action<string, string> handler);
        public bool Publish(string key, string value);
        public Task<bool> PublishAsync(string key, string value);
        public string[] GetHashFields(string key);
        public void RemoveHashFields(string key, string[] fields);
        public Task RemoveHashFieldsAsync(string key, string[] fields);
        public KeyValuePair<string, string>[] GetAllHashFieldValuePairs(string key);
        public Task<KeyValuePair<string, string>[]> GetAllHashFieldValuePairsAsync(string key);
        public bool IsKeyExist(string key);
        public Task<bool> IsKeyExistAsync(string key);
        public bool RemoveKey(string key);
        public Task<bool> RemoveKeyAsync(string key);
    }
}