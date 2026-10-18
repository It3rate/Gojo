using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

public static class QualitativeHierarchicalRecognitionExperiment
{
    public static int Run(string output)
    {
        Directory.CreateDirectory(output);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var study = new QualitativeHierarchicalRecognitionStudy(Console.WriteLine);
        var model = study.Sessions.Select(Model).ToArray();
        File.WriteAllText(Path.Combine(output, "gallery.html"), Head + "<script type=application/json id=native-data>" + JsonSerializer.Serialize(model) + "</script>" + Tail);
        var diagnostics = new
        {
            Scope = "Finite native phase programs; separate training and held-out instances at both token levels; observational queries, not material transfer.",
            Cases = model.Select(row => new { row.Id, row.Name, row.Columns, row.Rows, row.TrainingRows, row.InitialCollection,
                row.Hidden, row.NewQueries, row.CellCapacity, row.RowRequests, row.RowAccepted, row.Reuse,
                row.Blocked, row.Rejected, row.CellPrograms, row.RowPrograms, row.Before, row.After, row.LowerBefore, row.LowerAfter }),
            GenerationSeconds = Math.Round(clock.Elapsed.TotalSeconds, 3)
        };
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(diagnostics, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("Wrote " + Path.GetFullPath(output));
        return 0;
    }

    // This is an explicit audit/display adapter. It alone sees the supplier's
    // private correspondence and numeric realization, after all reads finish.
    private static CaseModel Model(QualitativeHierarchicalRecognitionSession session)
    {
        var fixture = session.Fixture; var observation = fixture.Observation;
        var sites = N.E(observation.Sites); var edges = N.E(observation.Segments).Cast<Relation>().ToArray();
        var symbols = N.E(session.Arrangement.PrototypeScope);
        var legends = symbols.Select((symbol, i) =>
        {
            var family = fixture.Motifs.Families.Single(row => ReferenceEquals(row.Definition, symbol));
            var occurrence = family.Occurrences[0];
            var name = family.SegmentCount == 3 ? "Triangle circuit" : family.SegmentCount == 8 ? "Eight-spoke junction" :
                N.E(occurrence.Segments).Cast<Relation>().All(edge => observation.TryReadSegment(edge, out _, out _, out _, out var heading) && N.E(heading).Length == 2)
                ? "Diamond circuit" : "Axis rectangle circuit";
            return new LegendModel(i, $"{(char)('A' + i)}", name);
        }).ToArray();
        int SymbolId(IElement symbol) => Array.FindIndex(symbols, actual => N.Same(symbol, actual));
        string Status(QualitativePatternTokenGraph graph, IElement node)
        {
            if (!graph.TryFindNode(node, out _, out var scope) || !N.Read(scope, out var values)) return "open";
            return values.Length == 1 ? "known" : values.Length == 0 ? "excluded" : "competing";
        }
        int[] Values(QualitativePatternTokenGraph graph, IElement node) => graph.TryFindNode(node, out _, out var scope) ? N.E(scope).Select(SymbolId).ToArray() : [];
        int DemandsAt(QualitativePatternGrammarEvaluation read, IElement node) => N.E(read.Demands).OfType<Relation>()
            .Count(demand => read.TryReadDemand(demand, out var obligation) && N.Same(obligation.Node, node));
        var displayBindings = N.E(session.Arrangement.NodeBindings).Cast<Relation>().ToDictionary(row => row.Right, row => fixture.ChamberCoordinates[row.Left], N.Participants.Instance);
        var queried = session.LowerRounds.SelectMany(round => N.E(round.NewFacts)).Cast<Relation>().Select(row => row.Left).ToHashSet(N.Participants.Instance);
        var cellData = session.RowPaths.SelectMany((path, rowIndex) => N.E(path).Select((node, columnIndex) =>
        {
            var coordinate = displayBindings[node];
            return new CellModel(columnIndex, rowIndex, coordinate.Column, coordinate.Row,
                Values(session.InitialCells, node), Values(session.FinalCells, node), Status(session.InitialCells, node), Status(session.FinalCells, node),
                N.E(session.HiddenNodes).Any(hidden => N.Same(hidden, node)), queried.Contains(node), DemandsAt(session.CellBefore, node));
        })).ToArray();
        var framed = session.CellGrammar.Grammars.SelectMany(candidate => N.E(candidate.FramedSymbols)).ToArray();
        int FramedId(IElement symbol) => Array.FindIndex(framed, actual => N.Same(symbol, actual));
        var frames = session.CellGrammar.Grammars.SelectMany((candidate, program) => N.E(candidate.FramedSymbols).Select((symbol, start) =>
        {
            var phases = N.E(candidate.Phases).Cast<Relation>().ToArray();
            return new FrameModel(FramedId(symbol), program, candidate.Period,
                Enumerable.Range(0, candidate.Period).Select(i => SymbolId(phases[(start + i) % phases.Length].Right)).ToArray());
        })).ToArray();
        RowModel Row(Relation path, int index)
        {
            var node = session.Ladder.NodeForPath(path);
            var before = session.InitialRowReads[index]; var after = session.FinalRowReads[index];
            string State(Relation values) => !N.Read(values, out var alternatives) ? "open" : alternatives.Length == 0 ? "excluded" : alternatives.Length == 1 ? "known" : "competing";
            return new(index, index < session.TrainingRowCount, N.E(before.Symbols).Select(FramedId).ToArray(),
                N.E(after.Symbols).Select(FramedId).ToArray(), State(before.Symbols), State(after.Symbols),
                N.E(before.PrivateMatches).Length, N.E(after.PrivateMatches).Length, N.E(after.Dominance).Length,
                DemandsAt(session.RowBefore, node), N.E(path).Sum(cell => DemandsAt(session.CellBefore, cell)),
                session.UpperRound.RequestReads.Count(request => N.Same(request.Node, node)),
                session.LowerRounds.Where(round => round.ParentCause is Relation cause && session.UpperRound.RequestReads.Any(request => ReferenceEquals(request.Read, cause) && N.Same(request.Node, node)))
                    .Select(round => new RoundModel(round.RequestReads.Count, N.E(round.NewFacts).Length, N.E(round.Reused).Length,
                        N.E(round.Blocked).Length, N.E(round.Remaining).Length)).ToArray());
        }
        var boundaryEdges = fixture.Motifs.Families.SelectMany(family => family.Occurrences)
            .Where(occurrence => N.E(fixture.ChamberFocus).Any(selected => ReferenceEquals(selected, occurrence.Read)))
            .SelectMany(occurrence => N.E(occurrence.Segments)).ToHashSet(N.Participants.Instance);
        return new(session.Id, session.Name, session.Description, fixture.Columns, fixture.Rows, session.TrainingRowCount,
            sites.Select(site => new[] { fixture.Positions[site].X, fixture.Positions[site].Y }).ToArray(),
            edges.Select(edge =>
            {
                observation.TryReadSegment(edge, out _, out var a, out var b, out _);
                return new[] { Array.FindIndex(sites, site => N.Same(site, a)), Array.FindIndex(sites, site => N.Same(site, b)), boundaryEdges.Contains(edge) ? 1 : 0 };
            }).ToArray(), legends, cellData, session.RowPaths.Select(Row).ToArray(), frames,
            session.CellGrammar.Grammars.Select(candidate => new ProgramModel(candidate.Period, N.E(candidate.Phases).Cast<Relation>().Select(phase => SymbolId(phase.Right)).ToArray(), N.E(candidate.Description).Length)).ToArray(),
            session.RowGrammar.Grammars.Select(candidate => new ProgramModel(candidate.Period, N.E(candidate.Phases).Cast<Relation>().Select(phase => FramedId(phase.Right)).ToArray(), N.E(candidate.Description).Length)).ToArray(),
            Count(session.RowBefore), Count(session.RowAfter), Count(session.CellBefore), Count(session.CellAfter),
            session.InitiallyCollectedCount, N.E(session.HiddenNodes).Length, session.NewCellQueries, N.E(session.CellService.Capacity).Length,
            session.UpperRound.RequestReads.Count, N.E(session.UpperRound.Accepted).Length,
            N.E(session.UpperRound.Reused).Length + session.LowerRounds.Sum(round => N.E(round.Reused).Length),
            N.E(session.UpperRound.Blocked).Length + session.LowerRounds.Sum(round => N.E(round.Blocked).Length),
            N.E(session.UpperRound.Rejected).Length + session.LowerRounds.Sum(round => N.E(round.Rejected).Length));
    }
    private static Counts Count(QualitativePatternGrammarEvaluation read) => new(N.E(read.Supported).Length, N.E(read.Unresolved).Length, N.E(read.Refuted).Length, N.E(read.Blocked).Length);
    private sealed record Counts(int Supported, int Open, int Refuted, int Blocked);
    private sealed record LegendModel(int Id, string Letter, string Name);
    private sealed record CellModel(int Column, int Row, int X, int Y, int[] Before, int[] After, string BeforeStatus, string AfterStatus, bool Hidden, bool Queried, int InitialDemands);
    private sealed record RoundModel(int Requests, int Facts, int Reused, int Blocked, int Remaining);
    private sealed record RowModel(int Index, bool Training, int[] Before, int[] After, string BeforeStatus, string AfterStatus,
        int PriorMatches, int FinalMatches, int Dominance, int InitialDemands, int InitialCellDemands, int Requests, RoundModel[] Rounds);
    private sealed record FrameModel(int Id, int Program, int Period, int[] Symbols);
    private sealed record ProgramModel(int Period, int[] Symbols, int Description);
    private sealed record CaseModel(string Id, string Name, string Description, int Columns, int Rows, int TrainingRows,
        double[][] Sites, int[][] Edges, LegendModel[] Legend, CellModel[] Cells, RowModel[] RowData, FrameModel[] Frames,
        ProgramModel[] CellPrograms, ProgramModel[] RowPrograms, Counts Before, Counts After, Counts LowerBefore, Counts LowerAfter,
        int InitialCollection, int Hidden, int NewQueries, int CellCapacity, int RowRequests, int RowAccepted, int Reuse, int Blocked, int Rejected);

    private const string Head = """
<!doctype html><html lang=en><meta charset=utf-8><meta name=viewport content='width=device-width,initial-scale=1'><title>Questions through a pattern stack</title><style>
*{box-sizing:border-box}body{margin:0;background:#eeeae0;color:#263e3b;font:15px/1.55 system-ui,sans-serif}header,main{max-width:1500px;margin:auto;padding:30px}header{padding-top:48px}h1{font:clamp(38px,5vw,66px)/1.05 Georgia,serif;margin:15px 0 22px}h2{font:31px/1.15 Georgia,serif;margin:0 0 14px}h3{font:22px/1.2 Georgia,serif;margin:0 0 15px}p{max-width:1000px}.eyebrow{font-size:11px;letter-spacing:.15em;color:#876849;font-weight:700}a{color:#815971}nav{display:flex;flex-wrap:wrap;gap:12px 20px;margin:22px 0}.intro{display:grid;grid-template-columns:1fr 1fr 1fr;gap:20px;margin-top:25px}.intro div{border-top:2px solid #a38c66;padding-top:12px}.intro strong{display:block}.small{font-size:12px;color:#706c60}article{background:#fbf8ef;border:1px solid #cbc3b3;border-radius:6px;overflow:hidden;margin-bottom:35px;scroll-margin-top:15px}.copy{padding:25px 28px}.copy p{margin:8px 0}.controls{background:#e5e5d8;border-block:1px solid #c9c8b7;display:flex;align-items:center;flex-wrap:wrap;gap:16px;padding:15px 28px}label{display:flex;align-items:center;gap:9px;font-size:13px}select,button{font:14px system-ui;color:#263e3b;background:#fffdf4;border:1px solid #b1b6a4;padding:8px;border-radius:4px;max-width:100%}input{accent-color:#547669}.stage-name{font-weight:650;font-size:13px}.layout{display:grid;grid-template-columns:minmax(0,1.4fr) minmax(350px,1fr)}.field{padding:16px 18px;min-width:0}svg{width:100%;display:block}.side{padding:22px;background:#f1eee3;border-left:1px solid #d7ceba;min-width:0}.metrics{display:grid;grid-template-columns:repeat(3,1fr);gap:8px;margin:12px 0 18px}.metric{background:#faf8ef;border:1px solid #d9d1c0;border-radius:4px;padding:10px;font-size:12px}.metric b{display:block;font:25px Georgia,serif}.legend{display:flex;flex-wrap:wrap;gap:10px 16px;font-size:12px;margin:10px 0}.dot{display:inline-block;width:9px;height:9px;border-radius:50%;margin-right:5px}.program{display:flex;align-items:center;gap:5px;flex-wrap:wrap;margin:7px 0;font-size:12px}.token{border:1px solid #c7bfae;border-radius:4px;display:inline-flex;align-items:center;justify-content:center;min-width:23px;height:25px;font-size:12px;font-weight:700}.row-list{display:flex;flex-wrap:wrap;gap:5px;margin:12px 0}.row-button{padding:6px 10px;font-size:12px}.row-button.chosen{outline:2px solid #496b64;outline-offset:1px}.trace{padding:14px;border-left:3px solid #b59a6a;background:#fffcf3;font-size:13px;margin-top:14px}.trace p{margin:8px 0}.notice{padding:12px 15px;border:1px solid #d6c39e;font-size:13px;background:#f8f0de}.status-known{color:#276b59}.status-open,.status-competing{color:#9e702e}.status-excluded{color:#a44352}details{border-top:1px solid #d8d0be;padding:15px 28px}summary{cursor:pointer}table{width:100%;border-collapse:collapse;font-size:12px}td,th{padding:8px;border-bottom:1px solid #d9d1bf;text-align:left}.scroll{overflow:auto}.field-note{font-size:12px;margin:10px 8px;color:#706c60}.guide{border-left:3px solid #a08e6e;padding-left:18px}footer{padding:30px;font-size:13px} @media(max-width:1000px){.layout{grid-template-columns:1fr}.side{border-left:0;border-top:1px solid #d7ceba}.field{max-width:850px;margin:auto;width:100%}}@media(max-width:650px){header,main{padding:16px}.intro{grid-template-columns:1fr;gap:10px}.copy,.controls{padding:18px}.controls{align-items:flex-start;flex-direction:column}.metrics{grid-template-columns:1fr 1fr}.side{padding:18px}h2{font-size:28px}label{max-width:100%;flex-wrap:wrap}}
</style><header><span class=eyebrow>GOJŌ · LAYERED RECOGNITION AND LOCAL REFINEMENT</span><h1>Questions through<br>a pattern stack</h1><p>A field can be read as strokes, motifs, rows and a repeating arrangement of rows. Here the same finite phase-graph reader runs at two levels. An unresolved upper claim can ask a row for evidence; that row can ask a local motif supplier. Actual answers travel back up, including contradictions and alternatives.</p><div class=intro><div><strong>1 · Native observation</strong>Connected segments and ordinal placement establish chambers and their possible motif roles.</div><div><strong>2 · A program becomes a token</strong>Repeated local roles define a row program. Its starting phase stays part of the row identity.</div><div><strong>3 · Ask down; read up</strong>A larger program asks only through exposed ports. Detailed private geometry stays behind the boundary.</div></div><nav><a href='#H01'>Checkerboard</a><a href='#H02'>Three phases</a><a href='#H03'>Four phases</a><a href='#H04'>Contrary answer</a><a href='#H05'>Capacity limit</a><a href='#H06'>Competing roles</a><a href='#H07'>Change the question</a><a href='../recognition-firmness/gallery.html'>Previous study</a><a href='../../../../Gojo/docs/GojoHierarchicalRecognition.md'>Findings</a><a href='diagnostics.json'>Diagnostics</a></nav><p class=small>The fine field and numeric display positions are available to this renderer, after recognition. Pattern readers receive neither those positions nor the generating recipe. All local motifs are currently induced in advance; this is selective content publication and refinement, rather than a fully demand-driven discovery engine.</p></header><main id=cases></main>
""";
    private const string Tail = """
<script>
const data=JSON.parse(document.getElementById('native-data').textContent), palette=['#327d91','#b65b65','#8b7743','#775995','#46785b','#b66e38'];
const colors={known:'#397d68',open:'#c29748',competing:'#b58637',excluded:'#b64e5b'};
const esc=s=>String(s).replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;');
function token(d,id){let l=d.Legend.find(x=>x.Id===id);return `<span class=token style="background:${palette[id%palette.length]}20;color:${palette[id%palette.length]}">${l?l.Letter:'?'}</span>`}
function frame(d,id){let f=d.Frames.find(x=>x.Id===id);return f?f.Symbols.map(x=>token(d,x)).join(''):'?'}
function stats(r){return `${r.Supported} supported · ${r.Open} open · ${r.Refuted} refuted`}
for(const d of data){
 const a=document.createElement('article');a.id=d.Id;
 a.innerHTML=`<div class=copy><span class=eyebrow>${d.Id} · ${d.Columns} × ${d.Rows} CHAMBERS</span><h2>${esc(d.Name)}</h2><p>${esc(d.Description)}</p></div><div class=controls><label>Round trip <input class=stage type=range min=0 max=2 value=2 step=1><span class=stage-name></span></label><label>View <select class=view><option value=fine>Fine strokes and chambers</option><option value=cells>Published cell roles</option><option value=rows>Rows as program tokens</option></select></label><button class=reset>Compare initial state</button></div><div class=layout><div class=field><svg role=img></svg><div class=legend>${d.Legend.map(l=>`<span><i class=dot style="background:${palette[l.Id%palette.length]}"></i>${l.Letter} · ${esc(l.Name)}</span>`).join('')}</div><p class=field-note>Click a row to inspect its query chain. Dashed outline: locally unread at the start. Solid gold outline: a new fine reply was actually supplied. An amber fill marks a remaining open role; red marks exclusion from the frozen advertised program scope. In row-token view, the first possible program is previewed when alternatives remain; the side panel lists them all.</p></div><div class=side></div></div><details><summary>Evidence, alternatives and limits</summary><div class=scroll><table><thead><tr><th>Level / stage</th><th>Supported alignments</th><th>Open</th><th>Refuted</th></tr></thead><tbody>${[['Cell rows / before',d.LowerBefore],['Cell rows / after',d.LowerAfter],['Rows of rows / before',d.Before],['Rows of rows / after',d.After]].map(([name,r])=>`<tr><td>${name}</td><td>${r.Supported}</td><td>${r.Open}</td><td>${r.Refuted}</td></tr>`).join('')}</tbody></table></div><p class=small>Counts are frozen candidate × path × start-phase alignments. Many refuted offsets are normal alternatives, not damaged motifs. A supported alignment means consistency with this finite program and observation, not a universal symmetry proof. All actual matches remain on the private native audit. Shorter row descriptions are preferred only when their per-node predictions and link evidence agree exactly.</p></details>`;
 document.getElementById('cases').append(a);let chosen=d.RowData.find(r=>r.Rounds.length)?.Index??d.TrainingRows;
 const stage=a.querySelector('.stage'), view=a.querySelector('.view'), svg=a.querySelector('svg'), side=a.querySelector('.side');
 function draw(){let final=+stage.value===2,asking=+stage.value===1, fieldWidth=d.Columns*8,fieldHeight=d.Rows*8;
 a.querySelector('.stage-name').textContent=['Initial publication','Initial unresolved questions','Replies travel up'][+stage.value];
 svg.setAttribute('viewBox',`-3 -4 ${fieldWidth+17} ${fieldHeight+9}`);svg.setAttribute('aria-label',`${d.Name}, ${view.value}, ${final?'after':'before'} refinement`);
 let out=`<rect x=-3 y=-4 width=${fieldWidth+17} height=${fieldHeight+9} fill="#fbf8ef"/>`;
 for(const cell of d.Cells){let state=final?cell.AfterStatus:cell.BeforeStatus,values=final?cell.After:cell.Before;
 let x=cell.X*8,y=fieldHeight-(cell.Y+1)*8,selected=cell.Row===chosen;
 let fill=state==='open'||state==='competing'?'#d7b66a':state==='excluded'?'#c56b78':values.length===1?palette[values[0]%palette.length]:'#798e88';
 out+=`<rect x=${x+.12} y=${y+.12} width=7.76 height=7.76 fill="${fill}" fill-opacity="${selected?.18:.065}"/>`;
 if(cell.Hidden)out+=`<rect x=${x+.35} y=${y+.35} width=7.3 height=7.3 rx=.4 fill=none stroke="${final&&cell.Queried?'#a97a24':'#aa8d4f'}" stroke-width=.22 ${final&&cell.Queried?'':'stroke-dasharray=".65 .45"'}/>`;
 if(view.value==='cells')out+=`<text x=${x+4} y=${y+4.7} text-anchor=middle font-size=2.3 font-family=system-ui fill="${fill}">${values.length?values.map(v=>d.Legend.find(l=>l.Id===v)?.Letter??'?').join('/'):state==='excluded'?'&empty;':'?'}</text>`;
 if(asking&&cell.InitialDemands>0&&cell.Row===chosen)out+=`<circle cx=${x+4} cy=${y+4} r=2.6 fill=none stroke="#aa7c30" stroke-width=.35/>`;
 }
 if(view.value!=='rows')for(const edge of d.Edges){let p=d.Sites[edge[0]],q=d.Sites[edge[1]];let midx=(p[0]+q[0])/2,midy=(p[1]+q[1])/2;
 let cell=d.Cells.find(c=>midx>c.X*8&&midx<(c.X+1)*8&&midy>c.Y*8&&midy<(c.Y+1)*8),values=cell?(final?cell.After:cell.Before):[];
 let col=edge[2]?'#7e8d81':values.length===1?palette[values[0]%palette.length]:'#4c6a65';
 out+=`<path d="M${p[0]},${fieldHeight-p[1]}L${q[0]},${fieldHeight-q[1]}" stroke="${col}" stroke-width="${edge[2]?.13:.34}" fill=none opacity="${view.value==='cells'?(edge[2]?.5:.1):1}"/>`;
 }
 for(const r of d.RowData){let cell=d.Cells.find(c=>c.Row===r.Index),y=fieldHeight-(cell.Y+.5)*8,state=final?r.AfterStatus:r.BeforeStatus,vals=final?r.After:r.Before;
 if(view.value==='rows'){let f=d.Frames.find(f=>f.Id===vals[0]);for(let c=0;c<d.Columns;c++){let symbol=f?.Symbols[c%f.Period],col=symbol===undefined?'#c0a06c':palette[symbol%palette.length];out+=`<rect x=${c*8+.5} y=${y-2.4} width=7 height=4.8 rx=.6 fill="${col}" opacity="${state==='known'?.7:.25}"/>`}}
 out+=`<rect data-row=${r.Index} x=${fieldWidth+1} y=${y-3.2} width=11 height=6.4 rx=.5 fill="${colors[state]}" fill-opacity="${r.Index===chosen?.28:.1}" stroke="${r.Index===chosen?colors[state]:'none'}" stroke-width=.25 style="cursor:pointer"/><text data-row=${r.Index} x=${fieldWidth+6.5} y=${y+.8} text-anchor=middle font-size=1.9 fill="#304b45" style="cursor:pointer">${r.Training?'T':'H'}${r.Index+1} ${state==='known'?'✓':state==='excluded'?'×':'?'}</text>`;
 out+=`<rect data-row=${r.Index} x=0 y=${y-4} width=${fieldWidth} height=8 fill=transparent style="cursor:pointer"/>`;
 }
 svg.innerHTML=out;svg.querySelectorAll('[data-row]').forEach(el=>el.onclick=()=>{chosen=+el.dataset.row;draw()});
 let counts=final?d.After:d.Before,r=d.RowData[chosen],state=final?r.AfterStatus:r.BeforeStatus,vals=final?r.After:r.Before;
 let round=r.Rounds.reduce((sum,next)=>({Requests:sum.Requests+next.Requests,Facts:sum.Facts+next.Facts,Reused:sum.Reused+next.Reused,Blocked:sum.Blocked+next.Blocked,Remaining:sum.Remaining+next.Remaining}),{Requests:0,Facts:0,Reused:0,Blocked:0,Remaining:0});
 let traceBody;
 if(r.Training)traceBody='<p>This row supplied training evidence. It is never used as a held-out test or refinement target.</p>';
 else if(!final&&!asking)traceBody='<p>Initial snapshot: no refinement replies have been folded into this read. The displayed roles come only from the initial local publications.</p><p>No new queries or replies are shown at this step.</p>';
 else if(asking){let targets=d.Cells.filter(cell=>cell.Row===chosen&&cell.InitialDemands>0).length;
 traceBody=`<p>The initial native read contains ${r.InitialDemands} unresolved upper obligations naming this row and ${r.InitialCellDemands} local obligations naming ${targets} cell${targets===1?'':'s'}.</p><p>Rings show local demand targets in this initial read. These are questions, not sent requests or accepted replies. Capacity has not been applied in this snapshot.</p>`;}
 else if(r.Rounds.length)traceBody=`<p>Upper unresolved obligation &rarr; exposed row port &rarr; re-evaluate actual cell path &rarr; local cell port.</p><p>${round.Requests} local requests; ${round.Facts} new cell publications; ${round.Reused} reused replies; ${round.Blocked} blocked requests; ${round.Remaining} remaining demands across ${r.Rounds.length} local round${r.Rounds.length===1?'':'s'}.</p><p>${state==='excluded'?'The accepted local observation excludes every frozen row program. The upper claim is refuted at this row.':state==='open'||state==='competing'?'The supplier has not established a unique complete row program. Its uncertainty travels upward.':'The local answer settles the preferred row program; its framed identity travels upward.'}</p>`;
 else traceBody='<p>The row already supplied enough evidence. No downward request was needed.</p>';
 side.innerHTML=`<span class=eyebrow>UPPER HELD-OUT READ</span><div class=metrics><div class=metric><b>${counts.Supported}</b>supported alignments</div><div class=metric><b>${counts.Open}</b>open alignments</div><div class=metric><b>${counts.Refuted}</b>refuted offsets / claims</div></div><h3>The same reader, twice</h3><div class=small>Frozen local programs</div>${d.CellPrograms.map(p=>`<div class=program><span>${p.Period} phases →</span>${p.Symbols.map(id=>token(d,id)).join('')}<span>↻ · ${p.Description} description rows</span></div>`).join('')}<div class=small style="margin-top:14px">Frozen programs over row identities</div>${d.RowPrograms.map(p=>`<div class=program><span>${p.Period} phases →</span>${p.Symbols.map(id=>`<span class=token style="height:auto;padding:3px">${frame(d,id)}</span>`).join('')}<span>↻</span></div>`).join('')||'<p class=notice>No upper program was established from the supplied training scope.</p>'}<p class=small>${d.TrainingRows} training rows; ${d.Rows-d.TrainingRows} separate held-out rows. Start phase is preserved when a row becomes a token.</p><h3>Follow a row question</h3><div class=row-list>${d.RowData.map(row=>`<button class="row-button ${chosen===row.Index?'chosen':''}" data-row=${row.Index}>${row.Training?'T':'H'}${row.Index+1}</button>`).join('')}</div><div class=trace><strong>Row ${chosen+1} · <span class=status-${state}>${state}</span></strong><p>${vals.length?vals.map(id=>frame(d,id)).join(' or '):state==='excluded'?'No surviving program in the advertised frozen scope.':'No settled row role yet.'}</p>${traceBody}</div><p class=small>${d.InitialCollection} initial local content samples. Refinement: ${final?d.NewQueries:0} / ${d.CellCapacity} new cell queries; ${final?d.RowAccepted:0} row replies; ${final?d.Reuse:0} cached reply reuses; ${final?d.Blocked:0} blocked requests. Reuse is not independent evidence.</p>${d.Id==='H07'?'<p class=notice>The triangle remains in the fine field. Only four-edge circuits answer this explicitly narrower question.</p>':''}${d.Id==='H05'?'<p class=notice>Blocked capacity is lack of evidence, not evidence against a pattern.</p>':''}${d.Id==='H06'?'<p class=notice>Two actual motifs compete for the selected single role. The finite alternatives are preserved.</p>':''}`;
 side.querySelectorAll('[data-row]').forEach(el=>el.onclick=()=>{chosen=+el.dataset.row;draw()});
 }
 stage.oninput=draw;view.onchange=draw;a.querySelector('.reset').onclick=()=>{stage.value=+stage.value===0?2:0;draw()};draw();
}
</script><footer><p class=guide>These experiments establish bounded compositional reads, not general vision, arbitrary tiling recognition or trained aesthetic judgement. Directional circuits do not by themselves prove equal lengths, regular polygons or Euclidean symmetry. The detailed native audit keeps the observation, alternatives, containment premises, phase bindings, actual requests, capacity allocations and qualitative timeline.</p></footer></html>
""";
}
