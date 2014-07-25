using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using X13.plugin;
using System.Diagnostics;

namespace X13.Engine_UT {
  [TestClass]
  public class PLC_UT {
    [TestInitialize()]
    public void TestInitialize() {
      PLC.instance.Clear();
      PLC.instance.Tick();
      var dAdd=Topic.root.Get("/etc/declarers/func/ADD");
      dAdd.Set(new PDeclarer());
      dAdd.Get("A").Set(new PinInfo() { dir=false });
      dAdd.Get("B").Set(new PinInfo() { dir=false });
      dAdd.Get("Q").Set(new PinInfo() { dir=true });
    }

    [TestMethod]
    public void T01() {
      var plc=PLC.instance;

      var A01=Topic.root.Get("/plc/T01/A01");
      A01.Set(new PiBlock("func/ADD"));
      A01.Get("A").Set(19);
      A01.Get("B").Set(2);
      var A01_Q=A01.Get("Q");
      var A02=Topic.root.Get("/plc/T01/A02");
      A02.Set(new PiBlock("func/ADD"));
      A02.Get("A").Set(A01_Q);
      A02.Get("B").Set(5);
      A02.Get("Q");

      plc.Init();

      plc.Tick();
      plc.Tick();

      plc.Start();
      Assert.AreEqual(2, A01.As<PiBlock>().layer);
      Assert.AreEqual(3, A02.As<PiBlock>().layer);

      plc.Stop();
    }
    [TestMethod]
    public void T02() {
      var plc=PLC.instance;

      var A01=Topic.root.Get("/plc/T02/A01");
      A01.Set(new PiBlock("func/ADD"));
      A01.Get("A").Set(19);
      A01.Get("B").Set(2);
      var A01_Q=A01.Get("Q");

      var A02=Topic.root.Get("/plc/T02/A02");
      A02.Set(new PiBlock("func/ADD"));
      A02.Get("A").Set(A01_Q);
      var A02_Q=A02.Get("Q");

      var A03=Topic.root.Get("/plc/T02/A03");
      A03.Set(new PiBlock("func/ADD"));
      A03.Get("A").Set(A02_Q);
      A03.Get("B").Set(5);
      var A03_Q=A03.Get("Q");
      A02.Get("B").Set(A03_Q);

      plc.Init();

      plc.Tick();
      plc.Tick();

      plc.Start();
      Assert.AreEqual(2, A01.As<PiBlock>().layer);
      Assert.AreEqual(3, A02.As<PiBlock>().layer);
      Assert.AreEqual(2, A03.As<PiBlock>().layer);

      plc.Stop();
    }
    [TestMethod]
    public void T03() {
      var plc=PLC.instance;

      var A01=Topic.root.Get("/plc/T03/A01");
      A01.Set(new PiBlock("func/ADD"));
      A01.Get("A").Set(19);
      A01.Get("B").Set(2);
      var A01_Q=A01.Get("Q");

      var A02=Topic.root.Get("/plc/T03/A02");
      A02.Set(new PiBlock("func/ADD"));
      A02.Get("A").Set(A01_Q);
      var A02_Q=A02.Get("Q");
      A02.Get("B").Set(A02_Q);

      plc.Init();

      plc.Tick();
      plc.Tick();

      plc.Start();
      Assert.AreEqual(2, A01.As<PiBlock>().layer);
      Assert.AreEqual(3, A02.As<PiBlock>().layer);

      plc.Stop();
    }
    [TestMethod]
    public void T04() {
      var plc=PLC.instance;
      plc.Init();

      var A01=Topic.root.Get("/plc/T01/A01");
      A01.Set(new PiBlock("func/ADD"));
      var A01_A=A01.Get("A");
      A01_A.Set(19);
      A01.Get("B").Set(2);
      var A01_Q=A01.Get("Q");

      var A02=Topic.root.Get("/plc/T01/A02");
      A02.Set(new PiBlock("func/ADD"));
      A02.Get("A").Set(A01_Q);
      A02.Get("B").Set(5);
      var A02_Q=A02.Get("Q");

      plc.Tick();
      plc.Tick();

      plc.Start();
      Assert.AreEqual(2, A01.As<PiBlock>().layer);
      Assert.AreEqual(3, A02.As<PiBlock>().layer);
      Assert.AreEqual(26, A02_Q.AsDouble);

      A01_A.Set(9);
      plc.Tick();
      Assert.AreEqual(16, A02_Q.AsDouble);

      plc.Stop();
    }
    [TestMethod]
    public void T05() {
      var c1=new PiConst(15);
      Assert.AreEqual(15, c1.As<long>());

      //var o1=c1.As<object>();
      GC.Collect();
      GC.Collect();
      long mem1=GC.GetTotalMemory(true);
      long value = 0;
      value = c1.As<long>();
      var watch = Stopwatch.StartNew();

      for(long i = 0; i < 10000000; i++) {
        c1.Set(i);
        value+= c1.As<long>();
      }

      watch.Stop();
      long mem2=GC.GetTotalMemory(false);

      System.IO.File.AppendAllText("PLC_UT_T05.log", watch.Elapsed.TotalMilliseconds.ToString()+"\t"+(mem2-mem1).ToString()+"/"+mem2.ToString()+"\r\n");
    }

  }
}
