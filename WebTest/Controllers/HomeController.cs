using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using RedisSessionProvider.Serialization;

namespace WebTest.Controllers
{
     
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            var entity = new Foo() { Bar = new Bar() { Age = 1, Name = "bar" }, Id = 1 };
            BinarySerializer sr = new BinarySerializer();
            byte[] sd = sr.SerializeOne(entity);
            object val =  sr.DeserializeOne(sd);
            Session.Clear(); 
            Session["hi"] = DateTime.Now.ToString();
            Session["foo"] = entity;
            var foo = Session["foo"] as Foo;
            var viewHtml = JsonConvert.SerializeObject(foo); 
            return View(new MvcHtmlString(viewHtml));
        }

        public ActionResult T1()
        {
            var foo = Session["foo"] as Foo;
            foo.Bar.Name = "t1";
            var viewHtml = JsonConvert.SerializeObject(foo);
            return Content(viewHtml);
        }

        public ActionResult T2()
        {
            var foo = Session["foo"] as Foo;
            foo.Bar.Name = "t2";
            System.Threading.Thread.Sleep(3000);
            var viewHtml = JsonConvert.SerializeObject(foo);
            return Content(viewHtml);
        }

        public ActionResult T3()
        {
            var foo = Session["foo"] as Foo;
            foo.Bar.Name = "t3";

            var viewHtml = JsonConvert.SerializeObject(foo);
            return Content(viewHtml);
        }

        public ActionResult T4()
        {
            var foo = Session["foo"] as Foo;
            foo.Bar.Name = "t4";

            var viewHtml = JsonConvert.SerializeObject(foo);
            return Content(viewHtml);
        }
    }
}
