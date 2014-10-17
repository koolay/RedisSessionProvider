using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace RedisSessionProvider.Serialization
{
    /// <summary>
    /// This serializer encodes/decodes Session values into/from JSON for Redis persistence, using
    ///     the Json.NET library. The only exceptions are for ADO.NET types (DataTable and DataSet),
    ///     which revert to using XML serialization.
    /// </summary>
    public class BinarySerializer : IRedisSerializer
    {
        protected readonly BinaryFormatter bf = new BinaryFormatter();

        public object DeserializeOne(byte[] objRaw)
        {

            if (objRaw == null)
            {
                return null;
            }

            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(objRaw, 0, objRaw.Length);
                memoryStream.Seek(0L, SeekOrigin.Begin);
                return this.bf.Deserialize(memoryStream);
            }
        }

        public byte[] SerializeOne(object origObj)
        {
            
            if (origObj == null)
            {
                return null;
            }

            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Seek(0L, SeekOrigin.Begin);
                this.bf.Serialize(memoryStream, origObj);
                return memoryStream.ToArray();
            }

        }
    }
}