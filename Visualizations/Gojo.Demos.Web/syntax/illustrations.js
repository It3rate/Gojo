/* Presentation only: coordinates, labels and observed states come from the
   checked gallery catalogue. This renderer does not execute Gojo structure. */
(function (global) {
  'use strict';
  const ink='#234248', teal='#16776c', orange='#bb622f', gray='#61756e', pale='#e7eeea';
  const escape = s => String(s).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const text=(x,y,s,color=ink,size=18,anchor='start') => '<text x="'+x+'" y="'+y+'" fill="'+color+'" font-size="'+size+'" text-anchor="'+anchor+'">'+escape(s)+'</text>';
  const line=(x,y,a,b,color=teal,width=3,dash=false) => '<path d="M'+x+' '+y+' L'+a+' '+b+'" fill="none" stroke="'+color+'" stroke-width="'+width+'"'+(dash?' stroke-dasharray="7 6"':'')+'/>';
  const dot=(x,y,color=teal,r=7,open=false) => '<circle cx="'+x+'" cy="'+y+'" r="'+r+'" fill="'+(open?'#fcfaf4':color)+'" stroke="'+color+'" stroke-width="3"/>';
  const rect=(x,y,w,h,fill=pale) => '<rect x="'+x+'" y="'+y+'" width="'+w+'" height="'+h+'" rx="12" fill="'+fill+'"/>';
  const path=(points,color=teal,width=3,close=false,dash=false) => '<path d="M'+points.map(p=>p.join(' ')).join(' L')+(close?' Z':'')+'" fill="'+(close?color+'14':'none')+'" stroke="'+color+'" stroke-width="'+width+'"'+(dash?' stroke-dasharray="7 6"':'')+'/>';
  const chip=(x,y,w,title,value,color=teal)=>rect(x,y,w,80)+text(x+18,y+29,title,gray,14)+text(x+18,y+58,value,color,String(value).length>18?17:22);
  const arrow=(x,y,a,b,color=teal)=>line(x,y,a,b,color)+path([[a-8,b-5],[a,b],[a-8,b+5]],color,2);
  function axes(map,xMin,xMax,yMin,yMax) {
    const a=map([xMin,0]),b=map([xMax,0]),c=map([0,yMin]),d=map([0,yMax]);
    return line(...a,...b,'#cad4ce',1)+line(...c,...d,'#cad4ce',1);
  }
  function curve(controls,map,color=teal,dash=false) {
    const p=controls.map(map);
    return '<path d="M'+p[0].join(' ')+' C'+p.slice(1).map(v=>v.join(' ')).join(' ')+'" fill="none" stroke="'+color+'" stroke-width="4"'+(dash?' stroke-dasharray="7 6"':'')+'/>';
  }
  function curveFigure(d,s) {
    const f=d.facts,replay=d.id==='curve-replay';
    const map=p=>[90+p[0]*45,365-p[1]*(replay?18:32)];
    let out=axes(map,-.7,12.7,-.5,replay?17:9);
    const controls=replay&&s>0?f.edited:f.controls;
    out+=curve(f.controls,map, s>0&&replay?gray:teal,s>0&&replay);
    if(replay&&s===3) out+=curve(f.edited,map);
    out+=path(controls.map(map),gray,2);
    controls.forEach((p,i)=>{ const q=map(p);out+=dot(...q,teal,6)+text(q[0]+9,q[1]-12,'P'+i,ink,16); });
    if(replay){
      const historical=map(f.old[5]);
      if(s>0)out+=dot(...historical,gray,8,true)+text(historical[0]+14,historical[1]+26,'old sample',gray,15);
      if(s===0)out+=dot(...historical,orange,9)+text(historical[0]+14,historical[1]-13,'(6, 7/2)',orange);
      if(s===1)out+=chip(85,36,235,'CURRENT SAMPLE','Pending',orange);
      if(s===2) {
        f.fresh.slice(0,3).forEach((p,i)=>{const q=map(p);out+=dot(...q,i===2?gray:orange,7)+text(q[0]-12,q[1]-16,'q'+i,i===2?gray:orange,16);});
        out+=text(92,55,'q0, q1 refreshed · q2 carried',ink,17);
      }
      if(s===3) {
        const q=map(f.fresh[5]);out+=dot(...q,orange,9)+text(q[0]+16,q[1]-14,'(6, 13/2)',orange,20);
      }
    } else {
      const colors=[orange,'#587cac',teal];
      f.levels.forEach((points,i)=>{
        if(s>i) {
          const p=points.map(map);
          if(p.length>1)out+=path(p,colors[i],2);
          p.forEach((q,j)=>{out+=dot(...q,colors[i],i===2?9:6)+text(q[0]+10,q[1]-12,(i===0?'q':i===1?'r':'sample')+(i<2?j:''),colors[i],15);});
        }
      });
      out+=text(88,42,'Shared parameter: t = 1/2',gray,17);
    }
    return out;
  }
  function render(d,s=0,thumbnail=false){
    const f=d.facts; let out='';
    switch(d.id){
      case 'bezier-construction': case 'curve-replay': out=curveFigure(d,s);break;
      case 'shared-sites': {
        const pos=n=>100+(n+4)*38, joint=s===1?f.moved:f.joint;
        out+=text(80,52,s===2?'Ordered frame, reversed':'Shared site versus equal coordinate',gray,18);
        if(s===2){
          out+=arrow(pos(f.start),145,pos(f.joint),145)+text(80,115,'Original',gray,16);
          out+=line(pos(f.joint),260,pos(f.start),260,orange)+path([[pos(f.start)+9,255],[pos(f.start),260],[pos(f.start)+9,265]],orange,2);
          out+=text(80,230,'Reversed',gray,16);
          [f.start,f.joint].forEach(n=>{out+=dot(pos(n),145)+dot(pos(n),260,orange)+text(pos(n),305,n,ink,18,'middle');});
        }else{
          out+=path([[pos(f.start),150],[pos(joint),150],[pos(f.end),150]],teal,5);
          out+=dot(pos(f.start),150)+dot(pos(joint),150,orange,10)+dot(pos(f.end),150);
          out+=text(pos(joint),115,'shared: '+joint,orange,18,'middle');
          out+=arrow(pos(f.lookalike),290,pos(f.end),290,gray)+dot(pos(f.lookalike),290,gray,9,true);
          out+=text(pos(f.lookalike),334,'separate tick: '+f.lookalike,gray,18,'middle');
          out+=text(80,195,'two connected segments',ink,17);
          if(s===1)out+=line(pos(f.joint),150,pos(f.joint),290,gray,1,true);
        } break;
      }
      case 'parameter-frames':{
        const t=s===0?f.samples[0]:f.samples[1], v=s===0?f.positions[0]:f.positions[1];
        const x=90+t*5.3;
        out+=text(80,60,'Parameter frame',gray,18)+line(90,120,620,120,gray,2)+dot(x,120)+text(x,95,t,teal,24,'middle');
        out+=text(90,151,'0',gray)+text(620,151,'100',gray,18,'end');
        out+=line(x,165,x,245,orange,2,true);
        out+=text(80,225,'Rail frame',gray,18)+line(90,290,620,290,gray,2)+dot(x,290,orange)+text(x,270,v,orange,24,'middle');
        out+=text(90,325,'10',gray)+text(620,325,'210',gray,18,'end');
        if(s===2)out+=text(350,385,'47 = 4 coarse ticks × 10 + 7 retained ticks',ink,19,'middle');
        break;
      }
      case 'partial-construction': {
        const map=p=>[220+p[0]*105,352-p[1]*73];
        out=axes(map,-1.5,3.7,-.2,4.2);
        if(s!==0)out+=path([f.a,f.b].map(map),gray,2);
        let a=map(f.a);out+=dot(...a)+text(a[0]-18,a[1]+32,'A (−1, 2)',ink,17);
        if(s===0){
          const x=map([2,0])[0];out+=line(x,65,x,337,orange,2,true)+text(x+12,64,'B (2, ?)',orange,17);
          out+=line(220,80,220,335,teal,3,true)+text(200,390,'sample: x = 0, y open',teal,18);
        }else{
          const b=map(f.b);out+=dot(...b)+text(b[0]-10,b[1]-18,'B (2, 3)',ink,17);
          if(s===1){const q=map([0,7/3]);out+=dot(...q,orange,9)+text(q[0]-20,q[1]-23,'(0, 7/3)',orange,20);}
          if(s===2)out+=chip(292,302,310,'SAMPLE 4 · SUPPORT 0…3','Read remains open',orange);
          if(s===3){const q=map([3,10/3]);out+=line(...b,...q,orange,2,true)+dot(...q,orange,9)+text(q[0]-80,q[1]-27,'(3, 10/3)',orange,20);}
        }break;
      }
      case 'curve-joins': {
        const map=p=>[85+p[0]*87,260-p[1]*85];
        out=axes(map,-.3,6.5,-.2,1.8)+curve(f.left,map)+curve(f.right,map,orange);
        out+=path(f.left.map(map),gray,1,true)+path(f.right.map(map),gray,1,true);
        const q=map([3,0]);out+=dot(...q,teal,10)+text(q[0],304,'shared joint (3, 0)',ink,17,'middle');
        if(s===0)out+=chip(75,38,280,'POSITION DISPLACEMENT','(0, 0)');
        if(s===1)out+=chip(75,38,250,'INCOMING DERIVATIVE','(3, 0)')+chip(385,38,250,'OUTGOING DERIVATIVE','(3, 0)',orange);
        if(s===2)out+=chip(75,38,250,'INCOMING SECOND','(0, 0)')+chip(385,38,250,'OUTGOING SECOND','(0, 6)',orange)+text(350,370,'C1 holds here · C2 does not',orange,20,'middle');
        break;
      }
      case 'frieze-patterns': {
        const map=p=>[s===3?348+p[0]*17:78+p[0]*34,210-p[1]*38];
        out=axes(map,s===3?-16:0,s===3?17:17,-4,4);
        const motif=(p,color)=>path(p.map(map),color,3,true)+p.map(q=>dot(...map(q),color,4)).join('');
        if(s===3){
          for(let i=-2;i<=2;i++)out+=motif(f.original.map(p=>[p[0]+i*f.shift,p[1]*(i%2?-1:1)]),i%2?orange:teal);
          out+=text(350,395,'Visible cells −2…2 · overall support remains open',gray,18,'middle');
        }else{
          out+=motif(f.original,teal);
          if(s>0)out+=motif(f.glide,orange);
          if(s>1)out+=motif(f.twice,'#587cac');
          out+=text(80,55,s===0?'Three shared corners':s===1?'One glide: +6, reflect y':'Two glides = one translation by 12',ink,20);
          if(s>0)out+=arrow(225,350,s===2?610:405,350,gray);
        }break;
      }
      case 'uncertain-cups': {
        const cup=(x,label,color,other=false)=>'<path d="M'+x+' 118 L'+(x+14)+' 262 Q'+(x+70)+' 281 '+(x+126)+' 262 L'+(x+140)+' 118" fill="'+color+'14" stroke="'+color+'" stroke-width="3"/>'+line(x+8,158,x+132,158,color,2,true)+line(x+12,183,x+128,183,color,2,true)+text(x+70,310,label,color,20,'middle')+(other?text(x+70,338,'separate quantity slot',gray,14,'middle'):'');
        out+=cup(95,'A · 0.9…1.1 cups',teal);
        if(s===0)out+=chip(340,163,285,'CONNECTED SUPPORT','[90, 110] fine ticks');
        if(s===1){out+=text(310,213,'−',gray,36,'middle')+cup(425,'the same A',teal)+line(160,82,495,82,teal,2,true)+text(330,62,'same unresolved cause',gray,17,'middle');}
        if(s===2)out+=text(310,213,'−',gray,36,'middle')+cup(425,'B · 0.9…1.1 cups',orange,true);
        if(s===3){out=chip(75,65,255,'OBSERVATION A','100 + q + b + εa')+chip(385,65,255,'OBSERVATION B','100 + q + b + εb')+line(200,175,510,175,teal,2,true)+text(350,204,'shared quantity q and bias b cancel',gray,18,'middle')+chip(205,247,300,'RETAINED DIFFERENCE','εa − εb ∈ [−2, 2]')+text(350,370,'All values in hundredths of a cup',gray,17,'middle');}
        if(s===1||s===2)out+=text(350,391,s===1?'A − A = 0':'A − B ∈ [−0.2, 0.2] cups',s===1?teal:orange,24,'middle');
        break;
      }
      case 'clearance': {
        // Cross section is schematic: the gap is enlarged so it is visible.
        out+='<circle cx="176" cy="199" r="105" fill="#e5ece6" stroke="'+teal+'" stroke-width="3"/><circle cx="176" cy="199" r="83" fill="#f5ddc7" stroke="'+orange+'" stroke-width="3"/>';
        out+=text(176,205,'bolt',orange,24,'middle')+text(176,345,'Gap enlarged for visibility',gray,15,'middle');
        out+=chip(340,70,290,'SUPPORTED CLEARANCE','0.10…0.30 mm');
        if(s===0)out+=text(350,205,'Bolt: 9.90…10.00 mm',ink,18)+text(350,240,'Hole: 10.10…10.20 mm',ink,18);
        else {
          const low=s===1?.1:.15, high=s===1?.3:.25, x=n=>340+n*850;
          out+=text(340,201,'Actual support',gray,16)+line(x(.1),225,x(.3),225,teal,11);
          out+=text(340,269,'Allowed clearance',gray,16)+line(x(low),293,x(high),293,orange,11);
          out+=text(340,350,s===1?'All supported states fit':'Some states remain outside',s===1?teal:orange,18);
        }break;
      }
      case 'material-refinement':{
        const [packs,units,tension]=f.states[s];
        out+=text(80,55,'Packs · 10 units each',gray,18)+text(385,55,'Loose units',gray,18);
        for(let i=0;i<packs;i++)out+=rect(80+i*76,99,62,95,'#d4e8de')+text(111+i*76,155,'10',teal,24,'middle');
        if(!packs)out+=text(95,150,'0',gray,24);
        for(let i=0;i<units;i++)out+=rect(386+(i%5)*40,98+Math.floor(i/5)*38,28,25,'#efcfae');
        if(!units)out+=text(403,147,'0',gray,24);
        out+=chip(80,304,250,'PACKS',packs+' × 10')+chip(382,304,250,tension?'UNPAID FINE DRAW':'LOOSE UNITS',tension?'tension '+tension:String(units),tension?orange:teal);
        break;
      }
      case 'local-blend':{
        out+=chip(58,52,220,'LIGHT INPUT',String(f.light))+chip(58,192,220,'HEAT INPUT',String(f.heat));
        out+=arrow(278,92,418,136)+text(317,77,'× 1/2',gray,16);
        out+=arrow(278,232,418,182,orange)+text(314,272,'× '+f.gains[s]+'/10',gray,16);
        out+=rect(423,104,206,130)+text(526,146,'Prediction',gray,16,'middle')+text(526,200,f.outputs[s],teal,40,'middle');
        out+=text(526,279,'Requested: '+f.target,ink,18,'middle');
        out+=chip(58,325,282,'ALTERNATIVE RETAINED','Prediction '+f.alternative,gray);
        out+=chip(366,325,268,s===2?'ROUTE REMAINDER':'CLAIM MISMATCH',s===2?'1 error tick':String(f.target-f.outputs[s]),orange);
        break;
      }
      case 'unit-standards':{
        const tokens=s<2?(s===0?['mile','hour','hour']:['hour','mile','hour']):s===2?['litre','km','100 km']:['litre','100 km','100 km'];
        out+=text(350,69,s<2?'A selected dimensional cancellation':'The standard keeps its tick scale',gray,19,'middle');
        out+=chip(100,111,215,'NUMERATOR',tokens[0])+text(350,159,'×',gray,26,'middle')+chip(385,111,215,'NUMERATOR',tokens[1]);
        out+=line(100,218,600,218,ink,2)+chip(242,246,215,'DENOMINATOR',tokens[2]);
        if(s!==2){const x=s===1?100:385;out+=line(x+18,171,x+191,139,orange,2)+line(260,306,438,273,orange,2);}
        out+=text(350,386,s===2?'Calibration still needed':s<2?'mile · matched pair recorded':'litre · standard retained',s===2?orange:teal,23,'middle');
        break;
      }
      default: throw new Error('Missing illustration: '+d.id);
    }
    return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 700 440" role="img" aria-label="'+escape(d.title+' — '+d.steps[s].title)+'" font-family="system-ui, sans-serif"><title>'+escape(d.title+' — '+d.steps[s].title)+'</title><rect width="700" height="440" fill="#fcfaf4"/>'+out+'</svg>';
  }
  global.GojoIllustrations={render};
})(globalThis);
