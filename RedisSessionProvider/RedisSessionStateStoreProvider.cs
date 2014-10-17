﻿namespace RedisSessionProvider
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using System.IO;
    using System.Web.Configuration;
    using System.Web.Hosting;
    using System.Web.SessionState;
    using System.Web.UI;

    using RedisSessionProvider.Config;
    using RedisSessionProvider.Redis;
    using RedisSessionProvider.Serialization; 
    
    /// <summary>
    /// This class is the main entry point into RedisSessionProvider for your application. To enable
    ///     it, change your SessionState web.config element to specify this class as the provider
    ///     of the SessionState. See the ReadMe file for details on how to configure the element.
    /// </summary>
    public class RedisSessionStateStoreProvider : SessionStateStoreProviderBase
    {
        /// <summary>
        /// Gets or sets the time an entry lives in Redis. Expiration times for keys are set at the
        ///     beginning and end of each web request (in GetItemExclusive and 
        ///     SetAndReleaseItemExclusive). The value of this variable will always correspond to
        ///     the timeout property of the SessionState configuration element in web.config
        /// </summary>
        protected virtual TimeSpan SessionTimeout { get; set; }

        private static IRedisDataAccessor RedisDataAccessor;
         

        /// <summary>
        /// The serializer from the RedisSerializationConfig to use
        /// </summary>
        private IRedisSerializer cereal;

        /// <summary>
        /// Initializes the RedisSessionStateStoreProvider, reading in settings from the web.config
        ///     SessionState element
        /// </summary>
        /// <param name="name">The name of the SessionStateStoreProvider, defaults to 
        ///     RedisSessionStateStore</param>
        /// <param name="config">A collection of metadata about the provider, can be empty</param>
        public override void Initialize(string name, NameValueCollection config)
        {
            // config will contain attribute values from the provider tag in the web.config
            if (string.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "Redis Session State Store Provider");
            }

            if (string.IsNullOrEmpty(name))
            {
                name = "RedisSessionProvider";
            }

            if(RedisSerializationConfig.RedisDataAccessor==null)
            {
                throw new NullReferenceException("RedisSerializationConfig.RedisHashAccessor is null");
            }

            // the base Provider class is a .NET pattern that allows for configurable persistence layers or "providers".
            //      might as well initialize the base with the description attribute and application name
            base.Initialize(name, config);

            // Get <sessionState> configuration element.
            System.Configuration.Configuration webCfg = WebConfigurationManager.OpenWebConfiguration(
                HostingEnvironment.ApplicationVirtualPath);
            SessionStateSection sessCfg = (SessionStateSection)webCfg.GetSection("system.web/sessionState");
            
            this.SessionTimeout = sessCfg.Timeout;
            
            this.cereal = RedisSerializationConfig.SessionDataSerializer;
           
            RedisDataAccessor = RedisSerializationConfig.RedisDataAccessor;
        }

        /// <summary>
        /// Creates a new, empty Session for first-time requests
        /// </summary>
        /// <param name="context">The HttpContext containing the current web request</param>
        /// <param name="timeout">The timeout of the Session to be created</param>
        /// <returns>A SessionStateStoreData object containing Session keys and properties</returns>
        public override SessionStateStoreData CreateNewStoreData(HttpContext context, int timeout)
        {
            return new SessionStateStoreData(
                new RedisSessionStateItemCollection(),
                SessionStateUtility.GetSessionStaticObjects(context),
                timeout);
        }

        /// <summary>
        /// This method does nothing in RedisSessionProvider, since there is no way to create an empty
        ///     hash in Redis.
        /// </summary>
        /// <param name="context">The HttpContext of the current request</param>
        /// <param name="id">The Session Id cookie value</param>
        /// <param name="timeout">The Session timeout value</param>
        public override void CreateUninitializedItem(HttpContext context, string id, int timeout)
        {
            // Redis has no way to create an empty hash, so do nothing
        }

        /// <summary>
        /// Cleans up the RedisSessionProvider, however it DOES NOT clear Redis data since each
        ///     interaction with Redis already sets the expiration time on each Session ID key, which
        ///     means that the Redis data should expire itself eventually anyways.
        /// </summary>
        public override void Dispose()
        {
        }

        /// <summary>
        /// The last call to occur from the SessionStateStoreProvider, this does nothing currently
        /// </summary>
        /// <param name="context">The HttpContext of the current web request</param>
        public override void EndRequest(HttpContext context)
        {
        }

        /// <summary>
        /// The first call to occur from the SessionStateStoreProvider for each request, current unused
        /// </summary>
        /// <param name="context">The HttpContext of the current web request</param>
        public override void InitializeRequest(HttpContext context)
        {
            // do nothing
        }

        /// <summary>
        /// This method sets up the event handler for the Session.OnEnd event. When the event handler is 
        ///     successfully attached, this method returns true. This method currently always returns false
        ///     because OnEnd is not supported in RedisSessionProvider v1. A future implementation may use
        ///     Redis Channels to implement this capability, but aside from that there is no way to 
        ///     generically communicate key-expirations back to the C# layer, which is where the 
        ///     expireCallback would be called.
        /// </summary>
        /// <param name="expireCallback">The callback function to execute when a Session expires</param>
        /// <returns>A bool indicating whether or not the expire callback was registered</returns>
        public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
        {
            // we do not accept callback methods on session expiration for now
            return false;
        }

        /// <summary>
        /// This method is called when the Session decides that no element has been modified. Due to
        ///     the implementation of RedisSessionProvider.RedisSessionStateItemCollection, this method
        ///     is never called (because the .Dirty property always returns true). The reason is because
        ///     we always dirty-check the collection in SetAndReleaseItemExclusive to ensure Session
        ///     correctness.
        /// </summary>
        /// <param name="context">The HttpContext of the current web request</param>
        /// <param name="id">The SessionId of the current request</param>
        /// <param name="lockId">A lock around the current Session so no other web request can modify it 
        ///     simultaneously</param>
        public override void ReleaseItemExclusive(HttpContext context, string id, object lockId)
        {
            // if, for some reason, we are in this method we still want to record that we are no
            //      longer going to use a session in local cache
            string currentRedisHashId = RedisSessionStateStoreProvider.RedisHashIdFromSessionId(
                    new HttpContextWrapper(context),
                    id);

            LocalSharedSessionDictionary sharedSessDict = new LocalSharedSessionDictionary();
            sharedSessDict.GetSessionForEndRequest(currentRedisHashId);
        }

        /// <summary>
        /// Gets a Session from Redis, indicating a non-exclusive lock on the Session. Note that GetItemExclusive
        ///     calls this method internally, meaning we do not support locks at all retrieving the Session.
        /// </summary>
        /// <param name="context">The HttpContext of the current request</param>
        /// <param name="id">The Session Id, which is the key name in Redis</param>
        /// <param name="locked">Whether or not the Session is locked to exclusive access for a single request 
        ///     thread</param>
        /// <param name="lockAge">The age of the lock</param>
        /// <param name="lockId">The object used to lock the Session</param>
        /// <param name="actions">Whether or not to initialize the Session (never)</param>
        /// <returns>The Session objects wrapped in a SessionStateStoreData object</returns>
        public override SessionStateStoreData GetItem(HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actions)
        {
            locked = false;
            lockAge = new TimeSpan(0);
            lockId = null;
            actions = SessionStateActions.None;

            try
            {
                string parsedRedisHashId = RedisSessionStateStoreProvider.RedisHashIdFromSessionId(
                    new HttpContextWrapper(context),
                    id);

                LocalSharedSessionDictionary sharedSessDict = new LocalSharedSessionDictionary();
            
                RedisSessionStateItemCollection items = sharedSessDict.GetSessionForBeginRequest(
                    parsedRedisHashId,
                    (redisKey) => {
                        return RedisSessionStateStoreProvider.GetItemFromRedis(
                            redisKey, 
                            new HttpContextWrapper(context),
                            this.SessionTimeout);
                    });

                return new SessionStateStoreData(
                    items,
                    SessionStateUtility.GetSessionStaticObjects(context),
                    Convert.ToInt32(this.SessionTimeout.TotalMinutes));
            }
            catch(Exception e)
            {
                if (RedisSessionConfig.SessionExceptionLoggingDel != null)
                {
                    RedisSessionConfig.SessionExceptionLoggingDel(e);
                }
            }

            return this.CreateNewStoreData(context, Convert.ToInt32(this.SessionTimeout.TotalMinutes));
        }

        /// <summary>
        /// Technically, this should lock the Session to exclusive access by a single request thread. We just return the
        ///     same as GetItem since we have no need for exclusivity.
        /// </summary>
        /// <param name="context">the HttpContext of the current web request</param>
        /// <param name="id">The Session ID, which is also the Redis key name</param>
        /// <param name="locked">Whether or not we locked the Session item (we never do)</param>
        /// <param name="lockAge">The age of the lock</param>
        /// <param name="lockId">The object used to lock the item</param>
        /// <param name="actions">Whether or not to initialize the Session (we never do)</param>
        /// <returns>The Session items from Redis, wrapped in a SessionStateStoreData object</returns>
        public override SessionStateStoreData GetItemExclusive(HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actions)
        {
            return GetItem(context, id, out locked, out lockAge, out lockId, out actions);
        }

        /// <summary>
        /// Deletes an entire Session from Redis by removing the key.
        /// </summary>
        /// <param name="context">The HttpContext of the current request</param>
        /// <param name="id">The Session Id</param>
        /// <param name="lockId">The object used to lock the Session</param>
        /// <param name="item">The Session's properties</param>
        public override void RemoveItem(HttpContext context, string id, object lockId, SessionStateStoreData item)
        { 
             
            var redisKey =  RedisSessionStateStoreProvider.RedisHashIdFromSessionId(
                    new HttpContextWrapper(context), 
                    id);
            RedisDataAccessor.Remove(redisKey);

        }

        /// <summary>
        /// This method should set the expiration time on the Redis key for the Session, but we do not
        ///     set it here opting instead to set it on each Get and SetItem call.
        /// </summary>
        /// <param name="context">The HttpContext of the current web request</param>
        /// <param name="id">The Session Id</param>
        public override void ResetItemTimeout(HttpContext context, string id)
        {
        }

        /// <summary>
        /// Checks if any items have changed in the Session, and stores the results to Redis
        /// </summary>
        /// <param name="context">The HttpContext of the current web request</param>
        /// <param name="id">The Session Id and Redis key name</param>
        /// <param name="item">The Session properties</param>
        /// <param name="lockId">The object used to exclusively lock the Session (there shouldn't be one)</param>
        /// <param name="newItem">Whether or not the Session was created in this HttpContext</param>
        public override void SetAndReleaseItemExclusive(HttpContext context, string id, SessionStateStoreData item, object lockId, bool newItem)
        {
            try
            {
                string currentRedisHashId = RedisSessionStateStoreProvider.RedisHashIdFromSessionId(
                    new HttpContextWrapper(context), 
                    id);

                LocalSharedSessionDictionary sharedSessDict = new LocalSharedSessionDictionary();
                RedisSessionStateItemCollection redisItems = 
                    sharedSessDict.GetSessionForEndRequest(currentRedisHashId);

                // we were unable to pull it from shared cache, meaning either this is the first request or
                //      something went wrong with the local cache. We still have all the parts needed to write
                //      to redis, however, by looking at SessionStateStoreData passed in from the Session module
                //      and the current hash key provided by the id parameter.
                if (redisItems == null)
                {
                    redisItems = (RedisSessionStateItemCollection)item.Items;
                }

                if (redisItems != null)
                {
                    RedisSessionStateStoreProvider.SerializeToRedis(
                        new HttpContextWrapper(context),
                        redisItems,
                        currentRedisHashId,
                        this.SessionTimeout);
                }
            }
            catch (Exception e)
            {
                if (RedisSessionConfig.SessionExceptionLoggingDel != null)
                {
                    RedisSessionConfig.SessionExceptionLoggingDel(e);
                }
            }
        }

        /// <summary>
        /// Gets a hash from Redis and passes it to the constructor of RedisSessionStateItemCollection
        /// </summary>
        /// <param name="redisKey">The key of the Redis hash</param>
        /// <param name="context">The HttpContext of the current web request</param>
        /// <returns>An instance of RedisSessionStateItemCollection, may be empty if Redis call fails</returns>
        public static RedisSessionStateItemCollection GetItemFromRedis(
            string redisKey, 
            HttpContextBase context,
            TimeSpan expirationTimeout)
        {

            try
            {
                Dictionary<string, object> redisData = RedisDataAccessor.GetHashAllItems(redisKey);
                
                RedisDataAccessor.SetExpire(redisKey, expirationTimeout);

                return new RedisSessionStateItemCollection(
                    redisData,
                    0);
            }
            catch (Exception e)
            {
                if (RedisSessionConfig.SessionExceptionLoggingDel != null)
                {
                    RedisSessionConfig.SessionExceptionLoggingDel(e);
                }
            }

            return new RedisSessionStateItemCollection();
        }

        /// <summary>
        /// Helper method for serializing objects to Redis
        /// </summary>
        /// <param name="confirmedChangedObjects">keys and values that have definitely changed</param>
        /// <param name="allObjects">keys and values that have been accessed during the current HttpContext</param>
        /// <param name="allObjectsOriginalState">keys and serialized values before we tampered with them</param>
        /// <param name="deletedObjects">keys that were deleted during the current HttpContext</param>
        /// <param name="currentRedisHashId">The current Redis key name</param>
        /// <param name="redisConn">A connection to Redis</param>
        public static void SerializeToRedis(
            HttpContextBase context,
            RedisSessionStateItemCollection redisItems,
            string currentRedisHashId,
            TimeSpan expirationTimeout)
        {
            Dictionary<string, object> setItems = new Dictionary<string,object>();
            List<string> delItems = new List<string>();

            
            // Determine if we are adding or removing keys, separate them into their own lists
            //      note that redisItems.GetChangedObjectsEnumerator contains complex logic
            foreach (var changedObj in
                redisItems.GetChangedObjectsEnumerator())
            {
                if (changedObj.Value != null)
                {
                    setItems.Add(changedObj.Key, changedObj.Value);
                }
                else
                {
                    delItems.Add(changedObj.Key);
                }
            }

          //  IDatabase redisConn = rConnWrap.GetConnection();

            if (setItems.Count > 0)
            {
                Dictionary<string, object> writeItems = setItems;
                RedisDataAccessor.SetHash(currentRedisHashId, writeItems);

                // call appropriate delegate if set for changing keys
               
            }
            if (delItems != null && delItems.Count > 0)
            {
                string[] removeItems = delItems.ToArray();
                RedisDataAccessor.RemoveHashFields(currentRedisHashId, removeItems);  
                // call appropriate delegate if set for removing keys
                if(RedisSessionConfig.RedisRemoveFieldDel != null)
                {
                    RedisSessionConfig.RedisRemoveFieldDel(
                        context,
                        removeItems,
                        currentRedisHashId);
                }
            }

            // always refresh the timeout of the session hash
            RedisDataAccessor.SetExpire(currentRedisHashId, expirationTimeout);
             
        }

        

        /// <summary>
        /// Helper method for getting a Redis key name from a Session Id
        /// </summary>
        /// <param name="context">The current HttpContext</param>
        /// <param name="sessId">The value of the Session ID cookie from the web request</param>
        /// <returns>A string which is the address of the Session in Redis</returns>
        public static string RedisHashIdFromSessionId(HttpContextBase context, string sessId)
        {
            if (RedisSessionConfig.RedisKeyFromSessionIdDel == null)
            {
                return sessId;
            }
            else
            {
                return RedisSessionConfig.RedisKeyFromSessionIdDel(context, sessId);
            }
        }
    }
}