using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using X13.lib;

namespace X13.plugin {
  public class Compiler {
    public IEnumerable<Lexem> ParseLex(string text) {
      if(string.IsNullOrWhiteSpace(text)) {
        yield break;
      }
      int line=1;
      int row=1;
      int startRow=1;
      int pos=0;
      int startPos=0;
      int state=0;
      int subSt=0;
      char ch;
      while(text.Length>pos || (text.Length==pos && state!=0)) {
        ch=text.Length>pos?text[pos]:'\n';
        switch(state) {
        case 0:     // init
          startPos=pos;
          startRow=row;
          if(char.IsWhiteSpace(ch)) {
            state=0;
          } else if(ch=='0') {
            state=1;
          } else if(char.IsDigit(ch)) {
            state=2;
          } else if(ch=='.') {
            state=4;
          } else if(ch=='"') {
            state=6;
            subSt=1;
          } else if(ch=='\''){
            state=6;
            subSt=2;
          }else if(ch=='/'){
            state=12;
          } else {
            throw new ArgumentException(string.Format("syntax error l:{0}, p:{1}, st:{2}", line, row, state));
          }
          break;
        case 1:     // integer, 0
          if(char.IsWhiteSpace(ch)) {
            yield return new Lexem() { typ=Lexem.LexTyp.Integer, content=text.Substring(startPos, pos-startPos), line=line, row=startRow };
            state=0;
          } else if(ch=='x' || ch=='X') {
            state=3;
          } else if(char.IsDigit(ch)) {
            state=2;
          } else if(ch=='.') {
            state=4;
          } else if(ch=='E' || ch=='e') {
            state=5;
          } else {
            throw new ArgumentException(string.Format("syntax error l:{0}, p:{1}, st:{2}", line, row, state));
          }
          break;
        case 2:   //integer
          if(char.IsWhiteSpace(ch)) {
            yield return new Lexem() { typ=Lexem.LexTyp.Integer, content=text.Substring(startPos, pos-startPos), line=line, row=startRow };
            state=0;
          } else if(char.IsDigit(ch)) {
            state=2;
          } else if(ch=='.') {
            state=4;
          } else if(ch=='E' || ch=='e') {
            state=5;
          } else {
            throw new ArgumentException(string.Format("syntax error l:{0}, p:{1}, st:{2}", line, row, state));
          }
          break;
        case 3:   //hex
          if(char.IsWhiteSpace(ch)) {
            yield return new Lexem() { typ=Lexem.LexTyp.Hex, content=text.Substring(startPos, pos-startPos), line=line, row=startRow };
            state=0;
          } else if(char.IsDigit(ch) || (ch>='A' && ch<='F') || (ch>='a' && ch<='f')) {
            state=3;
          } else {
            throw new ArgumentException(string.Format("syntax error l:{0}, p:{1}, st:{2}", line, row, state));
          }
          break;
        case 4:   //float
          if(char.IsWhiteSpace(ch)) {
            yield return new Lexem() { typ=Lexem.LexTyp.Float, content=text.Substring(startPos, pos-startPos), line=line, row=startRow };
            state=0;
          } else if(char.IsDigit(ch)) {
            state=4;
          } else if(ch=='E' || ch=='e') {
            state=5;
          } else {
            throw new ArgumentException(string.Format("syntax error l:{0}, p:{1}, st:{2}", line, row, state));
          }
          break;
        case 5:
          if(ch=='+' || ch=='-' || char.IsDigit(ch)) {
            state=4;
          } else {
            throw new ArgumentException(string.Format("syntax error l:{0}, p:{1}, st:{2}", line, row, state));
          }
          break;
        case 6:   // string
          if((subSt==1 && ch=='"') || (subSt==2 && ch=='\'')) {
            yield return new Lexem() { typ=Lexem.LexTyp.String, content=System.Text.RegularExpressions.Regex.Unescape(text.Substring(startPos+1, pos-startPos-1)), line=line, row=startRow };
            state=0;
          } else if(ch=='\\') {
            state=7;
          } else if(ch=='\t' || !char.IsControl(ch)) {
            state=6;
          } else {
            throw new ArgumentException(string.Format("syntax error l:{0}, p:{1}, st:{2}", line, row, state));
          }
          break;
        case 7: // string escaped char
          if("'\"\\nrt0".Contains(ch)) {
            state=6;
          } else if(ch=='U' || ch=='u') {
            state=8;
          } else {
            throw new ArgumentException(string.Format("syntax error l:{0}, p:{1}, st:{2}", line, row, state));
          }
          break;
        case 8: // string unicode
        case 9:
        case 10:
        case 11:
          if(char.IsDigit(ch) || (ch>='A' && ch<='F') || (ch>='a' && ch<='f')) {
            state=state==11?6:state+1;
          } else {
            throw new ArgumentException(string.Format("syntax error l:{0}, p:{1}, st:{2}", line, row, state));
          }
          break;
        case 12:   // comentar
          if(subSt==0){
            if(ch=='/') {
              subSt=1;
            } else if(ch=='*') {
              subSt=2;
            } else {
              throw new ArgumentException(string.Format("syntax error l:{0}, p:{1}, st:{2}", line, row, state));
            }
          } if(subSt==1){
            if(ch=='\n' || ch=='\r'){
              state=0;
            }
          } else if(subSt==2) {
            if(ch=='*') {
              subSt=3;
            }
          } else if(subSt==3) {
            if(ch=='/') {
              state=0;
            } else {
              subSt=2;
            }
          }
          break;
        }
        if(ch=='\n') {
          line++;
          row=1;
        } else {
          row++;
        }
        pos++;
      }
    }
  }
  public class Lexem {
    public enum LexTyp {
      Integer=1,
      Hex,
      Float,
      String,
    }
    public LexTyp typ;
    public string content;
    public int line;
    public int row;
  }

}