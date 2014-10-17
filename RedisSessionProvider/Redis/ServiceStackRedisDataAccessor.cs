﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CtripSZ;
using CtripSZ.Redis;
using CtripSZ.Frameworks.Redis;
using RedisSessionProvider.Serialization;
using RedisSessionProvider.Config;

namespace RedisSessionProvider.Redis
{
    public class ServiceStackRedisDataAccessor : IRedisDataAccessor
    {
        private readonly IRedisClientsManager clientManager;
        public ServiceStackRedisDataAccessor(string hostGroupName)
        {
            if (string.IsNullOrEmpty(hostGroupName))
                throw new ArgumentNullException("hostGroupName");

            clientManager = RedisHelper.GetClientManager(hostGroupName);
        }
 
        public void RemoveHashFields(string hashId, params string[] fields)
        {
            if (fields == null || fields.Length < 1)
                return;

            using(var client = this.clientManager.GetClient())
            {
                var nc = client as RedisNativeClient;
                nc.HDel(hashId, fields.Select(p => p.ToUtf8Bytes()).ToArray());
            }
        }

        public void Remove(string redisId)
        {
            using (var client = this.clientManager.GetClient())
            {
                client.Remove(redisId);
            }
        }

        public void SetExpire(string key, TimeSpan expireIn)
        {
            using (var client = this.clientManager.GetClient())
            {
                client.ExpireEntryIn(key, expireIn);
            }
        }

        public IRedisSerializer RedisSerializer
        {
            get
            {
                return RedisSerializationConfig.SessionDataSerializer ?? new BinarySerializer();
            }
            
        }

        public Dictionary<string, object> GetHashAllItems(string hashId)
        {
            using (var client = this.clientManager.GetClient())
            {
                var nc = client as RedisNativeClient;
                var multiDataList = nc.HGetAll(hashId);

                if (multiDataList != null && multiDataList.Length > 0)
                {
                    Dictionary<string, object> result = new Dictionary<string, object>();
                    for (var i = 0; i < multiDataList.Length; i += 2)
                    {
                        var key = multiDataList[i].FromUtf8Bytes();
                        var val = this.RedisSerializer.DeserializeOne(multiDataList[i + 1]);
                        result.Add(key, val);
                    }
                    return result;
                }

                return null;
            }
        }

        public void SetHash(string hashId, Dictionary<string, object> map, int timeOut = 0)
        {
             if (map == null || map.Count < 1)
                return;

            var keyValuePairsList = new List<KeyValuePair<string, byte[]>>();
          
            foreach(var kv in map)
            {
                var pair = new KeyValuePair<string, byte[]>(kv.Key, this.RedisSerializer.SerializeOne(kv.Value));
                keyValuePairsList.Add(pair);
            }

            using (var client = this.clientManager.GetClient())
            {
                var nc = client as RedisNativeClient;
                var keys = new byte[keyValuePairsList.Count][];
                var values = new byte[keyValuePairsList.Count][];

                for (var i = 0; i < keyValuePairsList.Count; i++)
                {
                    var kvp = keyValuePairsList[i];
                    keys[i] = kvp.Key.ToUtf8Bytes();
                    values[i] = kvp.Value;
                }

                nc.HMSet(hashId, keys, values);

            }
        }

      
    }
}