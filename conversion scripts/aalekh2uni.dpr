program vrideva2uni;

{$APPTYPE CONSOLE}

uses
  SysUtils,windows,classes,tntclasses;

var
  line:integer;
  convtbl:array [0..255] of widestring;



procedure buildtbl;
var
  i:integer;
begin
  for i:=$0 to $7f do begin
    convtbl[i]:=widechar(i);
  end;

  for i:=$80 to high(convtbl) do begin
    convtbl[i]:='?';
  end;

  convtbl[$81]:=#$924;
    convtbl[$81]:=convtbl[$81]+#$94d;convtbl[$81]:=convtbl[$81]+#$924;

  convtbl[$83]:=#$937;
    convtbl[$83]:=convtbl[$83]+#$94d;convtbl[$83]:=convtbl[$83]+#$91f;
    convtbl[$83]:=convtbl[$83]+#$94d;convtbl[$83]:=convtbl[$83]+#$930;

  convtbl[$84]:=#$938;
    convtbl[$84]:=convtbl[$84]+#$94d;convtbl[$84]:=convtbl[$84]+#$930;

  convtbl[$87]:=#$915;
    convtbl[$87]:=convtbl[$87]+#$94d;convtbl[$87]:=convtbl[$87]+#$937;

  convtbl[$88]:=#$926;
    convtbl[$88]:=convtbl[$88]+#$94d;convtbl[$88]:=convtbl[$88]+#$935;

  convtbl[$89]:=#$93c;

  convtbl[$8A]:=#$91c;
    convtbl[$8a]:=convtbl[$8a]+#$94d;convtbl[$8a]:=convtbl[$8a]+#$91e;

  convtbl[$8C]:=#$938;
    convtbl[$8c]:=convtbl[$8c]+#$94d;convtbl[$8c]:=convtbl[$8c]+#$924;

  convtbl[$A1]:='!';
  convtbl[$A2]:='';
  convtbl[$A3]:='#';
  convtbl[$A4]:='$';
  convtbl[$A5]:='%';
  convtbl[$A6]:='&';
  convtbl[$A7]:='';
  convtbl[$A8]:='(';
  convtbl[$A9]:=')';
  convtbl[$AA]:='*';
  convtbl[$AB]:='+';

  convtbl[$AC]:=','; //yap
  convtbl[$AD]:='-'; //yap
  convtbl[$ae]:='.';


  convtbl[$af]:='/';

  convtbl[$b0]:=#$966;
  convtbl[$b1]:=#$967;
  convtbl[$b2]:=#$968;
  convtbl[$b3]:=#$969;
  convtbl[$b4]:=#$96a;
  convtbl[$b5]:=#$96b;
  convtbl[$b6]:=#$96c;
  convtbl[$b7]:=#$96d;
  convtbl[$b8]:=#$96e;
  convtbl[$b9]:=#$96f;
  convtbl[$ba]:=#$903;

  convtbl[$bb]:=';';

  convtbl[$bc]:='<';
  convtbl[$bd]:='=';
  convtbl[$be]:='>';
  convtbl[$bf]:='?';
  convtbl[$c0]:='@';

  convtbl[$c3]:=#$901;
  convtbl[$c4]:=#$902;


  convtbl[$c6]:=#$905;
  convtbl[$c7]:=#$906;
  convtbl[$c8]:=#$907;
  convtbl[$c9]:=#$908;
  convtbl[$ca]:=#$909;
  convtbl[$cb]:=#$90a;
  convtbl[$cc]:=#$90b;
  convtbl[$cd]:=#$90f;
  convtbl[$ce]:=#$910;
  convtbl[$cf]:=#$913;
  convtbl[$d0]:=#$914;
  convtbl[$d1]:=#$93c;
  convtbl[$d2]:=#$938;
    convtbl[$d2]:=convtbl[$d2]+#$94d;
    convtbl[$d2]:=convtbl[$d2]+#$924;
    convtbl[$d2]:=convtbl[$d2]+#$94d;
    convtbl[$d2]:=convtbl[$d2]+#$924;
  convtbl[$d3]:=#$915;
  convtbl[$d4]:=#$916;
  convtbl[$d5]:=#$917;
  convtbl[$d6]:=#$918;
  convtbl[$d7]:=#$919;
  convtbl[$d8]:=#$91a;
  convtbl[$d9]:=#$91b;
  convtbl[$da]:=#$91c;
  convtbl[$db]:='[';
  convtbl[$dc]:='\';
  convtbl[$dd]:=']';
  convtbl[$de]:='^';
  convtbl[$df]:='_';
  convtbl[$e0]:=#$2018;
  convtbl[$a7]:=#$2019;
  convtbl[$e1]:=#$91d;
  convtbl[$e2]:=#$91e;
  convtbl[$e3]:=#$91f;
  convtbl[$e4]:=#$920;
  convtbl[$e5]:=#$921;
  convtbl[$e6]:=#$922;
  convtbl[$e7]:=#$923;
  convtbl[$e8]:=#$924;
  convtbl[$e9]:=#$925;
  convtbl[$ea]:=#$926;
  convtbl[$eb]:=#$927;
  convtbl[$ec]:=#$928;
  convtbl[$ed]:=#$92a;
  convtbl[$ee]:=#$92b;
  convtbl[$ef]:=#$92c;
  convtbl[$f0]:=#$92d;
  convtbl[$f1]:=#$92e;
  convtbl[$f2]:=#$92f;
  convtbl[$f3]:=#$930;
  convtbl[$f4]:=#$932;
  convtbl[$f5]:=#$933;
  convtbl[$f6]:=#$935;
  convtbl[$f7]:=#$936;
  convtbl[$f8]:=#$937;
  convtbl[$f9]:=#$938;
  convtbl[$fa]:=#$939;
  convtbl[$fb]:='}';
  convtbl[$fc]:='|';
  convtbl[$fd]:='{';
  convtbl[$fe]:='~';

end;


function isConsonant(c:widestring):boolean;
var
  v:integer;
begin
  result:=false;
//  if length(c)>1 then exit;
  if length(c)=0 then exit;
  v:=integer(c[length(c)]);
  result:=((v>=$915) and (v<=$939)) or ((v>=$958) and (v<=$95F));
end;

function isVowel(c:widestring):boolean;
var
  v:integer;
begin
  result:=false;
//  if length(c)>1 then exit;
  if length(c)=0 then exit;
  v:=integer(c[ length(c)]);
  result:=(v>=$904) and (v<=$914);
end;

function getshortvowel(c:widechar):widestring;
begin
  result:='';
  case c of
    #$905: result:=''; //a
    #$906: result:=#$93E; //A
    #$907: result:=#$93F; //i
    #$908: result:=#$940; //I
    #$909: result:=#$941; //u
    #$90a: result:=#$942; //U
    #$90F: result:=#$947; //e
    #$910: result:=#$948; //ai
    #$90b: result:=#$943; //R
    #$90e: result:=#$944; //RR
    #$90c: result:=#$962; //LR
    #$961: result:=#$963; //LRR
    #$913: result:=#$94b; //o
    #$914: result:=#$94c; //au
  end;
end;
function getunderdot(c:widechar):widechar;
begin
  result:=#0;
  case c of
    #$928:result:=#$929;
    #$930:result:=#$931;
    #$921:result:=#$95c; //#$919
    #$933:result:=#$934;
    #$916:result:=#$959;
    #$915:result:=#$958;
    #$917:result:=#$95a;
    #$922:result:=#$95d;
    #$92b:result:=#$95e;
    #$92f:result:=#$95f;
  end;
end;
function processinstruction(var p:pchar):widestring;
var
  ins:string;
  idx:integer;
begin
  inc(p);
  result:='';

  if (p^='g') and ((p+1)^='C') then begin
    inc(p,2);
    result:=result+#$964;
  end;
end;
function convVri(p:pchar):widestring;
var
  lastc,c:widestring;
  i:integer;
  underdot:widechar;
begin
  result:='';
  lastc:='';
  while p^<>#0 do begin
    if p^=#$1b then begin
      lastc:='';
      result:=result+processinstruction(p);
      continue;
    end;

    c:=convtbl[integer(p^)];
    underdot:=#0;
    if c=#$93c then begin
   //   if lastc<>'' then
      underdot:=getunderdot(lastc[1]);
    end;

    if underdot<>#0 then begin
      c:=underdot;
      result:=copy(result,1,length(result)-1);
    end else if isConsonant(lastc) then begin
      if isVowel(c) then begin
        c:=getshortvowel(c[1]);
      end else if isConsonant(c) then begin
        c:=#$94d+c;
      end;
    end;
    result:=result+c;
    lastc:=c;
    inc(p);
  end;
end;
var
  input,output:string;
  sl:ttntstringlist;
  i:integer;
  p:pchar;
  s:string;
  ws:widestring;
  infile:textfile;


begin
  buildtbl;
//  inittbl;

  if Paramcount=0 then begin
    writeLn('Aalekh to Unicode, Pali characters conversion (2007/4/9)');
    writeLn('syntax:');
    writeln('aalekh2uni input [output]');
    exit;
  end;

  input:=paramstr(1);output:=input+'.txt';
  if paramcount>1 then output:=paramstr(2);

  sl:=ttntstringlist.create;

  assignfile(infile,input);
  reset(infile);

  i:=0;
  line:=0;
  repeat
    inc(line);
    readln(infile,s);
    p:=pchar(s);
    sl.add(convvri(p));

//    writeln(i);
    inc(i);
  until eof(infile);

  inc(line);
  write(line);

  sl.SaveToFile(output);
  sl.free;


end.
