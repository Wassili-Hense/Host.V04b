using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using X13.plugin;

namespace X13.Engine_UT {
  [TestClass]
  public class PLC_UT {
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

      Topic.Process();
      Topic.Process();

      plc.Start();

      plc.Stop();
    }
  }
}
