(() => {
  'use strict';
  const $ = id => document.getElementById(id);
  const html = s => String(s).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const num = n => Number.isInteger(n) ? String(n) : Number(n.toFixed(3)).toString();
  let report, current, matches=[], index=0;
  const frames = {'identity':'identity', 'quarter-left':'quarter-turn left', 'quarter-right':'quarter-turn right', 'half-turn':'half-turn', 'vertical-mirror':'vertical mirror', 'horizontal-mirror':'horizontal mirror', 'rising-diagonal':'rising diagonal', 'falling-diagonal':'falling diagonal'};
  const describe = m => `${m.source} → ${m.target} · ${frames[m.frame]} · ${m.reversed ? 'backward' : 'forward'}`;
  const phase = (n,len) => ((n%len)+len)%len;
  function picked(m) {
    const kind=$('read-kind').value;
    if(kind==='equal') return {n:m.equalRun,p:1,q:1};
    if(kind==='proportion') return {n:m.similarRun,p:m.scaleNumerator,q:m.scaleDenominator};
    if(kind==='direction') return {n:m.directionRun,p:null,q:null};
    return {n:m.cut.run,p:m.cut.scaleNumerator,q:m.cut.scaleDenominator};
  }
  function pathVectors(m,target) {
    const read=picked(m), n=current.segments.length, list=[];
    for(let k=0;k<read.n;k++) {
      const slot=phase((target?m.target:m.source)+(target&&m.reversed?-k:k),n);
      const item=current.segments[slot], sense=target&&m.reversed?-1:1;
      let x=item.x*sense,y=item.y*sense,length=Math.abs(x)+Math.abs(y),extent=length;
      if($('read-kind').value==='direction') extent=1;
      if($('read-kind').value==='cut'&&(k===0||k===read.n-1)) {
        const lim=k===0?m.cut.firstLimitNumerator:m.cut.lastLimitNumerator;
        extent=lim/(target?read.q:read.p);
      }
      list.push({x:x*extent/length,y:y*extent/length,slot,extent});
    }
    return list;
  }
  function miniSvg(vectors,color,name) {
    const points=[[0,0]];
    for(const v of vectors) {const p=points.at(-1);points.push([p[0]+v.x,p[1]+v.y]);}
    const xs=points.map(p=>p[0]),ys=points.map(p=>p[1]);
    const minX=Math.min(...xs),maxX=Math.max(...xs),minY=Math.min(...ys),maxY=Math.max(...ys);
    const scale=Math.min(370/Math.max(1,maxX-minX),155/Math.max(1,maxY-minY));
    const px=(420-(maxX-minX)*scale)/2,py=(195-(maxY-minY)*scale)/2;
    const project=p=>[px+(p[0]-minX)*scale,py+(maxY-p[1])*scale];
    const screen=points.map(project);
    let result=`<svg viewBox="0 0 420 195" role="img" aria-label="${html(name)}"><polyline points="${screen.map(p=>p.join(',')).join(' ')}" fill="none" stroke="${color}" stroke-width="4"/>`;
    for(let i=0;i<vectors.length;i++) {
      const a=screen[i],b=screen[i+1],dx=b[0]-a[0],dy=b[1]-a[1],l=Math.hypot(dx,dy),ux=dx/l,uy=dy/l;
      const x=(a[0]+b[0])/2,y=(a[1]+b[1])/2,s=5;
      const triangle=[[x+ux*s,y+uy*s],[x-ux*s+uy*s*.7,y-uy*s-ux*s*.7],[x-ux*s-uy*s*.7,y-uy*s+ux*s*.7]];
      result+=`<polygon points="${triangle.map(p=>p.join(',')).join(' ')}" fill="${color}"/><text x="${x+(dy?12:0)}" y="${y+(dx?-10:4)}" text-anchor="middle" font-family="system-ui" font-size="15" fill="${color}">${vectors[i].slot}</text>`;
    }
    result+=`<circle cx="${screen[0][0]}" cy="${screen[0][1]}" r="5" fill="#fffdf7" stroke="${color}" stroke-width="2"/></svg>`;
    return result;
  }
  function showMatch() {
    $('witness-count').textContent=matches.length?`${index+1} of ${matches.length} witnesses`:'No witnesses in this read';
    $('previous-witness').disabled=index===0||!matches.length;
    $('next-witness').disabled=index>=matches.length-1;
    if(!matches.length){$('witness-content').innerHTML='<p class="empty">This combination has no recorded match. Try another transform or a weaker read.</p>';return;}
    const m=matches[index],r=picked(m),kind=$('read-kind').value;
    const source=pathVectors(m,false),target=pathVectors(m,true);
    const scale=r.p==null?'Extents omitted':`Target/source scale ${r.p}:${r.q}`;
    const lengthMessage=kind==='direction'?'Every reach is drawn as one display step here. This comparison preserves heading order while discarding extent.':kind==='cut'?'Outer arms end at the largest jointly supported cuts; smaller positive cuts also match. Interior reaches are complete.': 'Every reach in this comparison is complete. Each panel fits its own bounds; read the scale ratio instead of comparing panel widths.';
    $('witness-content').innerHTML=`<p class="witness-title">${html(describe(m))}</p><p>${r.n} reaches · ${scale}. Source is transformed, then compared to the target in the stated traversal.</p><div class="window-pair"><div class="window-box"><h5 class="source-ink">Source · authored direction</h5>${miniSvg(source,'#b04732','Source window in authored direction')}<p>Slots ${source.map(v=>v.slot).join(' · ')}</p></div><div class="window-box"><h5 class="target-ink">Target · ${m.reversed?'backward comparison':'authored direction'}</h5>${miniSvg(target,'#215ca8',m.reversed?'Target window read opposite to the pen':'Target window in authored direction')}<p>Slots ${target.map(v=>v.slot).join(' · ')}</p></div></div><p class="witness-limit">${lengthMessage} Open circles mark comparison starts. ${m.reversed?'Blue arrows oppose the target pen direction; they describe this read.':'Both sets of arrows follow their source pen.'}</p>`;
  }
  function filterMatches() {
    const transform=$('frame-filter').value,traversal=$('traversal-filter').value;
    matches=current.windows.matches.filter(m=>picked(m).n>=3 && (transform==='all'||m.frame===transform) && (traversal==='all'||m.reversed===(traversal==='backward')) && !(!m.reversed&&m.source===m.target&&m.frame==='identity'));
    matches.sort((a,b)=>picked(b).n-picked(a).n||a.source-b.source||a.target-b.target||a.frame.localeCompare(b.frame));
    index=0;
    const limit=matches.length;
    $('witness-list').innerHTML=matches.length?`<div class="table-scroll witness-list"><table><thead><tr><th>Inspect</th><th>Source → target</th><th>Transform</th><th>Read</th><th>Reaches</th><th>Scale</th></tr></thead><tbody>${matches.slice(0,limit).map((m,i)=>{const r=picked(m);return `<tr><td><button type="button" data-witness="${i}" aria-label="Inspect witness ${i+1}">View</button></td><td>${m.source} → ${m.target}</td><td>${html(frames[m.frame])}</td><td>${m.reversed?'backward':'forward'}</td><td>${r.n}</td><td>${r.p==null?'omitted':r.p+':'+r.q}</td></tr>`;}).join('')}</tbody></table></div>`:'<p>No rows for these filters.</p>';
    showMatch();
  }
  function selectPattern(id,updateHash=true) {
    current=report.patterns.find(p=>p.id===id)||report.patterns[0];
    $('pattern-select').value=current.id;
    document.querySelectorAll('.pattern-panel').forEach(panel=>panel.hidden=panel.dataset.pattern!==current.id);
    document.querySelectorAll('[data-pick]').forEach(link=>link.setAttribute('aria-current',String(link.dataset.pick===current.id)));
    $('witness-pattern').textContent=current.name;
    if(updateHash)history.replaceState(null,'',`#pattern-${current.id}`);
    filterMatches();
  }
  function stepPattern(dir) {const n=report.patterns.length;selectPattern(report.patterns[phase(report.patterns.indexOf(current)+dir,n)].id);}
  async function start() {
    const response=await fetch('study-data.json');if(!response.ok)throw new Error('Recorded data unavailable');report=await response.json();
    document.documentElement.classList.add('js');
    $('pattern-select').addEventListener('change',e=>selectPattern(e.target.value));
    $('pattern-select').addEventListener('keydown',e=>{if(['ArrowLeft','ArrowRight','ArrowUp','ArrowDown'].includes(e.key)){e.preventDefault();stepPattern(['ArrowLeft','ArrowUp'].includes(e.key)?-1:1);}});
    $('previous-pattern').addEventListener('click',()=>stepPattern(-1));$('next-pattern').addEventListener('click',()=>stepPattern(1));
    document.querySelectorAll('[data-pick]').forEach(link=>link.addEventListener('click',e=>{e.preventDefault();selectPattern(link.dataset.pick);$('inspector').scrollIntoView({block:'start'});}));
    document.querySelectorAll('[data-layer]').forEach(input=>input.addEventListener('change',()=>document.body.classList.toggle('no-'+input.dataset.layer,!input.checked)));
    ['read-kind','frame-filter','traversal-filter'].forEach(id=>$(id).addEventListener('change',filterMatches));
    $('previous-witness').addEventListener('click',()=>{index=Math.max(0,index-1);showMatch();});$('next-witness').addEventListener('click',()=>{index=Math.min(matches.length-1,index+1);showMatch();});
    $('witness-list').addEventListener('click',e=>{const button=e.target.closest('[data-witness]');if(button){index=Number(button.dataset.witness);showMatch();$('witness-content').scrollIntoView({block:'nearest'});}});
    const id=location.hash.replace('#pattern-','');selectPattern(id,false);
  }
  start().catch(error=>{console.error(error);const note=document.createElement('p');note.className='notice';note.textContent='Interactive controls could not load. All recorded diagrams and notes remain below; the JSON download contains the complete witnesses.';$('inspector').prepend(note);});
})();
