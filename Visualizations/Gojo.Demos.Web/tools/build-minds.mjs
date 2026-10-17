// Static publication adapter. Displays captured Gojo results; runs no participant logic.
// Source of roadmap prose: experiments/Minds/docs/ExperimentRoadmap.json.
// node Visualizations/Gojo.Demos.Web/tools/build-minds.mjs [--check]
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
const web=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const repo=path.resolve(web,'../..'), output=path.join(web,'minds');
const check=process.argv.includes('--check');
const roadmap=JSON.parse(await fs.readFile(path.join(repo,'experiments/Minds/docs/ExperimentRoadmap.json'),'utf8'));
const resultDir=path.join(repo,'experiments/Minds/results/two-space');
const traceText=await fs.readFile(path.join(resultDir,'trace.json'),'utf8');
const runs=JSON.parse(traceText);
const studySelection=JSON.parse(await fs.readFile(path.join(repo,'experiments/Minds/results/feedback-limits/publication.json'),'utf8'));
if(!/^[a-zA-Z0-9_-]+$/.test(studySelection.directory))throw new Error('Invalid selected study directory');
const studyRelative='experiments/Minds/results/feedback-limits/'+studySelection.directory;
const studyDirectory=path.join(repo,studyRelative);
const studySummary=JSON.parse(await fs.readFile(path.join(studyDirectory,'summary.json'),'utf8'));
const predictionSelection=JSON.parse(await fs.readFile(path.join(repo,'experiments/Minds/results/predictions/publication.json'),'utf8'));
if(!/^[a-zA-Z0-9_-]+$/.test(predictionSelection.directory))throw new Error('Invalid selected prediction directory');
const predictionRelative='experiments/Minds/results/predictions/'+predictionSelection.directory;
const predictionDirectory=path.join(repo,predictionRelative);
const predictionSummary=JSON.parse(await fs.readFile(path.join(predictionDirectory,'summary.json'),'utf8'));
const escape=s=>String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
async function write(file,content){
  if(check){
    const actual=await fs.readFile(file,'utf8');
    if(actual.replace(/\r\n/g,'\n')!==content.replace(/\r\n/g,'\n'))throw new Error('Stale generated file: '+path.relative(repo,file));
  }else{await fs.mkdir(path.dirname(file),{recursive:true});await fs.writeFile(file,content);}
}
function page(title,description,body){return `<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><meta name="description" content="${escape(description)}"><title>${escape(title)} · Gojo Minds</title><link rel="stylesheet" href="/minds/minds.css"></head>
<body><a class="skip" href="#content">Skip to content</a><main><nav aria-label="Main"><a class="brand" href="/minds/">GOJO / MINDS</a><div class="links"><a href="/minds/#runs">Current runs</a><a href="/minds/roadmap/">Roadmap</a><a href="/">All Gojo demos</a></div></nav><div id="content">${body}</div><footer><p>Small, inspectable experiments with Gojo. Completed runs and proposed work are marked separately. A negative or surprising result is useful information.</p><a href="/minds/notes/setup/">Two-space setup</a><a href="/minds/notes/readiness/">Readiness review</a><a href="/minds/notes/protocol/">Optional protocol template</a><a href="/minds/manifest.json">Publication manifest</a></footer></main></body></html>\n`;}
const list=items=>'<ul>'+items.map(s=>'<li>'+escape(s)+'</li>').join('')+'</ul>';
const badge=e=>`<span class="badge ${e.status==='Next'?'next':''}">${escape(e.status)}</span>`;
const ids=new Set();
for(const e of roadmap.experiments){if(!/^\d{2}$/.test(e.id)||ids.has(e.id))throw new Error('Invalid roadmap ID');ids.add(e.id);}
const completedCards=[
  {id:'01',title:'Two spaces exchange estimates',url:'/minds/two-space/',count:'5 selected conditions',
   blurb:'Watch two local estimates agree, keep swapping, or retain a gap. Their average can stay still while both parts move.'},
  {id:'02',title:'What changes the answer?',url:'/minds/feedback-limits/',count:studySummary.summaries.length+' conditions · '+studySummary.summaries.reduce((n,s)=>n+s.completed,0)+' runs',
   blurb:'Change tick size, message timing and references. Quiet values can still disagree, and missing messages differ from choosing not to respond.'},
  {id:'03',title:'Predict before the observation arrives',url:'/minds/predictions/',count:predictionSummary.cases.length+' selected sequences',
   blurb:'Continue the last two samples, then check the next value. The line succeeds on ramps, loses to hold-last on steps and alternation, and leaves missing outcomes unscored.'}
];
const runCards=completedCards.map(c=>`<a class="card" href="${c.url}"><p class="eyebrow">Experiment ${c.id} · Explored</p><h3>${escape(c.title)}</h3><p>${escape(c.blurb)}</p><p class="small">${escape(c.count)}</p><span class="go">Explore the recorded results →</span></a>`).join('');
await write(path.join(output,'index.html'),page('Experiments','Gojo experiments, captured results and the tools to build next.',`
<header><p class="eyebrow">Minds · experiment notebook</p><h1>Small systems.<br>What can they learn together?</h1><p class="lead">Start with local estimates, connect them, and test their predictions. Each experiment keeps the selected rules, observed results and unanswered questions together.</p></header>
<section id="runs"><div class="section-head"><h2>Completed experiments</h2><a href="/minds/roadmap/">Experiments and what to build next →</a></div><div class="cards">${runCards}</div></section>
<section class="section"><div class="notice"><p><strong>Next: when does a broader view help?</strong> Test whether observing the whole can warn of a disturbance or request useful detail, using information available before the outcome.</p></div><div class="actions"><a class="button" href="/minds/roadmap/#experiment-04">Experiment 04 and its prerequisites</a><a class="button secondary" href="/minds/notes/prediction-findings/">What experiment 03 showed</a></div></section>
<section class="section prose"><h2>What makes a useful result?</h2><p>Agreement, accurate prediction and useful action are separate questions. We retain misses, unresolved work and comparisons with ordinary exact arithmetic. The aim is to find where the structure helps and where it does not.</p><p>This work is motivated in part by <a href="https://www.mdpi.com/2409-9287/11/5/161">Ingressing Minds</a> and its invitation to examine unfamiliar forms of problem solving. These small experiments test concrete behaviors; stronger claims about cognition remain open.</p></section>`));
let phase='';
const steps=roadmap.experiments.map(e=>{
  const heading=e.phase!==phase?`<h2 class="phase">${escape(e.phase)}</h2>`:'';phase=e.phase;
  if(e.status==='Explored'&&e.resultUrl)return `${heading}<section class="milestone" id="experiment-${e.id}"><div class="build"><p class="eyebrow">Already built</p><p>${escape(e.before)}</p></div><div class="experiment"><p class="eyebrow">Experiment ${e.id} · ${badge(e)}</p><h3>${escape(e.title)}</h3><p>${escape(e.observe)}</p><p>${escape(e.limit)}</p><p><a href="${e.resultUrl}">Open the recorded results →</a> · <a href="${e.findingsUrl}">Findings and limits</a></p></div></section>`;
  return `${heading}<section class="milestone" id="experiment-${e.id}"><div class="build"><p class="eyebrow">${e.status==='Explored'?'Already built':'Build before experiment '+e.id}</p><p>${escape(e.before)}</p></div><div class="experiment"><p class="eyebrow">Experiment ${e.id} · ${badge(e)}</p><h3>${escape(e.title)}</h3><p class="lead">${escape(e.question)}</p><dl>${[['Try',e.try],['Observe',e.observe],['Controls',e.controls],['Ready when',e.ready]].map(([label,value])=>`<dt>${label}</dt><dd>${escape(value)}</dd>`).join('')}</dl><p class="boundary">${escape(e.limit)}</p>${e.id==='01'?'<p style="margin-top:16px"><a href="/minds/two-space/">Open the completed walkthrough →</a></p>':''}</div></section>`;
}).join('');
await write(path.join(output,'roadmap/index.html'),page(roadmap.title,'A sequence of Gojo experiments, with the missing tools and structural work before each one.',`
<header><p class="eyebrow">Suggested sequence · ${roadmap.date}</p><h1>Experiments and the work between them</h1><p class="lead">${escape(roadmap.introduction)}</p><p>Steps 01–03 establish what happens and whether predictions hold up. Steps 04–08 explore useful observation, activity, memory and reuse. Sorting supplies a different task; repair and search then ask how the organization itself can change.</p><a href="/minds/downloads/ExperimentRoadmap.md" download>Download the roadmap as Markdown</a></header>
<div class="toc" aria-label="Jump to experiment">${roadmap.experiments.map(e=>`<a href="#experiment-${e.id}">${e.id} · ${escape(e.title)}</a>`).join('')}</div><div class="notice"><p><strong>A practical order, not one mandatory ladder.</strong> Sorting can branch after step 02. A disappointing result can change the next question. None of these steps requires finishing exact replay or banning external decimal calculations first.</p></div>${steps}
<section class="section prose"><h2>Shared rules</h2>${list(roadmap.principles)}<h2>Build later, when a question needs it</h2>${list(roadmap.later)}<h2>Research context</h2>${roadmap.sources.map(s=>`<p><a href="${escape(s.url)}">${escape(s.title)}</a>. ${escape(s.note)}</p>`).join('')}<p>The current <a href="/minds/notes/readiness/">readiness review</a> records implementation footholds and their limits. The <a href="https://github.com/It3rate/Gojo/blob/master/Gojo/docs/GojoSignalPatternLearningPlan-2026-09.md">signal-learning plan</a> guides prediction and memory work.</p></section>`));
const md=`# ${roadmap.title}\n\nSuggested sequence, ${roadmap.date}.\n\n${roadmap.introduction}\n\n`+
  'Sorting can branch after experiment 02. A valid negative result can change the next question; it is not a broken gate. Exact replay and numeric compiler guards are not prerequisites.\n\n'+
  roadmap.experiments.map(e=>`## ${e.id}. ${e.title}\n\nStatus: ${e.status}. ${e.phase}.\n\n**${e.status==='Explored'?'Already built':'Build before this experiment'}:** ${e.before}\n\n**Question:** ${e.question}\n\n**Try:** ${e.try}\n\n**Observe:** ${e.observe}\n\n**Controls:** ${e.controls}\n\n**Ready when:** ${e.ready}\n\n**Limit:** ${e.limit}\n`).join('\n')+
  '\n## Shared rules\n\n'+roadmap.principles.map(s=>'- '+s).join('\n\n')+'\n\n## Later, when needed\n\n'+roadmap.later.map(s=>'- '+s).join('\n\n')+'\n\n## Research context\n\n'+roadmap.sources.map(s=>`- [${s.title}](${s.url}). ${s.note}`).join('\n\n')+'\n\nGenerated from `ExperimentRoadmap.json` by `Visualizations/Gojo.Demos.Web/tools/build-minds.mjs`. Edit that source to keep the site and document aligned.\n';
await write(path.join(repo,'experiments/Minds/docs/ExperimentRoadmap.md'),md);
await write(path.join(output,'downloads/ExperimentRoadmap.md'),md);

// Reuse the complete viewer and its embedded observations. Add navigation only.
let viewer=await fs.readFile(path.join(resultDir,'index.html'),'utf8');
if(!viewer.includes('<main>'))throw new Error('Viewer navigation insertion point changed');
viewer=viewer.replace('<main>','<main><nav aria-label="Minds notebook" style="display:flex;gap:24px;flex-wrap:wrap;margin-bottom:26px;font-size:14px"><a href="/minds/" style="color:#076c77">← Minds experiments</a><a href="/minds/roadmap/" style="color:#076c77">Roadmap</a><a href="/minds/notes/setup/" style="color:#076c77">Setup and interpretation</a><a href="/minds/two-space/trace.json" download style="color:#076c77">Download readings</a></nav>');
await write(path.join(output,'two-space/index.html'),viewer);
await write(path.join(output,'two-space/trace.json'),traceText);
await write(path.join(output,'fixtures/two-space-feedback.gojo'),await fs.readFile(path.join(repo,'Tests/Gojo.Encoding.Tests/Fixtures/Syntax/two-space-feedback.gojo'),'utf8'));
async function walk(dir){return(await Promise.all((await fs.readdir(dir,{withFileTypes:true})).map(e=>e.isDirectory()?walk(path.join(dir,e.name)):[path.join(dir,e.name)]))).flat();}
for(const file of await walk(studyDirectory)){
  const relative=path.relative(studyDirectory,file);
  let content=await fs.readFile(file,'utf8');
  if(relative==='index.html')content=content.replace('href="Protocol.md"','href="/minds/notes/limits-protocol/"').replace('href="Results.md"','href="/minds/notes/limits-results/"');
  await write(path.join(output,'feedback-limits',relative),content);
}

for(const file of await walk(predictionDirectory)){
  const relative=path.relative(predictionDirectory,file);
  let content=await fs.readFile(file,'utf8');
  if(relative==='index.html')content=content.replaceAll('href="Protocol.md"','href="/minds/notes/prediction-protocol/"').replaceAll('href="Results.md"','href="/minds/notes/prediction-results/"');
  await write(path.join(output,'predictions',relative),content);
}

// Render the repository notes without adding a runtime Markdown dependency.
// Supported subset used by these notes: headings, paragraphs, lists, tables,
// fenced code, links, inline code and emphasis. Everything else stays escaped.
const notes=[
  ['readme','experiments/Minds/README.md','About Minds'],
  ['setup','experiments/Minds/docs/TwoSpaceExploration.md','Two-space setup and interpretation'],
  ['results','experiments/Minds/results/two-space/Results.md','Observed results'],
  ['readiness','experiments/Minds/docs/ExperimentReadiness.md','Experiment readiness review'],
  ['limits-findings','experiments/Minds/docs/FeedbackLimitsFindings.md','Feedback limits: findings'],
  ['limits-protocol','experiments/Minds/docs/FeedbackLimitsProtocol.md','Feedback limits: selected rules'],
  ['limits-results',studyRelative+'/Results.md','Feedback limits: complete results'],
  ['prediction-findings','experiments/Minds/docs/PredictionFindings.md','Predictions: findings and limits'],
  ['prediction-protocol','experiments/Minds/docs/PredictionProtocol.md','Predictions: selected rules'],
  ['prediction-results',predictionRelative+'/Results.md','Predictions: complete results'],
  ['protocol','experiments/Minds/docs/ExperimentProtocol.template.md','Optional experiment protocol']
];
const routes=new Map(notes.map(([slug,file])=>[path.resolve(repo,file),'/minds/notes/'+slug+'/']));
routes.set(path.join(repo,'experiments/Minds/docs/ExperimentRoadmap.md'),'/minds/roadmap/');
routes.set(path.join(resultDir,'index.html'),'/minds/two-space/');
routes.set(path.join(resultDir,'trace.json'),'/minds/two-space/trace.json');
routes.set(path.join(repo,'Tests/Gojo.Encoding.Tests/Fixtures/Syntax/two-space-feedback.gojo'),'/minds/fixtures/two-space-feedback.gojo');
function link(href,source){
  if(/^(?:https?:|#|\/)/i.test(href))return href;
  const match=href.match(/^([^?#]*)(.*)$/), file=path.resolve(path.dirname(path.join(repo,source)),match[1]);
  if(routes.has(file))return routes.get(file)+match[2];
  if(file.startsWith(studyDirectory+path.sep))return '/minds/feedback-limits/'+path.relative(studyDirectory,file).replaceAll(path.sep,'/').replace(/index\.html$/,'')+match[2];
  if(file.startsWith(predictionDirectory+path.sep))return '/minds/predictions/'+path.relative(predictionDirectory,file).replaceAll(path.sep,'/').replace(/index\.html$/,'')+match[2];
  const relative=path.relative(repo,file).replaceAll(path.sep,'/');
  if(relative.startsWith('../'))throw new Error('Note link leaves repository: '+href);
  // Minds implementation files may not yet be on the public master branch.
  if(relative.startsWith('experiments/Minds/')||relative.startsWith('Tests/Minds.Tests/'))return '/minds/source/'+relative+(/\.(cs|csproj)$/.test(relative)?'.txt':'')+match[2];
  return 'https://github.com/It3rate/Gojo/blob/master/'+relative+match[2];
}
function inline(raw,source){
  const tokens=[];
  const token=html=>'\u0000'+(tokens.push(html)-1)+'\u0000';
  let s=raw.replace(/`([^`]+)`/g,(_,x)=>token('<code>'+escape(x)+'</code>'));
  s=s.replace(/\[([^\]]+)\]\(([^)]+)\)/g,(_,label,href)=>token(`<a href="${escape(link(href,source))}">${escape(label)}</a>`));
  s=escape(s).replace(/\*\*([^*]+)\*\*/g,'<strong>$1</strong>').replace(/\*([^*]+)\*/g,'<em>$1</em>');
  return s.replace(/\u0000(\d+)\u0000/g,(_,i)=>tokens[Number(i)]);
}
function markdown(text,source){
  const lines=text.replace(/\r/g,'').split('\n');let html='',i=0;
  const special=s=>/^(#{1,6} |```|~~~|\s*[-*] |\s*\d+\. |\|)/.test(s);
  while(i<lines.length){
    let s=lines[i];if(!s.trim()){i++;continue;}
    if(/^(```|~~~)/.test(s)){const fence=s.slice(0,3),body=[];i++;while(i<lines.length&&!lines[i].startsWith(fence))body.push(lines[i++]);i++;html+='<pre><code>'+escape(body.join('\n'))+'</code></pre>';continue;}
    const heading=s.match(/^(#{1,6}) (.*)$/);if(heading){const level=Number(heading[1].length);html+=`<h${level}>${inline(heading[2],source)}</h${level}>`;i++;continue;}
    if(s.startsWith('|')&&/^\|[\s:|-]+\|$/.test(lines[i+1]||'')){
      const cells=row=>row.trim().replace(/^\||\|$/g,'').split('|').map(x=>x.trim());
      html+='<div class="table-wrap"><table><thead><tr>'+cells(s).map(c=>'<th>'+inline(c,source)+'</th>').join('')+'</tr></thead><tbody>';i+=2;
      while(i<lines.length&&lines[i].startsWith('|'))html+='<tr>'+cells(lines[i++]).map(c=>'<td>'+inline(c,source)+'</td>').join('')+'</tr>';
      html+='</tbody></table></div>';continue;
    }
    if(/^\s*(?:[-*]|\d+\.) /.test(s)){
      const ordered=/^\s*\d+\. /.test(s),tag=ordered?'ol':'ul',pattern=ordered?/^\s*\d+\. /:/^\s*[-*] /;html+='<'+tag+'>';
      while(i<lines.length&&pattern.test(lines[i])){
        let item=lines[i++].replace(pattern,'');
        while(i<lines.length&&lines[i].trim()&&!special(lines[i]))item+=' '+lines[i++].trim();
        html+='<li>'+inline(item,source)+'</li>';
        if(!lines[i]?.trim()&&pattern.test(lines[i+1]||''))i++;
      }html+='</'+tag+'>';continue;
    }
    const paragraph=[s];i++;while(i<lines.length&&lines[i].trim()&&!special(lines[i]))paragraph.push(lines[i++]);
    html+='<p>'+inline(paragraph.join(' '),source)+'</p>';
  }return html;
}
const publishedSources=new Set();
for(const [slug,file,title] of notes){
  const raw=await fs.readFile(path.join(repo,file),'utf8');
  const rewritten=raw.replace(/\[([^\]]+)\]\(([^)]+)\)/g,(_,label,href)=>`[${label}](${link(href,file)})`);
  const downloadName=slug==='limits-results'?'FeedbackLimitsResults.md':slug==='prediction-results'?'PredictionResults.md':path.basename(file);
  await write(path.join(output,'downloads',downloadName),rewritten);
  const body=markdown(raw,file);
  for(const match of body.matchAll(/href="\/minds\/source\/([^"?#]+)(?:[^\"]*)"/g))publishedSources.add(match[1]);
  await write(path.join(output,'notes',slug,'index.html'),page(title,title,`<article class="prose"><p class="eyebrow" style="margin-top:32px">Minds · project notes</p><p class="small"><a href="/minds/downloads/${downloadName}" download>Download Markdown</a></p>${body}</article>`));
}
for(const file of publishedSources){
  const source=file.replace(/\.(cs|csproj)\.txt$/,'.$1');
  await write(path.join(output,'source',file),await fs.readFile(path.join(repo,source),'utf8'));
}
const manifestFiles=['experiments/Minds/docs/ExperimentRoadmap.json','experiments/Minds/results/two-space/trace.json','experiments/Minds/results/two-space/index.html','experiments/Minds/TwoSpace/FeedbackExperiment.cs','experiments/Minds/TwoSpace/Space.cs','experiments/Minds/TwoSpace/PairObserver.cs','experiments/Minds/TwoSpace/Graph.cs','experiments/Minds/Presentation/FeedbackReport.cs','Tests/Minds.Tests/TwoSpaceTests.cs','Tests/Gojo.Encoding.Tests/Fixtures/Syntax/two-space-feedback.gojo'];
manifestFiles.push(studyRelative+'/summary.json',studyRelative+'/Protocol.md','experiments/Minds/FeedbackLimits/StudyProtocol.cs','experiments/Minds/FeedbackLimits/GojoStudy.cs','experiments/Minds/FeedbackLimits/ConventionalReference.cs','experiments/Minds/FeedbackLimits/StudyComparison.cs','Tests/Minds.Tests/FeedbackLimitsTests.cs');
manifestFiles.push(predictionRelative+'/summary.json',predictionRelative+'/Protocol.md','experiments/Minds/Predictions/ForecastSpace.cs','experiments/Minds/Predictions/PredictionStudy.cs','experiments/Minds/Predictions/PredictionEvaluation.cs','experiments/Minds/Predictions/PredictionProtocol.cs','experiments/Minds/Presentation/PredictionsReport.cs','experiments/Minds/Presentation/predictions.html','Tests/Minds.Tests/PredictionTests.cs','Tests/Gojo.Encoding.Tests/Fixtures/Syntax/prospective-calibration.gojo');
const manifest={description:'Published captured observations and source fingerprints. This is a diagnostic publication, not a restorable checkpoint or a preregistered protocol.',roadmapDate:roadmap.date,files:{}};
for(const file of manifestFiles)manifest.files[file]='sha256-'+crypto.createHash('sha256').update(await fs.readFile(path.join(repo,file))).digest('hex');
await write(path.join(output,'manifest.json'),JSON.stringify(manifest,null,2)+'\n');
console.log((check?'Checked':'Built')+' Minds: '+runs.length+' observed conditions, '+roadmap.experiments.length+' roadmap experiments, '+notes.length+' notes.');
