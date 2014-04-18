using System;
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
    //[TestMethod] public void T01() { }
    //[TestMethod] public void T01() { }
    //[TestMethod] public void T01() { }
    //[TestMethod] public void T01() { }
    //[TestMethod] public void T01() { }
  }
}
