(function(){
  'use strict';
  const byId=id=>document.getElementById(id);
  const demoId=document.body.dataset.demo;
  if(!demoId){
    document.querySelectorAll('[data-filter]').forEach(button=>button.addEventListener('click',()=>{
      const category=button.dataset.filter;
      document.querySelectorAll('[data-filter]').forEach(b=>b.setAttribute('aria-pressed',String(b===button)));
      let count=0;
      document.querySelectorAll('[data-category]').forEach(card=>{
        card.hidden=category!=='All'&&card.dataset.category!==category;
        if(!card.hidden)count++;
      });
      byId('filter-status').textContent=count+' examples';
    }));
    return;
  }
  const d=window.GOJO_GALLERY.demos.find(d=>d.id===demoId);
  let current=0;
  function stepFromUrl(){
    const value=new URL(location.href).searchParams.get('step');
    const n=Number(value);
    return Number.isInteger(n)&&n>=0&&n<d.steps.length?n:0;
  }
  function showStep(index,updateUrl){
    current=Math.max(0,Math.min(d.steps.length-1,index));
    const s=d.steps[current];
    byId('drawing').innerHTML=window.GojoIllustrations.render(d,current);
    byId('visual-caption').textContent=s.result;
    byId('step-number').textContent='Step '+(current+1)+' of '+d.steps.length;
    byId('step-title').textContent=s.title;
    byId('step-text').textContent=s.text;
    byId('step-result').textContent=s.result;
    byId('step-keep').textContent=s.keep;
    document.querySelectorAll('[data-step]').forEach(b=>b.setAttribute('aria-pressed',String(Number(b.dataset.step)===current)));
    byId('previous-step').disabled=current===0;
    byId('next-step').disabled=current===d.steps.length-1;
    if(updateUrl){
      const url=new URL(location.href);
      if(current)url.searchParams.set('step',current);else url.searchParams.delete('step');
      history.pushState({},'',url);
    }
  }
  document.querySelectorAll('[data-step]').forEach(b=>b.addEventListener('click',()=>showStep(Number(b.dataset.step),true)));
  byId('previous-step').addEventListener('click',()=>showStep(current-1,true));
  byId('next-step').addEventListener('click',()=>showStep(current+1,true));
  window.addEventListener('popstate',()=>showStep(stepFromUrl(),false));
  const dialog=byId('syntax-dialog'), source=window.GOJO_GALLERY.sources[d.fixture];
  byId('syntax-code').textContent=source;
  byId('open-syntax').addEventListener('click',()=>{
    byId('copy-status').textContent='';
    dialog.showModal();
  });
  byId('close-syntax').addEventListener('click',()=>dialog.close());
  dialog.addEventListener('click',e=>{
    const bounds=dialog.getBoundingClientRect();
    if(e.target===dialog&&(e.clientX<bounds.left||e.clientX>bounds.right||e.clientY<bounds.top||e.clientY>bounds.bottom))dialog.close();
  });
  dialog.addEventListener('close',()=>byId('open-syntax').focus());
  byId('copy-syntax').addEventListener('click',async()=>{
    try{
      if(!navigator.clipboard)throw new Error('Clipboard unavailable');
      await navigator.clipboard.writeText(source);
      byId('copy-status').textContent='Copied.';
    }catch{
      const selection=window.getSelection(), range=document.createRange();
      range.selectNodeContents(byId('syntax-code'));selection.removeAllRanges();selection.addRange(range);
      byId('syntax-code').parentElement.focus();
      byId('copy-status').textContent='Source selected. Use Ctrl+C or Command+C to copy.';
    }
  });
  showStep(stepFromUrl(),false);
})();
