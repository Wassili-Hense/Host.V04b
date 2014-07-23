using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using X13.plugin;

namespace X13.Engine_UT {
  [TestClass]
  public class PLC_UT {
    [TestInitialize()]
    public void TestInitialize() {
      PLC.instance.Clear();
      PLC.instance.Tick();
    }

    [TestMethod]
    public void T01() {
      var plc=PLC.instance;

      var dAdd=Topic.root.Get("/etc/declarers/func/ADD");
      dAdd.Set(new PDeclarer());
      dAdd.Get("A").Set(new PinInfo(){dir=false} );
      dAdd.Get("B").Set(new PinInfo() { dir=false });
      dAdd.Get("Q").Set(new PinInfo() { dir=true });
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

      PLC.instance.Tick();
      PLC.instance.Tick();

      plc.Start();
      Assert.AreEqual(2, A01.As<PiBlock>().layer);
      Assert.AreEqual(3, A02.As<PiBlock>().layer);

      plc.Stop();
    }
    [TestMethod]
    public void T02() {
      var plc=PLC.instance;

      var dSub=Topic.root.Get("/etc/declarers/func/SUB");
      dSub.Set(new PDeclarer());
      dSub.Get("A").Set(new PinInfo() { dir=false });
      dSub.Get("B").Set(new PinInfo() { dir=false });
      dSub.Get("Q").Set(new PinInfo() { dir=true });
      var A01=Topic.root.Get("/plc/T02/A01");
      A01.Set(new PiBlock("func/SUB"));
      A01.Get("A").Set(19);
      A01.Get("B").Set(2);
      var A01_Q=A01.Get("Q");

      var A02=Topic.root.Get("/plc/T02/A02");
      A02.Set(new PiBlock("func/SUB"));
      A02.Get("A").Set(A01_Q);
      var A02_Q=A02.Get("Q");

      var A03=Topic.root.Get("/plc/T02/A03");
      A03.Set(new PiBlock("func/SUB"));
      A03.Get("A").Set(A02_Q);
      A03.Get("B").Set(5);
      var A03_Q=A03.Get("Q");
      A02.Get("B").Set(A03_Q);

      plc.Init();

      PLC.instance.Tick();
      PLC.instance.Tick();

      plc.Start();
      Assert.AreEqual(2, A01.As<PiBlock>().layer);
      Assert.AreEqual(3, A02.As<PiBlock>().layer);
      Assert.AreEqual(2, A03.As<PiBlock>().layer);

      plc.Stop();
    }
    [TestMethod]
    public void T03() {
      var plc=PLC.instance;

      var dMul=Topic.root.Get("/etc/declarers/func/MUL");
      dMul.Set(new PDeclarer());
      dMul.Get("A").Set(new PinInfo() { dir=false });
      dMul.Get("B").Set(new PinInfo() { dir=false });
      dMul.Get("Q").Set(new PinInfo() { dir=true });
      var A01=Topic.root.Get("/plc/T03/A01");
      A01.Set(new PiBlock("func/MUL"));
      A01.Get("A").Set(19);
      A01.Get("B").Set(2);
      var A01_Q=A01.Get("Q");

      var A02=Topic.root.Get("/plc/T03/A02");
      A02.Set(new PiBlock("func/MUL"));
      A02.Get("A").Set(A01_Q);
      var A02_Q=A02.Get("Q");
      A02.Get("B").Set(A02_Q);

      plc.Init();

      PLC.instance.Tick();
      PLC.instance.Tick();

      plc.Start();
      Assert.AreEqual(2, A01.As<PiBlock>().layer);
      Assert.AreEqual(3, A02.As<PiBlock>().layer);

      plc.Stop();
    }
  }
}
