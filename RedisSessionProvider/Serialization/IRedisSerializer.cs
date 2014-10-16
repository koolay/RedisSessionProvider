
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisSessionProvider.Serialization
{
    public interface IRedisSerializer
    {
        object DeserializeOne(byte[] objRaw);


        byte[] SerializeOne(object origObj);
    }
}
