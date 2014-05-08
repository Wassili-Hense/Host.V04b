﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace X13.Engine_UT {
  /// <summary>
  /// Zusammenfassungsbeschreibung für Topic_UT
  /// </summary>
  [TestClass]
  public class Topic_UT {
    public Topic_UT() {
      //
      // TODO: Konstruktorlogik hier hinzufügen
      //
    }

    private TestContext testContextInstance;

    /// <summary>
    ///Ruft den Textkontext mit Informationen über
    ///den aktuellen Testlauf sowie Funktionalität für diesen auf oder legt diese fest.
    ///</summary>
    public TestContext TestContext {
      get {
        return testContextInstance;
      }
      set {
        testContextInstance = value;
      }
    }

    #region Zusätzliche Testattribute
    //
    // Sie können beim Schreiben der Tests folgende zusätzliche Attribute verwenden:
    //
    // Verwenden Sie ClassInitialize, um vor Ausführung des ersten Tests in der Klasse Code auszuführen.
    // [ClassInitialize()]
    // public static void MyClassInitialize(TestContext testContext) { }
    //
    // Verwenden Sie ClassCleanup, um nach Ausführung aller Tests in einer Klasse Code auszuführen.
    // [ClassCleanup()]
    // public static void MyClassCleanup() { }
    //
    // Mit TestInitialize können Sie vor jedem einzelnen Test Code ausführen. 
    // [TestInitialize()]
    // public void MyTestInitialize() { }
    //
    // Mit TestCleanup können Sie nach jedem einzelnen Test Code ausführen.
    // [TestCleanup()]
    // public void MyTestCleanup() { }
    //
    #endregion

    private static Topic root;
    private static Random r;
    [ClassInitialize()]
    public static void MyClassInitialize(TestContext testContext) {
      r=new Random((int)DateTime.Now.Ticks);
      root=Topic.root;
      //root.SetJson("{\"$type\":\"X13.Engine_UT.TestObj, Engine_UT\",\"A\":315,\"B\":0.41}");
      //Topic.Process();
      //root.ToJson();
    }
    [TestInitialize()]
    public void TestInitialize() {
      Topic.Clear();
      Topic.Process();
    }

    private List<TopicCmd> cmds1;
    private void cmds1Fire(Topic t, TopicCmd c) {
      cmds1.Add(c);
    }

    [TestMethod]
    public void T01() {
      Topic A1=root.Get("A1");
      Assert.AreEqual(root, A1.parent);
      Assert.AreEqual("A1", A1.name);
      Assert.AreEqual("/A1", A1.path);
    }
    [TestMethod]
    public void T02() {
      Topic A1=root.Get("A1");
      long val=r.Next();
      A1.Set(val);
      Topic.Process();
      Assert.AreEqual(val, A1.AsLong);
    }
    [TestMethod]
    public void T03() {
      Topic A1=root.Get("A1");
      long val=r.Next();
      A1.Set(val);
      Topic A2=root.Get("/A2");
      A2.Set(A1);
      Topic.Process();
      Assert.AreEqual(val, A2.AsLong);

      val=r.Next();
      A1.Set(val);
      Topic.Process();
      Assert.AreEqual(val, A2.AsLong);

      var now=DateTime.Now;
      A2.Set(now);
      Topic.Process();
      Assert.AreEqual(now, A1.AsDateTime);

      A2.Set(null);   // reset reference
      Topic.Process();
      Assert.AreEqual(null, A2.AsObject);

      A2.Set(true);
      Topic.Process();
      Assert.AreEqual(now, A1.AsDateTime);
      Assert.AreEqual(true, A2.AsObject);
    }
    [TestMethod]
    public void T04() {   // parse to bool
      Topic A1=root.Get("A1");
      A1.Set(true);
      Topic.Process();
      Assert.AreEqual(true, A1.AsBool);
      A1.Set(false);
      Topic.Process();
      Assert.AreEqual(false, A1.AsBool);
      A1.Set((object)true);
      Topic.Process();
      Assert.AreEqual(true, A1.AsBool);
      A1.Set(0);
      Topic.Process();
      Assert.AreEqual(false, A1.AsBool);
      A1.Set(r.Next(1, int.MaxValue));
      Topic.Process();
      Assert.AreEqual(true, A1.AsBool);
      A1.Set("false");
      Topic.Process();
      Assert.AreEqual(false, A1.AsBool);
      A1.Set("True");
      Topic.Process();
      Assert.AreEqual(true, A1.AsBool);
    }
    [TestMethod]
    public void T05() {   // parse to long
      Topic A1=root.Get("A1");
      A1.Set((object)257);
      Topic.Process();
      Assert.AreEqual(257, A1.AsLong);
      A1.Set(25.7);
      Topic.Process();
      Assert.AreEqual(25, A1.AsLong);
      A1.Set("94");
      Topic.Process();
      Assert.AreEqual(94, A1.AsLong);
      A1.Set("0x15");
      Topic.Process();
      Assert.AreEqual(0, A1.AsLong);
      A1.Set("17.6");
      Topic.Process();
      Assert.AreEqual(17, A1.AsLong);
      A1.Set(true);
      Topic.Process();
      Assert.AreEqual(1, A1.AsLong);
      A1.Set(new DateTime(917L));
      Topic.Process();
      Assert.AreEqual(917, A1.AsLong);
    }
    [TestMethod]
    public void T06() {   // parse to double
      Topic A1=root.Get("A1");
      A1.Set((object)257.158);
      Topic.Process();
      Assert.AreEqual(257.158, A1.AsDouble);
      A1.Set(52);
      Topic.Process();
      Assert.AreEqual(52.0, A1.AsDouble);
      A1.Set("913");
      Topic.Process();
      Assert.AreEqual(913.0, A1.AsDouble);
      A1.Set("0x15");
      Topic.Process();
      Assert.AreEqual(0.0, A1.AsDouble);
      A1.Set("294.3187");
      Topic.Process();
      Assert.AreEqual(294.3187, A1.AsDouble);
      A1.Set(true);
      Topic.Process();
      Assert.AreEqual(1.0, A1.AsDouble);
      A1.Set(DateTime.FromOADate(1638.324));
      Topic.Process();
      Assert.AreEqual(1638.324, A1.AsDouble);
    }
    [TestMethod]
    public void T07() {
      Topic A3=root.Get("A3");
      long val=r.Next();
      A3.Set(val);
      Topic.Process();
      Assert.AreEqual(val, A3.AsLong);
      A3.Remove();
      A3.Set(Math.PI);
      Topic.Process();
      Assert.AreEqual(true, A3.disposed);
      Assert.AreEqual(null, A3.AsObject);
    }
    [TestMethod]
    public void T08() {
      Topic t0=root.Get("child");
      var arr=t0.children.ToArray();
      Assert.AreEqual(0, arr.Length);
      var t1=t0.Get("ch_a");
      arr=t0.children.ToArray();
      Assert.AreEqual(1, arr.Length);
      Assert.AreEqual(t1, arr[0]);
      t1=t0.Get("ch_b");
      var t2=t1.Get("a");
      t2=t1.Get("b");
      t1=t0.Get("ch_c");
      t2=t1.Get("a");
      arr=t0.children.ToArray();
      Assert.AreEqual(3, arr.Length);
      arr=t0.all.ToArray();
      Assert.AreEqual(7, arr.Length);  // child, ch_a, ch_b, ch_b/a, ch_b/b, ch_c, ch_c/a
      Assert.AreEqual(t2, arr[6]);
      Assert.AreEqual(t1, arr[5]);
      Assert.AreEqual(t0, arr[0]);
    }
    [TestMethod]
    public void T09() {
      Topic t0=root.Get("child1");
      cmds1=new List<TopicCmd>();
      Topic.Process();
      t0.changed+=cmds1Fire;
      Topic.Process();
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(t0, cmds1[0].src);
      Assert.AreEqual(TopicCmd.Art.subscribe, cmds1[0].art);
      cmds1.Clear();
      var t1=t0.Get("ch_a");
      t1.Set("Hi");
      Topic.Process();
      Assert.AreEqual(0, cmds1.Count);
      cmds1.Clear();
      t0.changed-=cmds1Fire;
      t0.Set(2.98);
      Topic.Process();
      Assert.AreEqual(0, cmds1.Count);
      cmds1.Clear();
    }
    [TestMethod]
    public void T10() {
      Topic A1=root.Get("A1");
      long val=r.Next();
      A1.Set(val);
      Topic A2=root.Get("/A2");
      cmds1=new List<TopicCmd>();
      A2.changed+=cmds1Fire;
      A2.Set(A1);
      Topic.Process();
      Assert.AreEqual(val, A2.AsLong);
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(A2, cmds1[0].src);
      cmds1.Clear();

      val=r.Next();
      A1.Set(val);
      Topic.Process();
      Assert.AreEqual(val, A2.AsLong);
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(A1, cmds1[0].src);
      cmds1.Clear();

      var now=DateTime.Now;
      A2.Set(now);
      Topic.Process();
      Assert.AreEqual(now, A1.AsDateTime);
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(A1, cmds1[0].src);
      cmds1.Clear();

      A2.Set(null);   // reset reference
      Topic.Process();
      Assert.AreEqual(null, A2.AsObject);
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(A2, cmds1[0].src);
      cmds1.Clear();

      A2.Set(true);
      Topic.Process();
      Assert.AreEqual(now, A1.AsDateTime);
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(A2, cmds1[0].src);
      cmds1.Clear();

      val=r.Next();
      A1.Set(val);
      Topic.Process();
      Assert.AreEqual(val, A1.AsLong);
      Assert.AreEqual(0, cmds1.Count);
      cmds1.Clear();
    }
    [TestMethod]
    public void T11() {
      Topic t0=root.Get("child2");
      var t1=t0.Get("ch_a");
      var t1_a=t1.Get("a");
      cmds1=new List<TopicCmd>();
      Topic.Process();
      t0.children.changed+=cmds1Fire;
      Topic.Process();
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(TopicCmd.Art.subscribe, cmds1[0].art);
      Assert.AreEqual(t1, cmds1[0].src);
      cmds1.Clear();

      t1.Set("Hi");
      Topic.Process();
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(TopicCmd.Art.changed, cmds1[0].art);
      Assert.AreEqual(t1, cmds1[0].src);
      cmds1.Clear();

      var t2=t0.Get("ch_b");
      Topic.Process();
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(TopicCmd.Art.create, cmds1[0].art);
      Assert.AreEqual(t2, cmds1[0].src);
      cmds1.Clear();

      var t2_a=t2.Get("a");
      Topic.Process();
      Assert.AreEqual(0, cmds1.Count);
      cmds1.Clear();
    }
    [TestMethod]
    public void T12() {
      Topic t0=root.Get("child3");
      var t1=t0.Get("ch_a");
      var t1_a=t1.Get("a");
      cmds1=new List<TopicCmd>();
      Topic.Process();
      t0.all.changed+=cmds1Fire;
      Topic.Process();
      Assert.AreEqual(3, cmds1.Count);
      Assert.AreEqual(TopicCmd.Art.subscribe, cmds1[0].art);
      Assert.AreEqual(t0, cmds1[0].src);
      Assert.AreEqual(TopicCmd.Art.subscribe, cmds1[1].art);
      Assert.AreEqual(t1, cmds1[1].src);
      Assert.AreEqual(TopicCmd.Art.subscribe, cmds1[2].art);
      Assert.AreEqual(t1_a, cmds1[2].src);
      cmds1.Clear();

      t1.Set("Hi");
      Topic.Process();
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(TopicCmd.Art.changed, cmds1[0].art);
      Assert.AreEqual(t1, cmds1[0].src);
      cmds1.Clear();

      var t2=t0.Get("ch_b");
      Topic.Process();
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(TopicCmd.Art.create, cmds1[0].art);
      Assert.AreEqual(t2, cmds1[0].src);
      cmds1.Clear();

      var t2_a=t2.Get("a");
      Topic.Process();
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(TopicCmd.Art.create, cmds1[0].art);
      Assert.AreEqual(t2_a, cmds1[0].src);
      cmds1.Clear();
    }
    [TestMethod]
    public void T13() {
      var b1=root.Get("B1");
      Topic.Process();
      b1.Remove();
      Topic.Process();
      Assert.IsTrue(b1.disposed);
      Assert.IsFalse(root.Exist("B1"));
      b1=null;
      var b2=root.Get("B2");
      var b2_a=b2.Get("A");
      Topic.Process();
      b2.Remove();
      Topic.Process();
      Assert.IsTrue(b2.disposed);
      Assert.IsFalse(root.Exist("B2"));
      Assert.IsTrue(b2_a.disposed);
      Assert.IsFalse(root.Exist("/B2/A"));

    }
    [TestMethod]
    public void T14() {
      cmds1=new List<TopicCmd>();

      var b3=root.Get("B3");
      Topic.Process();
      b3.all.changed+=cmds1Fire;
      b3.Set(91.02);
      Topic.Process();
      cmds1.Clear();

      var c3=b3.Move(root, "C3");
      Topic.Process();
      Assert.AreEqual(true, b3.disposed);
      Assert.AreEqual(false, root.Exist("B3"));
      Assert.AreNotEqual(b3, c3);
      Assert.AreEqual("C3", c3.name);
      Assert.AreEqual(91.02, c3.AsDouble);
      Assert.AreEqual(2, cmds1.Count);
      Assert.AreEqual(b3, cmds1[0].src);
      Assert.AreEqual(TopicCmd.Art.move, cmds1[0].art);
      Assert.AreEqual(c3, cmds1[1].src);
      Assert.AreEqual(TopicCmd.Art.create, cmds1[1].art);
      cmds1.Clear();

      var c3_a=c3.Get("A");
      c3_a.Set(9577);
      Topic.Process();
      cmds1.Clear();

      var d3=c3.Move(root, "D3");
      Topic.Process();
      Assert.AreEqual(true, c3.disposed);
      Assert.AreEqual(false, root.Exist("C3"));
      Assert.AreNotEqual(c3, d3);
      Assert.AreEqual("D3", d3.name);
      Assert.AreEqual(91.02, d3.AsDouble);
      Assert.AreEqual(d3, c3_a.parent);
      Assert.AreEqual("/D3/A", c3_a.path);
      Assert.AreEqual(9577, c3_a.AsLong);
      Assert.AreEqual(3, cmds1.Count);
      Assert.AreEqual(c3, cmds1[0].src);
      Assert.AreEqual(TopicCmd.Art.move, cmds1[0].art);
      Assert.AreEqual(d3, cmds1[1].src);
      Assert.AreEqual(TopicCmd.Art.create, cmds1[1].art);
      Assert.AreEqual(c3_a, cmds1[2].src);
      Assert.AreEqual(TopicCmd.Art.create, cmds1[2].art);
      cmds1.Clear();

      d3.Set(17);
      var e3=d3.Move(root, "e3");
      Topic.Process();
      Assert.AreEqual(17, e3.AsLong);
      cmds1.Clear();

    }
    [TestMethod]
    public void T15() {
      cmds1=new List<TopicCmd>();

      var r=Topic.root.Get("t15");
      var a1=r.Get("a1");
      var a2=r.Get("a2");
      a1.Set(13);
      a2.Set(a1);
      a2.changed+=cmds1Fire;
      Topic.Process();
      cmds1.Clear();

      Assert.AreEqual(13, a2.AsLong);
      var b1=a1.Move(r, "b1");
      Topic.Process();
      Assert.AreEqual(b1, a2.AsRef);
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(a2, cmds1[0].src);
      Assert.AreEqual(TopicCmd.Art.changed, cmds1[0].art);
      cmds1.Clear();

      b1.Set(15);
      Topic.Process();
      Assert.AreEqual(15, a2.AsLong);
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(b1, cmds1[0].src);
      Assert.AreEqual(TopicCmd.Art.changed, cmds1[0].art);
      cmds1.Clear();

      b1.Remove();
      Topic.Process();
      Assert.AreEqual(null, a2.AsRef);
      Assert.AreEqual(15, a2.AsLong);
      Assert.AreEqual(1, cmds1.Count);
      Assert.AreEqual(a2, cmds1[0].src);
      Assert.AreEqual(TopicCmd.Art.changed, cmds1[0].art);
      cmds1.Clear();

    }
    [TestMethod]
    public void T16() {
      Topic t16=Topic.root.Get("/t16");
      Topic.Process();
      t16.SetJson("true");
      Topic.Process();
      Assert.AreEqual(typeof(bool), t16.vType);
      Assert.AreEqual(true, t16.AsBool);
      t16.SetJson("137");
      Topic.Process();
      Assert.AreEqual(typeof(long), t16.vType);
      Assert.AreEqual(137, t16.AsLong);
      t16.SetJson("35.97");
      Topic.Process();
      Assert.AreEqual(typeof(double), t16.vType);
      Assert.AreEqual(35.97, t16.AsDouble);
      t16.SetJson("\"2014-04-15T01:23:45\"");
      Topic.Process();
      Assert.AreEqual(typeof(DateTime), t16.vType);
      Assert.AreEqual(new DateTime(2014, 04, 15, 01, 23, 45), t16.AsDateTime);
      t16.SetJson("\"Hello\"");
      Topic.Process();
      Assert.AreEqual(typeof(string), t16.vType);
      Assert.AreEqual("Hello", t16.AsString);

      var a=t16.Get("a");
      a.SetJson("{\"$ref\":\"/t16/b\"}");
      Topic.Process();
      Topic b;
      Assert.AreEqual(true, t16.Exist("b", out b));
      Assert.AreEqual(typeof(Topic), a.vType);
      Assert.AreEqual(b, a.AsRef);

      b.SetJson("{\"$type\":\"X13.Engine_UT.TestObj, Engine_UT\",\"A\":43,\"B\":9.81}");
      Topic.Process();
      var to=b.As<TestObj>();
      Assert.IsNotNull(to);
      Assert.AreEqual(typeof(TestObj), to.GetType());
      Assert.AreEqual(43L, to.A);
      Assert.AreEqual(9.81, to.B);
    }
    [TestMethod]
    public void T17() {
      Topic r=Topic.root.Get("/t17");
      r.Set(false);
      Topic.Process();
      Assert.AreEqual("false", r.ToJson());

      r.Set(34);
      Topic.Process();
      Assert.AreEqual("34", r.ToJson());

      r.Set(28.09);
      Topic.Process();
      Assert.AreEqual("28.09", r.ToJson());

      r.Set(new DateTime(2014, 05, 06, 23, 45, 56));
      Topic.Process();
      Assert.AreEqual("\"2014-05-06T23:45:56\"", r.ToJson());

      r.Set("X13.HomeAutomation");
      Topic.Process();
      Assert.AreEqual("\"X13.HomeAutomation\"", r.ToJson());

      var a=r.Get("a");
      var b=r.Get("b");
      a.Set(b);
      Topic.Process();
      Assert.AreEqual("{\"$ref\":\"/t17/b\"}", a.ToJson());

      var to=new TestObj();
      to.A=27;
      to.B=3.1415;

      r.Set(to);
      Topic.Process();
      Assert.AreEqual("{\"$type\":\"X13.Engine_UT.TestObj, Engine_UT\",\"A\":27,\"B\":3.1415}", r.ToJson());
    }

    //[TestMethod] public void T01() { }
    //[TestMethod] public void T01() { }
    //[TestMethod] public void T01() { }
    //[TestMethod] public void T01() { }
    //[TestMethod] public void T01() { }
  }
  public class TestObj {
    public long A;
    public double B;
  }
}
