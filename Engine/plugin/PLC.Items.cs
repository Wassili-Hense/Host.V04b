﻿using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace X13.plugin {
  internal class PiLink {
    internal PiVar input;
    internal PiVar output;
    internal int layer;

    internal PiLink(PiVar ip, PiVar op) {
      input=ip;
      output=op;
    }
  }
  internal interface IPiValue {
    T As<T>();
    void Set<T>(T val);
    Topic.VT GetVT();
    Type GetVType();
  }
  internal class PiConst : IPiValue {
    public static PiConst Create<T>(T val) {
      PiConst r=new PiConst();
      Perform.Set<T>(val, ref r._vt, ref r._o, ref r._dt);
      return r;
    }
    private Topic.PriDT _dt;
    private object _o;
    private Topic.VT _vt;

    public T As<T>() {
      return Perform.GetVal<T>(_vt, ref _o, _dt);
    }
    public void Set<T>(T val) {
      if(_vt==Topic.VT.Ref && typeof(T)!=typeof(Topic)) {
        //Ignore
      } else {
        Perform.Set<T>(val, ref _vt, ref _o, ref _dt);
      }
    }

    public Topic.VT GetVT() {
      return _vt;
    }

    public Type GetVType() {
        switch(_vt) {
        case Topic.VT.Null:
        case Topic.VT.Undefined:
          return null;
        case Topic.VT.Bool:
          return typeof(bool);
        case Topic.VT.Integer:
          return typeof(long);
        case Topic.VT.Float:
          return typeof(double);
        case Topic.VT.DateTime:
          return typeof(DateTime);
        case Topic.VT.String:
          return _o==null?null:typeof(string);
        case Topic.VT.Ref:
          return _o==null?null:typeof(Topic);
        case Topic.VT.Object:
          return _o==null?null:_o.GetType();
        }
        return null;
    }
  }

  internal class PiVar : IPiValue {
    internal Topic _owner;
    internal List<PiLink> _links;
    internal PiBlock[] calcPath;
    internal PiBlock block;

    /// <summary>false - input, true - output, null - io</summary>
    internal bool? dir { get { return pi==null?null:(bool?)pi.dir; } }
    internal int layer;
    internal PinInfo pi;
    internal bool gray;

    internal PiVar(Topic src) {
      this._owner = src;
      _links=new List<PiLink>();
      layer=0;
    }
    public T As<T>() {
      return _owner.As<T>();
    }
    public void Set<T>(T val) {
      if(_owner._vt==Topic.VT.Ref && typeof(T)!=typeof(Topic)) {
        PLC.instance.GetVar((_owner._o as Topic), true).Set<T>(val);      // ??????
      } else {
        var cmd=Perform.Create<T>(_owner, val, PLC.instance.signAlt);
        cmd.art = Perform.Art.changed;
        PLC.instance.DoPlcCmd(cmd);
      }
    }
    public Topic.VT GetVT() {
      return _owner._vt;
    }
    public Type GetVType() {
      return _owner.vType;
    }
  }

  [JsonObject(MemberSerialization.OptIn)]
  public class PiBlock : ITenant, IComparable<PiBlock> {
    private Topic _owner;
    internal SortedList<string, PiVar> _pins;
    internal PiBlock[] calcPath;
    private PDeclarer _decl;
    public int layer;

    public PiBlock(string declarer) {
      this.declarer=declarer;
      _pins=new SortedList<string, PiVar>();
      calcPath=new PiBlock[] { this };
      layer=0;
    }
    public Topic owner {
      get {
        return _owner;
      }
      set {
        if(_owner!=value) {
          if(_owner!=null) {
            _owner.children.changed-=children_changed;
          }
          _owner=value;
          if(_owner!=null) {
            _decl=PDeclarer.Get(declarer);
            if(_decl==null) {
              X13.lib.Log.Warning("{0}<{1}> - unknown declarer", this._owner.path, this.declarer);
            }
            PLC.instance.AddBlock(this);

            _owner.children.changed+=children_changed;
          }
        }
      }
    }
    [JsonProperty]
    public string declarer { get; private set; }

    private void children_changed(Topic src, Perform p) {
      if(p.art==Perform.Art.create || p.art==Perform.Art.subscribe) {
        if(_decl==null) {
          return;
        }
        PinInfo pi;
        if(_decl.ExistPin(src.name, out pi)) {
          var pin=PLC.instance.GetVar(src, true);
          pin.pi=pi;
          pin.block=this;
          _pins.Add(src.name, pin);
        }
      }
      if(p.art==Perform.Art.changed || p.art==Perform.Art.subscribe) {
        PiVar v;
        if(_pins.TryGetValue(src.name, out v) && (v.dir==false || p.art==Perform.Art.subscribe) && p.prim!=PLC.instance.sign) {
          Calculate();
        }
      }
    }
    private void Calculate() {
      // ADD
      if(_pins.ContainsKey("A") && _pins.ContainsKey("B") && _pins.ContainsKey("Q")) {
        PLC.instance.vm.Load(new byte[] { (byte)VM.OP.ADD, (byte)VM.OP.RET });
        PLC.instance.vm.PushS(_pins["A"]);
        PLC.instance.vm.PushS(_pins["B"]);
        PLC.instance.vm.Step();
        PLC.instance.vm.Step();
        _pins["Q"].Set(PLC.instance.vm.PopS().As<object>());
        //double r=_pins["A"].As<double>()+_pins["B"].As<double>();
        //_pins["Q"].Set(r);
      }
    }

    public int CompareTo(PiBlock other) {
      int l1=this.layer<=0?(this._pins.Select(z=>z.Value).Where(z1 => z1.dir==false && z1.layer>0).Max(z2 => z2.layer)):this.layer;
      int l2=other==null?int.MaxValue:(other.layer<=0?(other._pins.Select(z => z.Value).Where(z1 => z1.dir==false && z1.layer>0).Max(z2 => z2.layer)):other.layer);
      return l1.CompareTo(l2);
    }
  }

  [JsonObject(MemberSerialization.OptIn)]
  public class PDeclarer : ITenant {
    public static PDeclarer Get(string d) {
      Topic t;
      if(Topic.root.Get("/etc/declarers", true).Exist(d, out t) && t.vType==typeof(PDeclarer)) {
        return t.As<PDeclarer>();
      }
      return null;
    }

    private Topic _owner;
    public Topic owner {
      get {
        return _owner;
      }
      set {
        if(_owner!=value) {
          if(_owner!=null) {
            _owner.children.changed-=children_changed;
          }
          _owner=value;
          if(_owner!=null) {
            _owner.children.changed+=children_changed;
          }
        }
      }
    }
    [JsonProperty]
    private string info { get; set; }
    private void children_changed(Topic src, Perform p) {
    }

    public bool ExistPin(string name, out PinInfo pi) {
      Topic t;
      if(_owner.Exist(name, out t) && t.vType==typeof(PinInfo)) {
        pi=t.As<PinInfo>();
        return true;
      }
      pi=null;
      return false;
    }
  }

  [JsonObject(MemberSerialization.OptIn)]
  public class PinInfo {
    [JsonProperty]
    public bool dir { get; set; }
  }
}