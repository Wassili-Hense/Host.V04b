using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using X13.plugin;
using System.IO;
using System.Threading;

namespace X13.Engine_UT {
  [TestClass]
  public class PS_UT {
    [TestInitialize()]
    public void TestInitialize() {
      if(File.Exists("../data/persist.xdb")) {
        File.Delete("../data/persist.xdb");
      }
      Topic.Clear();
      Topic.root.Get("/etc/PersistentStorage/verbose").Set(false);
      Topic.Process();
    }

    [TestMethod]
    public void PS_T01() {
      PersistentStorage ps=new PersistentStorage();
      ps.Init();
      Topic.Process();
      ps.Start();
      var r=Topic.root.Get("/PS_UT/T01");
      var r_a=r.Get("A");
      var r_b=r.Get("B");
      r_b.local=true;
      Topic.Process();
      r_a.Set(192);
      r_b.Set(true);
      Topic.Process();
      //Thread.Sleep(3000);
      ps.Stop();
    }
    [TestMethod]
    public void PS_T02() {
      File.Copy("../data/T02.xdb", "../data/persist.xdb");
      PersistentStorage ps=new PersistentStorage();
      ps.Init();
      Topic.Process();
      ps.Start();
      Topic r, r_a, r_b;
      Assert.IsTrue(Topic.root.Exist("/PS_UT/T02", out r));
      Assert.IsTrue(r.Exist("A", out r_a));
      Assert.IsTrue(r.Exist("B", out r_b));
      Assert.AreEqual(192, r_a.AsLong);
      Assert.IsTrue(r_b.local);
      ps.Stop();
    }
    //[TestMethod] public void PS_T01() { }
    //[TestMethod] public void PS_T01() { }
    //[TestMethod] public void PS_T01() { }
    //[TestMethod] public void PS_T01() { }
    //[TestMethod] public void PS_T01() { }
    //[TestMethod] public void PS_T01() { }
  }
}
