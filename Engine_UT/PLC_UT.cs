using System;
using System.Linq;
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

      //var A02=Topic.root.Get("/plc/T01/A02");
      //A02.Set(new PiBlock("func/ADD"));
      //A02.Get("A").Set(A01_Q);
      //A02.Get("B").Set(5);
      //var A02_Q=A02.Get("Q");

      plc.Tick();
      plc.Tick();

      plc.Start();
      //Assert.AreEqual(2, A01.As<PiBlock>().layer);
      Assert.AreEqual(21, A01_Q.As<double>());
      //Assert.AreEqual(3, A02.As<PiBlock>().layer);
      //Assert.AreEqual(26, A02_Q.As<double>());

      A01_A.Set(9);
      plc.Tick();
      Assert.AreEqual(11, A01_Q.As<double>());
      //Assert.AreEqual(16, A02_Q.As<double>());

      plc.Stop();
    }
    [TestMethod]
    public void T20() {
      Compiler c=new Compiler();
      Lexem[] l;
      l=c.ParseLex("0").ToArray();
      Assert.AreEqual(1, l.Length);
      Assert.AreEqual(Lexem.LexTyp.Integer, l[0].typ);
      Assert.AreEqual("0", l[0].content);

      l=c.ParseLex("012 34").ToArray();
      Assert.AreEqual(2, l.Length);
      Assert.AreEqual(Lexem.LexTyp.Integer, l[0].typ);
      Assert.AreEqual("012", l[0].content);
      Assert.AreEqual(Lexem.LexTyp.Integer, l[1].typ);
      Assert.AreEqual("34", l[1].content);

      l=c.ParseLex("0x15\t257").ToArray();
      Assert.AreEqual(2, l.Length);
      Assert.AreEqual(Lexem.LexTyp.Hex, l[0].typ);
      Assert.AreEqual("0x15", l[0].content);
      Assert.AreEqual(Lexem.LexTyp.Integer, l[1].typ);
      Assert.AreEqual("257", l[1].content);

      l=c.ParseLex("24.19 0.91e1\n.173").ToArray();
      Assert.AreEqual(3, l.Length);
      Assert.AreEqual(Lexem.LexTyp.Float, l[0].typ);
      Assert.AreEqual("24.19", l[0].content);
      Assert.AreEqual(Lexem.LexTyp.Float, l[1].typ);
      Assert.AreEqual("0.91e1", l[1].content);
      Assert.AreEqual(Lexem.LexTyp.Float, l[2].typ);
      Assert.AreEqual(".173", l[2].content);
      Assert.AreEqual(2, l[2].line);

      l=c.ParseLex("\"qwerty\"").ToArray();
      Assert.AreEqual(1, l.Length);
      Assert.AreEqual(Lexem.LexTyp.String, l[0].typ);
      Assert.AreEqual("qwerty", l[0].content);

      l=c.ParseLex("'\\'\\\"\\\\' \"1 \\n\\r\\u0041\"").ToArray();
      Assert.AreEqual(2, l.Length);
      Assert.AreEqual(Lexem.LexTyp.String, l[0].typ);
      Assert.AreEqual("\'\"\\", l[0].content);
      Assert.AreEqual(Lexem.LexTyp.String, l[1].typ);
      Assert.AreEqual("1 \n\r\u0041", l[1].content);

      l=c.ParseLex("12 // integral constant\r").ToArray();
      Assert.AreEqual(1, l.Length);
      Assert.AreEqual(Lexem.LexTyp.Integer, l[0].typ);
      Assert.AreEqual("12", l[0].content);

      l=c.ParseLex("13 /*integral constant */ 14").ToArray();
      Assert.AreEqual(2, l.Length);
      Assert.AreEqual(Lexem.LexTyp.Integer, l[0].typ);
      Assert.AreEqual("13", l[0].content);
      Assert.AreEqual(Lexem.LexTyp.Integer, l[1].typ);
      Assert.AreEqual("14", l[1].content);

      l=c.ParseLex("15 /*integral \n\r constant */ 16").ToArray();
      Assert.AreEqual(2, l.Length);
      Assert.AreEqual(Lexem.LexTyp.Integer, l[0].typ);
      Assert.AreEqual("15", l[0].content);
      Assert.AreEqual(Lexem.LexTyp.Integer, l[1].typ);
      Assert.AreEqual("16", l[1].content);

    }


  }
}
