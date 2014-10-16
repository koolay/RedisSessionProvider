using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebTest
{
    [Serializable]
    public class Foo
    {
        public Bar Bar { get; set; }
        public int Id { get; set; }
    }

    [Serializable]
    public class Bar
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}