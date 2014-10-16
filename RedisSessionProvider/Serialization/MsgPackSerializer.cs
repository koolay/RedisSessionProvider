using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MsgPack.Serialization;

namespace RedisSessionProvider.Serialization
{
    public class MsgPackSerializer : IRedisSerializer
    {
        private IMessagePackSingleObjectSerializer serializer = SerializationContext.Default.GetSerializer(typeof(object));
        public object DeserializeOne(byte[] objRaw)
        {

            return serializer.UnpackSingleObject(objRaw);

        }

        public byte[] SerializeOne(object origObj)
        {
            return serializer.PackSingleObject(origObj);
        }
    }
}
