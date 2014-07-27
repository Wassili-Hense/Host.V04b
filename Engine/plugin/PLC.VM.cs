﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace X13.plugin {
  internal class VW {

  }
  public class PiConst {
    static PiConst() {
    }
    public static PiConst Create<T>(T val) {
      PiConst r=new PiConst();
      Perform.Set<T>(val, ref r._vt, ref r._o, ref r._dt);
      return r;
    }
    private Topic.PriDT _dt;
    private object _o;
    private Topic.VT _vt;
    //public PiConst(long l) {
    //  _dt=new Topic.PriDT() { l=l };
    //  _o=null;
    //  _vt=Topic.VT.Integer;
    //}
    public void Set(long v) {
      _dt.l=v;
      _o=null;
      _vt=Topic.VT.Integer;
    }
    public T As<T>() {
      return Perform.GetVal<T>(_vt, ref _o, _dt);
    }
  }
}