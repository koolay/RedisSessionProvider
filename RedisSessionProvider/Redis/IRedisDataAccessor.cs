using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using RedisSessionProvider.Serialization;

namespace RedisSessionProvider.Redis
{
    /// <summary>
    /// redis处理接口，须实现线程安全
    /// </summary>
    public interface IRedisDataAccessor
    {
        IRedisSerializer RedisSerializer { get;}

        #region hash access

        Dictionary<string, object> GetHashAllItems(string hashId);

        void SetHash(string hashId, Dictionary<string, object> map, int timeOut = 0);

        void RemoveHashFields(string hashId, params string[] fields);


        #endregion 
        void Remove(string redisId);

        void SetExpire(string key, TimeSpan expireIn);


    }
}
