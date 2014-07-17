using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace X13.plugin {
  public class PLC {
    static PLC() {
      instance=new PLC();
    }
    public static readonly PLC instance;

    private List<PiBlock> _blocks;
    private Dictionary<Topic, PiVar> _vars;
    public PLC() {
      _blocks=new List<PiBlock>();
      _vars=new Dictionary<Topic, PiVar>();
    }
    public void Init() {
    }
    public void Start() {
    }
    public void Stop() {
    }


    internal void AddBlock(PiBlock bl) {
      _blocks.Add(bl);
    }
    internal PiVar GetVar(Topic t) {
      PiVar v;
      if(!_vars.TryGetValue(t, out v)) {
        v=new PiVar(t);
        _vars[t]=v;
      }
      return v;
    }
  }
  internal class PiLink {
    public PiVar input;
    public PiVar output;
    public int layer;
  }

  internal class PiVar {
    internal Topic _owner;
    internal List<PiLink> _links;

    /// <summary>false - input, true - output, null - io</summary>
    public bool? dir { get { return pi==null?null:(bool?)pi.dir; } }
    public int layer;
    public   PinInfo pi;
    public bool gray;

    public PiVar(Topic src) {
      this._owner = src;
      _links=new List<PiLink>();
      layer=int.MinValue;
    }
  }

  [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
  public class PiBlock : ITenant {
    private Topic _owner;
    private List<PiVar> _pins;
    private PDeclarer _decl;

    public PiBlock(string declarer) {
      this.declarer=declarer;
      _pins=new List<PiVar>();
      PLC.instance.AddBlock(this);
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
            _owner.children.changed+=children_changed;
          }
        }
      }
    }
    [Newtonsoft.Json.JsonProperty]
    public string declarer{get; private set;}

    private void children_changed(Topic src, TopicCmd p) {
      if(p.art==TopicCmd.Art.create || p.art==TopicCmd.Art.subscribe) {
        if(_decl==null) {
          return;
        }
        PinInfo pi;
        if(_decl.ExistPin(src.name, out pi)) {
          var pin=PLC.instance.GetVar(src);
          pin.pi=pi;
          _pins.Add(pin);
        }
      }
    }
    public int layer;
  }

  [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
  public class PDeclarer : ITenant {
    private static Topic _droot;
    static PDeclarer() {
      _droot=Topic.root.Get("/etc/declarers", true);
    }
    internal static PDeclarer Get(string d) {
      Topic t;
      if(_droot.Exist(d, out t) && t.vType==typeof(PDeclarer)) {
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
    [Newtonsoft.Json.JsonProperty]
    private string info { get; set; }
    private void children_changed(Topic src, TopicCmd p) {
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

  [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
  public class PinInfo{
    [Newtonsoft.Json.JsonProperty]
    public bool dir { get; set; }
  }

  /*
  public class PiGroup : PiBlock {
    private List<PiBlock> _blocks;

    public PiGroup()
      : base("schema") {
        _blocks=new List<PiBlock>();
    }
    protected override void children_changed(Topic src, TopicCmd p) {
      base.children_changed(src, p);
    }
  }*/
}
