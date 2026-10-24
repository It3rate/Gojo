/* Verify that the local Gestalt viewer observes saved native passes only. */
const fs=require('node:fs'),path=require('node:path'),zlib=require('node:zlib'),crypto=require('node:crypto');
const {pathToFileURL,fileURLToPath}=require('node:url');
const {chromium}=require(process.env.GOJO_PLAYWRIGHT_MODULE||'C:/Users/Robin/.codex/visualizations/2026/08/07/019fdd58-cd69-7233-9ae5-8dda7bde6a4a/node_modules/playwright');
const review=path.resolve(process.argv[2]||'D:/_quickDraw/gojo-quickdraw-gestalt/review.html');
const output=path.resolve(process.argv[3]||path.join(path.dirname(review),'qa'));
const assert=(value,message)=>{if(!value)throw new Error(message)};
const sha256=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
let browser;
(async()=>{
 fs.mkdirSync(output,{recursive:true});
 browser=await chromium.launch({executablePath:process.env.GOJO_BROWSER||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true});
 const page=await browser.newPage({viewport:{width:1440,height:1180},deviceScaleFactor:1}),errors=[],requests=[];
 page.on('pageerror',error=>errors.push(error.message));await page.route('http**',route=>{requests.push(route.request().url());return route.abort()});
 await page.goto(pathToFileURL(review).href);
 const inventory=await page.evaluate(()=>REVIEW_DATA.sources.map(source=>({id:source.id,programId:source.construction.id,passes:source.passes.map((pass,index)=>({index,rungId:pass.rungId,operations:pass.operations.map(op=>({id:op.id,status:op.status})),edgeCount:source.construction.rungs[index].visibleEdgeIds.length}))})));
 assert(inventory.length===5,'The review must contain the five acquired churches');
 const before=await page.evaluate(()=>JSON.stringify(REVIEW_DATA)),archives=[];
 let passes=0,nativePaths=0,operations=0,rejected=0,roles=0,downloads=0,sourcesSimplified=0,sharedJoints=0,viewpointCandidates=0;
 for(const source of inventory){
   assert(source.passes.length>1,'Each source needs its baseline and later interpretations');
   await page.locator(`[data-source="${source.id}"]`).click();
   const href=await page.locator('#archive').getAttribute('href');
   const archive=href.startsWith('file:')?fileURLToPath(href):path.resolve(path.dirname(review),href);
   let bytes=fs.readFileSync(archive);const archiveSha256=sha256(bytes);if(archive.endsWith('.gz'))bytes=zlib.gunzipSync(bytes);assert(JSON.parse(bytes.toString('utf8')),'Native archive unreadable');
   archives.push({programId:source.programId,archiveSha256,programSha256:sha256(bytes)});
   if(source.passes.at(-1).edgeCount<source.passes[0].edgeCount)sourcesSimplified++;
   for(const pass of source.passes){
     await page.locator(`[data-pass="${pass.index}"]`).click();
     const actual=await page.evaluate(index=>{
       const r=source.construction.rungs[index],edges=new Map(source.construction.analysis.edges.map(edge=>[edge.id,edge]));
       const expected=points=>`M${points[0].join(',')} `+(points.length===2?'L'+points[1].join(','):points.length===3?'Q'+points.slice(1).map(p=>p.join(',')).join(' '):'C'+points.slice(1).map(p=>p.join(',')).join(' '));
       const paths=[...document.querySelectorAll('#native-canvas [data-native-edge]')];
       const policy=source.policy.passSchedule||REVIEW_DATA.policy.passSchedule||[];
       const weightRow=[...document.querySelectorAll('#pass-settings dt')].find(node=>/^Current pass Error weight$/i.test(node.textContent));
       const expectedJoints=new Map();for(const id of r.visibleEdgeIds){const edge=edges.get(id);for(const site of [edge.controlSiteIds[0],edge.controlSiteIds.at(-1)]){if(!expectedJoints.has(site))expectedJoints.set(site,new Set());expectedJoints.get(site).add(id)}}
       const expectedJointIds=[...expectedJoints].filter(([,ids])=>ids.size>1).map(([site])=>site).sort();
       const reference=source.passes[index].referencePolicy||source.passes[index].policyWeights||{};
       const referencesMatch=Object.entries(reference).every(([key,value])=>[...document.querySelectorAll('#pass-settings dt')].some(node=>node.textContent==='Current reference '+label(key)&&node.nextElementSibling.textContent===valueText(value)));
       return {ids:paths.map(node=>node.dataset.nativeEdge).sort(),expectedIds:[...r.visibleEdgeIds].sort(),
         exact:paths.every(node=>node.getAttribute('d')===expected(edges.get(node.dataset.nativeEdge).controls)),
         policyMatches:index===0||!policy[index-1]||Number(weightRow?.nextElementSibling?.textContent)===policy[index-1].errorWeight,referencesMatch,
         jointIds:[...document.querySelectorAll('#native-canvas [data-native-joint]')].map(node=>node.dataset.nativeJoint).sort(),expectedJointIds,
         sourceCount:document.querySelectorAll('#source-canvas .source-stroke').length,expectedSource:source.observation.counts.strokes,
         residueIds:[...document.querySelectorAll('[data-residue-edge]')].map(node=>node.dataset.residueEdge).sort(),expectedResidue:[...(source.passes[index].residueEdgeIds||[])].sort()};
     },pass.index);
     assert(JSON.stringify(actual.ids)===JSON.stringify(actual.expectedIds)&&actual.exact,'Current pass diverges from native execution');
     assert(actual.sourceCount===actual.expectedSource,'A source stroke was discarded');
     assert(actual.policyMatches,'The settings panel shows another pass error weight');
     assert(actual.referencesMatch,'The settings panel changed the current source and interpretation weights');
     assert(JSON.stringify(actual.jointIds)===JSON.stringify(actual.expectedJointIds),'A displayed joint lacks actual shared native endpoint identity');sharedJoints+=actual.jointIds.length;
     assert(JSON.stringify(actual.residueIds)===JSON.stringify(actual.expectedResidue),'Suppressed detail does not follow native guide membership');
     passes++;nativePaths+=actual.ids.length;
     if(actual.jointIds.length){await page.locator(`[data-native-joint="${actual.jointIds[0]}"]`).press('Enter');assert((await page.locator('#detail').innerText()).includes('Native site'),'Shared joint has no inspectable native identity');}
     const supportedEdge=await page.evaluate(()=>rung().visibleEdgeIds.find(id=>(edgeSpans.get(id)||[]).length));
     assert(supportedEdge,'No visible native edge retains source correspondence');
     await page.locator(`[data-edge="${supportedEdge}"]`).press('Enter');
     assert((await page.locator('#detail').innerText()).includes('Source support'),'Direct native-edge inspection lost source support');
     await page.locator('[data-filter="all"]').click();
     assert(await page.locator('[data-operation]').count()===pass.operations.length,'Some accepted or rejected operations are absent');
     for(const operation of pass.operations){
       await page.locator(`[data-operation="${operation.id}"]`).click();
       const result=await page.evaluate(id=>{
         const op=current().operations.find(op=>op.id===id);
         return {inputs:[...document.querySelectorAll('[data-input-edge]')].map(node=>node.dataset.inputEdge).sort(),expectedInputs:[...op.inputEdgeIds].sort(),
           results:[...document.querySelectorAll('#native-canvas .native-edge.selected')].map(node=>node.dataset.nativeEdge).sort(),expectedResults:op.replacementEdgeIds.filter(id=>rung().visibleEdgeIds.includes(id)).sort(),
           sourceHighlights:document.querySelectorAll('#source-canvas .source-highlight').length,expectedSource:op.sourceSpanIds.length};
       },operation.id);
       assert(JSON.stringify(result.inputs)===JSON.stringify(result.expectedInputs),'Selected operation does not expose its actual native inputs');
       assert(JSON.stringify(result.results)===JSON.stringify(result.expectedResults),'Selected operation implies an unexecuted replacement');
       assert(result.sourceHighlights===result.expectedSource,'Selected operation source support was lost');
       const text=await page.locator('#detail').innerText();assert(text.includes('Source support')&&text.includes('Reason'),'Operation rationale or source intervals omitted');
       if(operation.status==='rejected'){assert(text.includes('Rejected; proposed change was not applied'),'Rejected proposal was presented as applied');rejected++;}
       operations++;
     }
     for(const group of ['roles','unsupported']){
       const count=await page.locator(`[data-${group}]`).count();
       for(let i=0;i<count;i++){await page.locator(`[data-${group}]`).nth(i).click();assert((await page.locator('#detail').innerText()).length>30,'Role/remainder has no inspectable qualification');roles++;}
     }
     const views=await page.locator('[data-viewpoint]').count();for(let i=0;i<views;i++){await page.locator('[data-viewpoint]').nth(i).click();assert((await page.locator('#detail').innerText()).includes('Selected interpretation'),'Viewpoint candidate omitted its provisional interpretation');viewpointCandidates++;}
     const pending=page.waitForEvent('download');await page.locator('#decisions').click();const download=await pending;
     const exported=JSON.parse(fs.readFileSync(await download.path(),'utf8'));
     assert(exported.sourceId===source.id&&exported.programId===source.programId&&exported.pass.rungId===pass.rungId,'Decision download identifies another source or pass');downloads++;
   }
   await page.locator('[data-filter="all"]').click();
   const accepted=source.passes.at(-1).operations.find(op=>op.status==='accepted');
   if(accepted)await page.locator(`[data-operation="${accepted.id}"]`).click();
   else if(await page.locator('#clear').count())await page.locator('#clear').click();
   await page.mouse.move(0,0);await page.screenshot({path:path.join(output,source.id+'.png'),fullPage:true});
 }
 assert(sourcesSimplified>0,'No drawing was actually simplified');assert(rejected>0,'No rejected alternative remains inspectable');
 const geometry=await page.locator('#native-canvas [data-native-edge]').evaluateAll(nodes=>nodes.map(node=>node.getAttribute('d')));
 for(const id of ['residue','joints','guides','previous','overlay','hints']){
   const checkbox=page.locator('#'+id);await checkbox.setChecked(!(await checkbox.isChecked()));
   assert(JSON.stringify(await page.locator('#native-canvas [data-native-edge]').evaluateAll(nodes=>nodes.map(node=>node.getAttribute('d'))))===JSON.stringify(geometry),'An inspection layer mutated the current drawing');
 }
 await page.locator('#hints').check();assert(await page.locator('[data-hint]').count()>0,'Source annotations are unavailable');
 await page.locator('[data-hint] .hint').first().click();assert((await page.locator('#detail').innerText()).includes('Source annotation only'),'Source label implies an unverified current-pass joint');
 await page.locator('#hints').uncheck();await page.locator('#guides').uncheck();await page.locator('#previous').uncheck();await page.locator('#overlay').uncheck();await page.locator('#residue').check();
 await page.locator('#play').click();await page.waitForTimeout(1550);assert(Number(await page.locator('#pass').inputValue())>0,'Pass walkthrough did not advance');await page.locator('#play').click();
 await page.setViewportSize({width:390,height:844});await page.locator('[data-source="5437095780810752"]').click();await page.locator('[data-pass]').last().click();
 assert(!await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+1),'Mobile review overflows horizontally');
 await page.screenshot({path:path.join(output,'mobile.png'),fullPage:true});
 assert(await page.evaluate(()=>JSON.stringify(REVIEW_DATA))===before,'Inspecting the review changed captured evidence');
 assert(errors.length===0,errors.join('\n'));assert(requests.length===0,'Local review made a remote request');
 const result={drawings:inventory.length,passes,nativePaths,operations,rejected,roles,downloads,sourcesSimplified,sharedJoints,viewpointCandidates,
   reviewSha256:sha256(fs.readFileSync(review)),sourceManifestSha256:await page.evaluate(()=>REVIEW_DATA.sourceManifestSha256),archives,
   capturedGeometryExact:true,sourcePreserved:true,rejectedNeverApplied:true,inspectionOnly:true,mobileOverflow:false,externalRequests:requests.length,consoleErrors:errors};
 fs.writeFileSync(path.join(output,'browser-summary.json'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));await browser.close();
})().catch(async error=>{console.error(error);if(browser)await browser.close();process.exitCode=1});
