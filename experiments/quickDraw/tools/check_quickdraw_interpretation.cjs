/* Exercise the standalone local review against its retained native reads. */
const fs = require('node:fs');
const path = require('node:path');
const {pathToFileURL,fileURLToPath} = require('node:url');
const {chromium} = require(process.env.GOJO_PLAYWRIGHT_MODULE || 'C:/Users/Robin/.codex/visualizations/2026/08/07/019fdd58-cd69-7233-9ae5-8dda7bde6a4a/node_modules/playwright');
const review = path.resolve(process.argv[2] || 'D:/_quickDraw/gojo-quickdraw-structure/review.html');
const output = path.resolve(process.argv[3] || path.join(path.dirname(review), 'qa'));
const assert = (value, message) => {if (!value) throw new Error(message);};
const browserPath = process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe';
let activeBrowser;

(async()=>{
  fs.mkdirSync(output, {recursive:true});
  const browser = await chromium.launch({executablePath:browserPath, headless:true});
  activeBrowser = browser;
  const page = await browser.newPage({viewport:{width:1440,height:1200},deviceScaleFactor:1});
  const errors=[], requests=[];
  page.on('pageerror', e=>errors.push(e.message));
  await page.route('http**', route=>{requests.push(route.request().url());return route.abort();});
  await page.goto(pathToFileURL(review).href);
  const inventory=await page.evaluate(()=>REVIEW_DATA.sources.map(s=>({id:s.id,variants:s.variants.map(v=>({mode:v.mode,rungs:v.rungs.length}))})));
  assert(inventory.length===5, 'Review must contain the five curated drawings');
  const capturedBefore=await page.evaluate(()=>JSON.stringify(REVIEW_DATA.sources.map(s=>s.variants.map(v=>v.analysis))));
  let modes=0,rungs=0,paths=0,candidates=0,annotationSelections=0,annotationMarkers=0,annotationDownloads=0,nativeAnnotationDownloads=0;
  for(const drawing of inventory){
    await page.locator(`[data-source="${drawing.id}"]`).click();
    assert(drawing.variants.length===3, 'Each drawing needs three native interpretations');
    for(const variant of drawing.variants){
      modes++;
      await page.locator(`[data-mode="${variant.mode}"]`).click();
      const metricMatches=await page.evaluate(({sourceId,mode})=>{
        const v=REVIEW_DATA.sources.find(s=>s.id===sourceId).variants.find(v=>v.mode===mode);
        return document.getElementById('mode-note').textContent.includes(v.approximationAudit.orderedSampleError.rms.toFixed(1)+' RMS');
      },{sourceId:drawing.id,mode:variant.mode});
      assert(metricMatches,'Proposal difference label is not its matched native audit result');
      for(let rung=0;rung<variant.rungs;rung++){
        await page.locator('#step').evaluate((el,value)=>{el.value=String(value);el.dispatchEvent(new Event('input'));},rung);
        const checked=await page.evaluate(({sourceId,mode,index})=>{
          const source=REVIEW_DATA.sources.find(s=>s.id===sourceId),variant=source.variants.find(v=>v.mode===mode),rung=variant.rungs[index];
          const rendered=[...document.querySelectorAll('#native-canvas [data-native-edge]')];
          const em=new Map(variant.analysis.edges.map(e=>[e.id,e]));
          const expectedPath=p=>`M${p[0][0]},${p[0][1]} `+(p.length===2?`L${p[1][0]},${p[1][1]}`:p.length===3?`Q${p.slice(1).map(q=>q.join(',')).join(' ')}`:`C${p.slice(1).map(q=>q.join(',')).join(' ')}`);
          return {count:rendered.length,expected:rung.visibleEdgeIds.length,
            geometry:rendered.every(el=>rung.visibleEdgeIds.includes(el.dataset.nativeEdge)&&el.getAttribute('d')===expectedPath(em.get(el.dataset.nativeEdge).controls)),
            sourceStrokes:document.querySelectorAll('#source-canvas .source-stroke').length,expectedStrokes:source.observation.counts.strokes};
        },{sourceId:drawing.id,mode:variant.mode,index:rung});
        assert(checked.count===checked.expected&&checked.geometry, 'Displayed geometry diverged from retained native controls');
        assert(checked.sourceStrokes===checked.expectedStrokes, 'Acquired strokes disappeared from source display');
        rungs++;paths+=checked.count;
      }
      const link=await page.locator('#archive').getAttribute('href');
      assert(link && (link.startsWith('file:')||link.endsWith('.gojo.json.gz')), 'Native program download missing');
      const expectedAnnotations=await page.evaluate(({sourceId,mode})=>REVIEW_DATA.sources.find(s=>s.id===sourceId).variants.find(v=>v.mode===mode).annotations,{sourceId:drawing.id,mode:variant.mode});
      const downloading=page.waitForEvent('download');
      await page.locator('#annotation-download').click();
      const downloaded=await downloading;
      const exportedAnnotations=JSON.parse(fs.readFileSync(await downloaded.path(),'utf8'));
      assert(JSON.stringify(exportedAnnotations)===JSON.stringify(expectedAnnotations),'Downloaded annotation read differs from displayed data');
      const expectedProgramId=await page.evaluate(({sourceId,mode})=>REVIEW_DATA.sources.find(s=>s.id===sourceId).variants.find(v=>v.mode===mode).id,{sourceId:drawing.id,mode:variant.mode});
      assert(exportedAnnotations.programId===expectedProgramId,'Downloaded annotations do not identify their native program');
      annotationDownloads++;
      const nativeAnnotation=await page.evaluate(({sourceId,mode})=>REVIEW_DATA.sources.find(s=>s.id===sourceId).variants.find(v=>v.mode===mode).annotationArchivePath,{sourceId:drawing.id,mode:variant.mode});
      if(nativeAnnotation){
        assert(await page.locator('#annotation-native-download').isVisible(),'Native annotation download is hidden');
        assert(await page.locator('#annotation-native-download').getAttribute('href')===nativeAnnotation,'Native annotation download selects another variant');
        const archivePath=nativeAnnotation.startsWith('file:')?fileURLToPath(nativeAnnotation):path.resolve(path.dirname(review),nativeAnnotation);
        let archiveBytes=fs.readFileSync(archivePath);if(archivePath.endsWith('.gz'))archiveBytes=require('node:zlib').gunzipSync(archiveBytes);
        assert(JSON.parse(archiveBytes.toString('utf8')),'Native annotation archive is not valid JSON');
        nativeAnnotationDownloads++;
      }
      const hit=page.locator('#native-canvas [data-edge]').first();
      await hit.scrollIntoViewIfNeeded();
      const point=await hit.evaluate(el=>{const p=el.getPointAtLength(el.getTotalLength()/2),matrix=el.getScreenCTM();return {x:matrix.a*p.x+matrix.c*p.y+matrix.e,y:matrix.b*p.x+matrix.d*p.y+matrix.f};});
      await page.mouse.move(point.x,point.y);
      assert(await page.locator('#tooltip').isVisible(), 'Native edge has no source-support hover');
      await page.mouse.click(point.x,point.y);
      assert((await page.locator('#detail').innerText()).includes('Source support'), 'Edge click does not expose source interval');
      for(const kind of ['lines','joints','symmetries','relations']){
        await page.locator(`[data-filter="${kind}"]`).click();
        const model=await page.evaluate(({sourceId,mode,kind})=>{
          const source=REVIEW_DATA.sources.find(s=>s.id===sourceId),v=source.variants.find(v=>v.mode===mode);
          const candidates=v.annotations[kind],shown=[...document.querySelectorAll('#source-canvas [data-annotation-kind="'+kind+'"]')];
          const native=[...document.querySelectorAll('#native-canvas [data-annotation-kind="'+kind+'"]')];
          const expectedNative=candidates.filter(c=>c.read?.point);
          return {count:candidates.length,shown:shown.length,sourceIds:shown.map(e=>e.dataset.annotationId).sort(),
            expectedIds:candidates.map(c=>c.id).sort(),nativeIds:native.map(e=>e.dataset.annotationId).sort(),expectedNativeIds:expectedNative.map(c=>c.id).sort(),
            nativeLocations:native.every(e=>{const c=candidates.find(c=>c.id===e.dataset.annotationId);return Number(e.dataset.anchorX)===c.read.point[0]&&Number(e.dataset.anchorY)===c.read.point[1]}),
            special:candidates.map((c,i)=>({i,multiple:(c.candidates?.length||0)>1,failed:c.read?.status==='outside-tolerance',paired:(c.correspondences||[]).some(pair=>pair.source!==pair.target)}))};
        },{sourceId:drawing.id,mode:variant.mode,kind});
        assert(JSON.stringify(model.sourceIds)===JSON.stringify(model.expectedIds),'Some acquired annotation labels are missing: '+kind);
        assert(JSON.stringify(model.nativeIds)===JSON.stringify(model.expectedNativeIds)&&model.nativeLocations,'Proposal labels must use only qualified captured-participant reads: '+kind);
        assert(await page.locator('[data-list-candidate]').count()===model.count,'Annotation list count diverges from saved read');
        annotationMarkers+=model.shown;
        const selectedIndices=[...new Set([0,Math.floor(model.count/2),model.count-1,...model.special.filter(c=>c.multiple||c.failed||c.paired).slice(0,2).map(c=>c.i)])].filter(i=>i>=0&&i<model.count);
        for(const index of selectedIndices){
          await page.locator('[data-list-candidate]').nth(index).click();
          const detail=await page.locator('#detail').innerText();
          assert(detail.includes('Source support')&&detail.includes('Current proposal read'),'Annotation omits source support or separate proposal status');
          assert(await page.locator('#source-canvas .source-highlight').count()>0,'Annotation selection has no acquired support');
          assert(await page.locator('#native-canvas .native-edge.selected').count()>0,'Annotation selection has no mapped native participants');
          annotationSelections++;
        }
      }
    }
    for(const type of ['crosses','corners','circles','nearJoins','unresolved']){
      await page.locator(`[data-filter="${type}"]`).click();
      const count=await page.locator('[data-list-candidate]').count();
      for(let i=0;i<count;i++){
        await page.locator('[data-list-candidate]').nth(i).click();
        assert(await page.locator('#source-canvas .source-highlight').count()>0, 'Candidate has no highlighted acquired support');
        assert(await page.locator('#native-canvas .native-edge.selected').count()>0, 'Candidate has no corresponding native support');
        candidates++;
      }
    }
    if(drawing.id==='5437095780810752'){
      await page.locator('[data-filter="crosses"]').click();
      assert(await page.locator('[data-list-candidate]').count()===0, 'Clock drawing acquired an invented cross');
    }
    if(['5743197026254848','6169132418465792','5437095780810752'].includes(drawing.id)){
      await page.locator('[data-mode="gentle-priors"]').click();
      await page.locator('#layer-relations').uncheck();
      await page.locator('[data-filter="joints"]').click();
      if(drawing.id==='6169132418465792'){
        await page.locator('[data-filter="symmetries"]').click();
        if(await page.locator('[data-list-candidate]').count())await page.locator('[data-list-candidate]').first().click();
      }else{
        const preferred=await page.evaluate(id=>{
          const annotation=variant.annotations.joints.find(c=>id==='5437095780810752'?c.kind==='closed-cycle':c.label==='X');
          return annotation?'annotation-joints-'+annotation.id:null;
        },drawing.id);
        if(preferred)await page.locator(`[data-list-candidate="${preferred}"]`).click();
      }
      await page.screenshot({path:path.join(output,drawing.id+'.png'),fullPage:true});
    }
    if(drawing.id==='4578701515685888'){
      await page.locator('#layer-relations').uncheck();
      await page.locator('[data-filter="unresolved"]').click();
      await page.locator('[data-list-candidate]').first().click();
      await page.screenshot({path:path.join(output,'disagreement.png'),fullPage:true});
    }
  }
  for(const capture of [
    {sourceId:'5181133614481408',kind:'joints',label:'right-angle L',file:'right-angle.png'},
    {sourceId:'5743197026254848',kind:'joints',label:'T',file:'t-joint.png'},
    {sourceId:'4578701515685888',kind:'symmetries',file:'mirror.png'}
  ]){
    await page.locator(`[data-source="${capture.sourceId}"]`).click();
    await page.locator('[data-mode="gentle-priors"]').click();
    await page.locator('#final').click();
    await page.locator('#layer-relations').uncheck();
    await page.locator(`[data-filter="${capture.kind}"]`).click();
    const chosen=await page.evaluate(capture=>{
      const entries=variant.annotations[capture.kind];
      const selected=capture.label?entries.find(c=>(c.candidates||[]).some(candidate=>candidate.label===capture.label)):entries[0];
      return selected?'annotation-'+capture.kind+'-'+selected.id:null;
    },capture);
    assert(chosen,'Missing requested inspected annotation example: '+capture.file);
    await page.locator(`[data-list-candidate="${chosen}"]`).click();
    assert((await page.locator('#detail').innerText()).includes('Current proposal read'),'Inspected example lacks separate native read');
    await page.screenshot({path:path.join(output,capture.file),fullPage:true});
  }
  await page.locator('#overlay').check();
  assert(await page.locator('#native-canvas .overlay').count()>0,'Source overlay absent');
  await page.locator('#overlay').uncheck();
  await page.locator('#guides').uncheck();
  assert(await page.locator('#native-canvas .native-guide').count()===0,'Guide toggle ignored');
  await page.locator('#guides').check();
  const beforeLayerToggle=await page.locator('#native-canvas [data-native-edge]').evaluateAll(nodes=>nodes.map(node=>node.getAttribute('d')));
  for(const kind of ['lines','joints','symmetries','relations'])await page.locator('#layer-'+kind).uncheck();
  assert(await page.locator('[data-annotation-id]').count()===0,'An annotation layer remained visible after hiding all labels');
  assert(JSON.stringify(await page.locator('#native-canvas [data-native-edge]').evaluateAll(nodes=>nodes.map(node=>node.getAttribute('d'))))===JSON.stringify(beforeLayerToggle),'Annotation visibility changed native geometry');
  for(const kind of ['lines','joints','symmetries'])await page.locator('#layer-'+kind).check();
  const rejectsInventedCross=await page.evaluate(()=>{
    const s=REVIEW_DATA.sources[0],c=s.observation.crosses[0],v=structuredClone(s.variants[0]);
    const arm=v.analysis.edges.find(e=>e.id===v.observationMapping.spanEdges[c.spans[1]][0]);
    arm.controls=[[900,0],[900,512]];
    return crossingRead(c,v).point===null&&crossingRead(c,v).status.includes('lost');
  });
  assert(rejectsInventedCross,'An intersection outside the finite native spans was displayed as a crossing');
  await page.locator('#play').click();
  await page.waitForTimeout(950);
  assert(Number(await page.locator('#step').inputValue())>=1,'Draw-in-steps playback did not advance');
  await page.locator('#play').click();
  await page.setViewportSize({width:390,height:844});
  await page.locator('[data-source="6169132418465792"]').click();
  const overflow=await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+1);
  assert(!overflow,'Mobile page overflows horizontally');
  await page.screenshot({path:path.join(output,'mobile.png'),fullPage:true});
  assert(errors.length===0,errors.join('\n'));
  assert(requests.length===0,'Local review attempted a remote request');
  assert(await page.evaluate(()=>JSON.stringify(REVIEW_DATA.sources.map(s=>s.variants.map(v=>v.analysis))))===capturedBefore,'Inspecting or switching annotation reads mutated saved geometry');
  const result={drawings:inventory.length,modes,rungs,nativePathsChecked:paths,candidateSelections:candidates,
    nativeGeometryMatches:true,sourceSupportVisible:true,proposalMetricsMatched:true,rejectsInventedCross,
    annotationSelections,annotationMarkers,annotationDownloads,nativeAnnotationDownloads,
    annotationOnlyGeometryPreserved:true,sourceAndProposalReadSeparated:true,
    mobileOverflow:false,externalRequests:requests.length,consoleErrors:errors};
  fs.writeFileSync(path.join(output,'browser-summary.json'),JSON.stringify(result,null,2)+'\n');
  console.log(JSON.stringify(result));
  await browser.close();
})().catch(async error=>{console.error(error);if(activeBrowser)await activeBrowser.close();process.exitCode=1;});
