import fs from "node:fs/promises";
import path from "node:path";
import { Presentation, PresentationFile } from "file:///C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs";

const OUT = String.raw`F:\Math\Drive\presentations\Gojo - Current Synthesis.pptx`;
const QA = String.raw`C:\Users\Robin\source\repos\Gojo\.codex-tmp\gojo-slides-20260901\final-qa`;
const ASSETS = String.raw`C:\Users\Robin\source\repos\Gojo\.codex-tmp\gojo-slides-20260901\extracted_media\presentations-ComplexitySpaces.pptx`;
const DRIVE = String.raw`F:\Math\Drive`;
const REPO = String.raw`C:\Users\Robin\source\repos\Gojo`;
const SOURCE_DECK = "https://docs.google.com/presentation/d/171x9oXIFok-h_eJhroY0pUzYl64iI0z_p6YBL8IxN74";

const C = {
  paper: "#F6F2EA",
  ink: "#1C1A20",
  muted: "#6E6875",
  hair: "#D8D0C5",
  violet: "#211B2D",
  violet2: "#342944",
  amber: "#F1A85B",
  cyan: "#43B9B2",
  magenta: "#DF6B9E",
  blue: "#718CE3",
  green: "#67B87D",
  red: "#D75A5A",
  white: "#FFFFFF",
};

const W = 1280;
const H = 720;
const FONT = "Aptos";
const DISPLAY = "Aptos Display";
const MONO = "Cascadia Mono";

async function writeBlob(filePath, blob) {
  await fs.mkdir(path.dirname(filePath), { recursive: true });
  await fs.writeFile(filePath, new Uint8Array(await blob.arrayBuffer()));
}

function addText(slide, text, x, y, w, h, style = {}) {
  const shape = slide.shapes.add({
    geometry: "textbox",
    name: style.name,
    position: { left: x, top: y, width: w, height: h },
    fill: "none",
    line: { style: "solid", fill: "none", width: 0 },
  });
  shape.text = text;
  shape.text.style = {
    fontSize: style.fontSize ?? 24,
    bold: style.bold ?? false,
    color: style.color ?? C.ink,
    typeface: style.typeface ?? FONT,
    lineSpacing: style.lineSpacing ?? 1.08,
    alignment: style.alignment ?? "left",
    verticalAlignment: style.verticalAlignment ?? "top",
    autoFit: "shrinkText",
    insets: style.insets ?? { top: 0, right: 0, bottom: 0, left: 0 },
  };
  return shape;
}

function addRect(slide, x, y, w, h, fill, radius = 0, line = "none", lineWidth = 0, name) {
  return slide.shapes.add({
    geometry: radius ? "roundRect" : "rect",
    name,
    position: { left: x, top: y, width: w, height: h },
    fill,
    line: { style: "solid", fill: line, width: lineWidth },
    ...(radius ? { borderRadius: radius } : {}),
  });
}

function addCircle(slide, x, y, d, fill, line = C.white, lineWidth = 0, name) {
  return slide.shapes.add({
    geometry: "ellipse",
    name,
    position: { left: x, top: y, width: d, height: d },
    fill,
    line: { style: "solid", fill: line, width: lineWidth },
  });
}

function connect(slide, a, b, color = C.muted, width = 3, opts = {}) {
  return slide.shapes.connect(a, b, {
    kind: opts.kind ?? "straight",
    fromSide: opts.fromSide,
    toSide: opts.toSide,
    line: { style: opts.dashed ? "dashed" : "solid", fill: color, width },
    // artifact-tool's imported OOXML line-end naming places `tail` at the
    // destination returned by shapes.connect, so use it for forward arrows.
    ...(opts.head === false ? {} : { tail: { type: opts.headType ?? "arrow", width: "med", length: "med" } }),
    ...(opts.tail ? { head: { type: "arrow", width: "sm", length: "sm" } } : {}),
  });
}

function addRule(slide, x, y, w, color = C.hair, height = 2) {
  return addRect(slide, x, y, w, height, color, 0, "none", 0);
}

function addHeader(slide, kicker, title, slideNo, status) {
  addText(slide, kicker.toUpperCase(), 72, 43, 380, 24, {
    fontSize: 15, bold: true, color: C.amber, typeface: FONT,
  });
  addText(slide, title, 72, 76, 1120, 72, {
    fontSize: 49, bold: true, color: C.ink, typeface: DISPLAY, lineSpacing: 0.96,
  });
  addRule(slide, 72, 151, 1136, C.hair, 2);
  if (status) {
    addText(slide, status.toUpperCase(), 985, 46, 185, 20, {
      fontSize: 13, bold: true, color: C.muted, alignment: "right",
    });
  }
  addText(slide, String(slideNo).padStart(2, "0"), 1164, 680, 44, 18, {
    fontSize: 13, bold: true, color: C.muted, alignment: "right",
  });
}

function addImageCard(slide, imageBytes, contentType, x, y, w, h, alt, fit = "contain", bg = C.violet) {
  addRect(slide, x, y, w, h, bg, 22, bg, 0);
  return slide.images.add({
    blob: imageBytes,
    contentType,
    alt,
    fit,
    position: { left: x + 10, top: y + 10, width: w - 20, height: h - 20 },
    geometry: "roundRect",
    borderRadius: 16,
  });
}

function addNotes(slide, lines) {
  slide.speakerNotes.textFrame.setText([
    "[Sources]",
    ...lines.map((line) => `- ${line}`),
    "[/Sources]",
  ].join("\n"));
  slide.speakerNotes.setVisible(true);
}

function addTickLine(slide, x, y, w, ticks, color, startLabel, endLabel) {
  addRect(slide, x, y, w, 5, color, 3, color, 0);
  for (let i = 0; i <= ticks; i += 1) {
    const tx = x + (w * i) / ticks;
    addRect(slide, tx - 1, y - 9, 2, 22, color, 0, color, 0);
  }
  if (startLabel !== undefined) addText(slide, String(startLabel), x - 8, y + 17, 40, 24, { fontSize: 16, bold: true, color });
  if (endLabel !== undefined) addText(slide, String(endLabel), x + w - 20, y + 17, 48, 24, { fontSize: 16, bold: true, color, alignment: "right" });
}

function addFlowNode(slide, text, x, y, w, h, fill, color = C.white, name) {
  const node = addRect(slide, x, y, w, h, fill, 20, fill, 0, name);
  addText(slide, text, x + 12, y + 9, w - 24, h - 18, {
    fontSize: 19, bold: true, color, alignment: "center", verticalAlignment: "middle",
  });
  return node;
}

const imagePaths = {
  network: path.join(ASSETS, "image3.png"),
  complexity: path.join(ASSETS, "image16.png"),
  comparison: path.join(ASSETS, "image11.png"),
  measurementError: path.join(ASSETS, "image2.png"),
  distributed: path.join(ASSETS, "image13.png"),
  pcbProcess: path.join(DRIVE, "presentations", "scratch-process-poster.jpg"),
  pcbProbe: path.join(DRIVE, "presentations", "continuity-probe-poster.jpg"),
};
const IMG = {};
for (const [key, filePath] of Object.entries(imagePaths)) {
  IMG[key] = await fs.readFile(filePath);
}

const p = Presentation.create({ slideSize: { width: W, height: H } });

// 1 — Title
{
  const s = p.slides.add();
  s.background.fill = C.paper;
  addText(s, "GOJO", 74, 62, 260, 28, { fontSize: 16, bold: true, color: C.amber });
  addText(s, "Numeric structure\nthat keeps its context", 74, 112, 585, 176, {
    fontSize: 69, bold: true, color: C.ink, typeface: DISPLAY, lineSpacing: 0.91,
  });
  addText(s, "A current synthesis of the kernel, uncertainty, and the path into CAD", 78, 318, 510, 70, {
    fontSize: 26, color: C.muted, lineSpacing: 1.12,
  });
  addText(s, "structure   /   tension   /   local reads", 78, 617, 490, 26, {
    fontSize: 17, bold: true, color: C.muted,
  });
  addImageCard(s, IMG.network, "image/png", 682, 70, 526, 570,
    "Historical Gojo illustration of connected measures, perspectives, and local structure", "contain", C.violet);
  addNotes(s, [
    `${REPO}\\Gojo\\docs\\GojoSystemPrompt.md (canonical current Gojo rules)`,
    `${DRIVE}\\presentations\\ComplexitySpaces.pptx (historical visual source)`,
    `${SOURCE_DECK} (narrative reference)`,
  ]);
}

// 2 — Thesis
{
  const s = p.slides.add();
  s.background.fill = C.paper;
  addHeader(s, "The thesis", "Local problems can settle locally—and stay legible", 2, "synthesis");
  addText(s,
    "Intelligence can emerge from many connected local corrections. Each local world receives change, accounts for what it can, and exposes what remains.\n\nNo global solver has to erase disagreement to produce a useful next state.",
    78, 202, 455, 300, { fontSize: 27, color: C.ink, lineSpacing: 1.12 });

  const source = addFlowNode(s, "incoming\ntension", 590, 290, 138, 86, C.magenta, C.white, "incoming-tension");
  const a = addCircle(s, 820, 198, 78, C.cyan, C.white, 4, "local-world-a");
  const b = addCircle(s, 958, 300, 92, C.amber, C.white, 4, "local-world-b");
  const c = addCircle(s, 818, 424, 72, C.blue, C.white, 4, "local-world-c");
  connect(s, source, a, C.cyan, 4, { fromSide: "right", toSide: "left", kind: "curved" });
  connect(s, source, b, C.amber, 4, { fromSide: "right", toSide: "left" });
  connect(s, source, c, C.blue, 4, { fromSide: "right", toSide: "left", kind: "curved" });
  connect(s, a, b, C.muted, 2, { fromSide: "bottom", toSide: "top", dashed: true, head: false });
  connect(s, b, c, C.muted, 2, { fromSide: "bottom", toSide: "right", dashed: true, head: false });
  addText(s, "absorbed", 798, 172, 110, 22, { fontSize: 16, bold: true, color: C.cyan, alignment: "center" });
  addText(s, "routed", 956, 272, 94, 22, { fontSize: 16, bold: true, color: C.amber, alignment: "center" });
  addText(s, "residue", 804, 506, 100, 22, { fontSize: 16, bold: true, color: C.blue, alignment: "center" });
  addText(s, "Each state remains inspectable.", 628, 578, 470, 30, { fontSize: 22, bold: true, color: C.muted, alignment: "center" });
  addNotes(s, [
    `${SOURCE_DECK} (source slide: The thesis)`,
    `${REPO}\\Gojo\\docs\\GojoSystemPrompt.md`,
    `${REPO}\\Gojo\\axioms\\Gojo Structure Through Tension.md`,
  ]);
}

// 3 — Requirements
{
  const s = p.slides.add();
  s.background.fill = C.paper;
  addHeader(s, "Requirements", "A numeric kernel must keep what scalars discard", 3, "current model");
  addImageCard(s, IMG.complexity, "image/png", 76, 201, 430, 430,
    "Historical Gojo illustration of a measure surrounded by known and unknown complexity", "contain", C.violet);
  const items = [
    ["Identity", "which participant this is"],
    ["Unit shape", "what one tick promises"],
    ["Resolution", "which distinctions are currently readable"],
    ["Orientation", "face, frame, and signed location stay distinct"],
    ["Topology", "what is actually connected, and in what order"],
    ["Change", "what arrived and what has settled"],
    ["Unknowns", "what stays open, blocked, or unresolved"],
  ];
  let y = 194;
  for (const [lead, rest] of items) {
    addText(s, lead, 570, y, 162, 34, { fontSize: 24, bold: true, color: C.ink });
    addText(s, rest, 738, y + 1, 435, 40, { fontSize: 22, color: C.muted });
    addRule(s, 570, y + 44, 610, C.hair, 1);
    y += 63;
  }
  addNotes(s, [
    `${SOURCE_DECK} (source slide: requirements)`,
    `${REPO}\\Gojo\\docs\\GojoSystemPrompt.md`,
    `${DRIVE}\\Articles\\Numbers.docx and ${DRIVE}\\Articles\\Mathematics Without Infinity.docx (historical concepts)`,
  ]);
}

// 4 — Carrier
{
  const s = p.slides.add();
  s.background.fill = C.paper;
  addHeader(s, "Numeric primitive", "Carrier values live at sites—not in scalars", 4, "implemented kernel");

  addRect(s, 70, 194, 690, 410, C.violet, 26, C.violet, 0);
  addTickLine(s, 136, 356, 538, 12, C.cyan, "0", "12");
  addCircle(s, 434, 326, 64, C.amber, C.white, 4, "carrier-value-site");
  addText(s, "7", 434, 337, 64, 38, { fontSize: 30, bold: true, color: C.violet, alignment: "center", verticalAlignment: "middle" });
  addText(s, "forward face", 132, 286, 150, 26, { fontSize: 18, bold: true, color: C.cyan });
  addText(s, "inverse face", 532, 428, 150, 26, { fontSize: 18, bold: true, color: C.magenta, alignment: "right" });
  const fwd = addCircle(s, 174, 470, 36, C.cyan, C.white, 3);
  const inv = addCircle(s, 632, 470, 36, C.magenta, C.white, 3);
  connect(s, fwd, inv, C.white, 3, { fromSide: "right", toSide: "left", tail: true });
  addText(s, "stable identity • unit shape • attached relations", 136, 541, 538, 28, { fontSize: 19, bold: true, color: C.white, alignment: "center" });

  addText(s, "One carrier currently exposes:", 820, 202, 360, 36, { fontSize: 25, bold: true });
  addText(s,
    "• a stable identity\n• an unsigned visible value\n• signed tension\n• a flow face\n• a stable unit shape\n• attached relations",
    820, 258, 350, 242, { fontSize: 24, color: C.ink, lineSpacing: 1.18 });
  addText(s, "The current implementation uses ulong for visible value and long for tension. Public change enters through tension, not direct assignment.", 820, 520, 354, 92, { fontSize: 21, color: C.muted, lineSpacing: 1.12 });
  addNotes(s, [
    `${REPO}\\Gojo\\Core\\Carrier.cs`,
    `${REPO}\\Gojo\\Core\\CarrierLocation.cs`,
    `${REPO}\\Gojo\\Core\\TickRef.cs`,
    `${REPO}\\Gojo\\Core\\UnitShape.cs`,
  ]);
}

// 5 — Relations
{
  const s = p.slides.add();
  s.background.fill = C.paper;
  addHeader(s, "Relations", "Relations turn isolated values into structure", 5, "implemented kernel");
  addText(s, "A concept exists only when its parts are connected.", 76, 197, 520, 48, { fontSize: 29, bold: true });
  addText(s, "P(C,n) = L(C,n) → C\nS(C,a,b) = P(C,a) → P(C,b)", 78, 272, 520, 100, { fontSize: 27, bold: true, color: C.violet2, typeface: MONO, lineSpacing: 1.35 });
  addText(s, "A → B is primitive topology. Reversing it creates a different ordered frame; coordinate equality, shared-site identity, contact, and projection equality remain different reads.", 78, 410, 520, 152, { fontSize: 24, color: C.muted, lineSpacing: 1.13 });

  addRect(s, 650, 202, 548, 408, C.violet, 26, C.violet, 0);
  const pa = addCircle(s, 742, 335, 48, C.magenta, C.white, 4, "point-a");
  const pb = addCircle(s, 1040, 335, 48, C.cyan, C.white, 4, "point-b");
  connect(s, pa, pb, C.amber, 8, { fromSide: "right", toSide: "left" });
  addText(s, "P(C,a)", 700, 285, 130, 28, { fontSize: 20, bold: true, color: C.white, alignment: "center", typeface: MONO });
  addText(s, "P(C,b)", 998, 285, 130, 28, { fontSize: 20, bold: true, color: C.white, alignment: "center", typeface: MONO });
  addText(s, "ordered segment", 814, 350, 180, 28, { fontSize: 18, bold: true, color: C.amber, alignment: "center" });
  addImageCard(s, IMG.comparison, "image/png", 742, 433, 365, 118,
    "Historical illustration of isolation becoming comparison", "contain", C.white);
  addNotes(s, [
    `${REPO}\\Gojo\\Core\\Relation.cs`,
    `${REPO}\\Gojo\\Reads\\DirectedSegmentReads.cs`,
    `${REPO}\\Gojo\\docs\\GojoSystemPrompt.md`,
    `${DRIVE}\\Articles\\Thinking in Directed Segments.docx (historical visual intuition)`,
  ]);
}

// 6 — Tension and settlement
{
  const s = p.slides.add();
  s.background.fill = C.paper;
  addHeader(s, "Change", "Tension makes every tick accountable", 6, "implemented foothold");
  const n1 = addFlowNode(s, "signed\ndelta", 78, 264, 138, 78, C.magenta);
  const n2 = addFlowNode(s, "carrier", 284, 264, 138, 78, C.violet2);
  const n3 = addFlowNode(s, "local\nrelations", 490, 264, 156, 78, C.blue);
  const n4 = addFlowNode(s, "accepted", 732, 217, 150, 70, C.green);
  const n5 = addFlowNode(s, "blocked", 732, 323, 150, 70, C.red);
  const n6 = addFlowNode(s, "timeline +\nremainder", 968, 264, 196, 78, C.amber, C.violet);
  connect(s, n1, n2, C.magenta, 4, { fromSide: "right", toSide: "left" });
  connect(s, n2, n3, C.violet2, 4, { fromSide: "right", toSide: "left" });
  connect(s, n3, n4, C.green, 4, { fromSide: "right", toSide: "left", kind: "curved" });
  connect(s, n3, n5, C.red, 4, { fromSide: "right", toSide: "left", kind: "curved" });
  connect(s, n4, n6, C.green, 3, { fromSide: "right", toSide: "left", kind: "curved" });
  connect(s, n5, n6, C.red, 3, { fromSide: "right", toSide: "left", kind: "curved" });
  addText(s, "positive tension increases visible value", 78, 437, 350, 30, { fontSize: 22, bold: true, color: C.magenta });
  addText(s, "negative tension draws value toward zero; any unmet draw remains visible", 78, 484, 520, 62, { fontSize: 22, color: C.muted, lineSpacing: 1.12 });
  addText(s, "incoming = accepted + blocked + explicit remainder", 676, 472, 488, 48, { fontSize: 23, bold: true, color: C.violet2, typeface: MONO, alignment: "center" });
  addText(s, "Settlement is local and inspectable. The current carrier loop is synchronous; time-batched settlement remains future work.", 676, 545, 488, 70, { fontSize: 20, color: C.muted, alignment: "center", lineSpacing: 1.1 });
  addNotes(s, [
    `${REPO}\\Gojo\\Core\\Carrier.cs`,
    `${REPO}\\Gojo\\Core\\Carrier.RouteSettlement.cs`,
    `${REPO}\\Gojo\\ConceptSurface\\ParticipationSettlementSurface.cs`,
    `${REPO}\\Gojo\\docs\\ParticipationSettlementReads.md`,
  ]);
}

// 7 — Projection
{
  const s = p.slides.add();
  s.background.fill = C.paper;
  addHeader(s, "Observation", "Projection reads across frames without mutation", 7, "implemented kernel");

  addRect(s, 76, 198, 666, 412, C.violet, 26, C.violet, 0);
  addText(s, "source basis [a,b]", 128, 244, 220, 26, { fontSize: 20, bold: true, color: C.white });
  addTickLine(s, 128, 313, 484, 8, C.cyan, "a", "b");
  const sx = addCircle(s, 336, 288, 52, C.amber, C.white, 3, "source-location-x");
  addText(s, "x", 336, 299, 52, 30, { fontSize: 23, bold: true, color: C.violet, alignment: "center" });
  addText(s, "target basis [c,d]", 128, 410, 220, 26, { fontSize: 20, bold: true, color: C.white });
  addTickLine(s, 128, 480, 484, 5, C.magenta, "c", "d");
  const ty = addCircle(s, 438, 455, 52, C.amber, C.white, 3, "projected-location-y");
  addText(s, "y", 438, 466, 52, 30, { fontSize: 23, bold: true, color: C.violet, alignment: "center" });
  connect(s, sx, ty, C.amber, 4, { fromSide: "bottom", toSide: "top", kind: "curved", dashed: true });
  addText(s, "projection", 390, 371, 120, 22, { fontSize: 17, bold: true, color: C.amber, alignment: "center" });

  addText(s, "y = c + (x-a)(d-c)/(b-a)", 798, 230, 394, 62, { fontSize: 26, bold: true, typeface: MONO, color: C.violet2, alignment: "center" });
  addText(s, "The read returns a result and any residue. Incompatible or unreduced participation stays visible.", 812, 329, 360, 122, { fontSize: 25, color: C.ink, lineSpacing: 1.13 });
  addText(s, "Observation is not transfer. Mutation requires an explicit import, output, or settlement path.", 812, 494, 360, 92, { fontSize: 22, bold: true, color: C.muted, lineSpacing: 1.1 });
  addNotes(s, [
    `${REPO}\\Gojo\\Core\\Relation.Projection.cs`,
    `${REPO}\\Gojo\\Structures\\ReadWitness.cs`,
    `${REPO}\\Gojo\\docs\\GojoReadSpine.md`,
  ]);
}

// 8 — Resolution
{
  const s = p.slides.add();
  s.background.fill = C.paper;
  addHeader(s, "Resolution", "Refinement adds detail; coarse reads stay valid", 8, "implemented + direction");

  addText(s, "coarse read", 78, 228, 180, 26, { fontSize: 21, bold: true, color: C.muted });
  addTickLine(s, 266, 242, 520, 4, C.blue, "0", "4");
  addText(s, "refined read", 78, 356, 180, 26, { fontSize: 21, bold: true, color: C.muted });
  addTickLine(s, 266, 370, 520, 12, C.cyan, "0", "12");
  addText(s, "crystallized tick promise", 78, 484, 220, 52, { fontSize: 21, bold: true, color: C.muted });
  addTickLine(s, 314, 501, 472, 12, C.amber, "0", "12");
  const coarseSite = addCircle(s, 522, 216, 52, C.magenta, C.white, 3);
  const fineSite = addCircle(s, 522, 344, 52, C.magenta, C.white, 3);
  const crystalSite = addCircle(s, 522, 475, 52, C.magenta, C.white, 3);
  connect(s, coarseSite, fineSite, C.magenta, 3, { fromSide: "bottom", toSide: "top", dashed: true });
  connect(s, fineSite, crystalSite, C.magenta, 3, { fromSide: "bottom", toSide: "top", dashed: true });

  addText(s, "A ladder preserves correspondence and history between reads.", 846, 216, 346, 86, { fontSize: 27, bold: true, color: C.ink, lineSpacing: 1.08 });
  addText(s, "Finer structure refines what the coarse read could already say; it does not rewrite the earlier value into a different numeric format.", 846, 335, 346, 126, { fontSize: 23, color: C.muted, lineSpacing: 1.12 });
  addText(s, "Crystallization stabilizes an already readable unit shape. It need not preserve a live derivation or mutable value.", 846, 501, 346, 100, { fontSize: 22, color: C.violet2, lineSpacing: 1.1 });
  addNotes(s, [
    `${REPO}\\Gojo\\Structures\\ResolutionLadder.cs`,
    `${REPO}\\Gojo\\Structures\\StructuralLadderSurface.cs`,
    `${REPO}\\Gojo\\Structures\\RefinementRead.cs`,
    `${REPO}\\Gojo\\axioms\\Gojo Resolution Ladders.md`,
    `${REPO}\\Gojo\\axioms\\Gojo Crystallized Resolution.md`,
  ]);
}

// 9 — Uncertainty
{
  const s = p.slides.add();
  s.background.fill = C.paper;
  addHeader(s, "Uncertainty", "Unknown is not empty—and error is not one thing", 9, "current read model");
  addText(s, "Each unresolved state says something different about what happened.", 78, 198, 650, 46, { fontSize: 27, bold: true });

  const states = [
    ["open slot", C.blue], ["residue", C.cyan], ["illegal lane", C.red],
    ["blocked transfer", C.magenta], ["tension / overflow", C.amber], ["competing candidate", C.violet2],
  ];
  const baseY = 330;
  let prev;
  states.forEach(([label, color], i) => {
    const x = 90 + i * 176;
    const node = addCircle(s, x, baseY, 46, color, C.white, 3, `uncertainty-${i}`);
    if (prev) connect(s, prev, node, C.hair, 3, { fromSide: "right", toSide: "left", head: false });
    addText(s, label, x - 44, baseY + 68, 136, 60, { fontSize: 18, bold: true, color, alignment: "center", lineSpacing: 1.05 });
    prev = node;
  });
  addText(s, "A lower-resolution read is still valid. It must not invent detail to fill an empty-looking field.", 90, 502, 720, 76, { fontSize: 25, color: C.ink, lineSpacing: 1.12 });
  addImageCard(s, IMG.measurementError, "image/png", 854, 482, 330, 150,
    "Historical illustration contrasting measurement error with category error", "contain", C.violet);
  addText(s, "Measurement error is only one case.", 856, 448, 326, 26, { fontSize: 17, bold: true, color: C.muted, alignment: "center" });
  addNotes(s, [
    `${SOURCE_DECK} (source slide: uncertainty)`,
    `${REPO}\\Gojo\\docs\\GojoSystemPrompt.md`,
    `${REPO}\\Gojo\\docs\\GojoReadSpine.md`,
    `${DRIVE}\\presentations\\ComplexitySpaces.pptx (historical measurement/category-error illustration)`,
  ]);
}

// 10 — Communication and scaling
{
  const s = p.slides.add();
  s.background.fill = C.paper;
  addHeader(s, "Communication + scaling", "Small systems compose through explicit surfaces", 10, "documented composition spine");
  const labels = ["source", "port / read", "projection", "basis + lane", "transfer", "capacity", "settlement", "timeline", "residue"];
  const colors = [C.violet2, C.blue, C.cyan, C.amber, C.magenta, C.red, C.green, C.blue, C.muted];
  const nodes = [];
  labels.forEach((label, i) => {
    const row = i < 5 ? 0 : 1;
    const col = row === 0 ? i : i - 5;
    const x = row === 0 ? 78 + col * 222 : 190 + col * 240;
    const y = row === 0 ? 236 : 430;
    nodes.push(addFlowNode(s, label, x, y, 154, 66, colors[i], C.white, `spine-${i}`));
  });
  for (let i = 0; i < 4; i += 1) connect(s, nodes[i], nodes[i + 1], C.hair, 3, { fromSide: "right", toSide: "left" });
  connect(s, nodes[4], nodes[5], C.magenta, 3, { fromSide: "bottom", toSide: "top", kind: "curved" });
  for (let i = 5; i < 8; i += 1) connect(s, nodes[i], nodes[i + 1], C.hair, 3, { fromSide: "right", toSide: "left" });
  addText(s, "Keep projection, admissibility, transfer, capacity, settlement, and time as separate stages.", 110, 548, 1060, 42, { fontSize: 26, bold: true, color: C.ink, alignment: "center" });
  addText(s, "Observation, draining transfer, and coupling must remain structurally different.", 110, 606, 1060, 34, { fontSize: 21, color: C.muted, alignment: "center" });
  addNotes(s, [
    `${SOURCE_DECK} (source slide: communication / scaling)`,
    `${REPO}\\Gojo\\docs\\GojoSystemPrompt.md (composition spine)`,
    `${REPO}\\Gojo\\ConceptSurface\\BoundaryFlowRoutesAndTransfers.cs`,
    `${REPO}\\Gojo\\ConceptSurface\\BoundaryFlowCapacity.cs`,
    `${REPO}\\Gojo\\Structures\\TimeStepSequence.cs`,
  ]);
}

// 11 — Context and perspectives
{
  const s = p.slides.add();
  s.background.fill = C.paper;
  addHeader(s, "Context + perspectives", "Equal visible values can remain structurally different", 11, "current model");
  addText(s, "4 ticks", 1010, 345, 128, 42, { fontSize: 31, bold: true, color: C.ink, alignment: "center" });
  const result = addCircle(s, 1044, 402, 62, C.amber, C.white, 4, "equal-visible-result");
  const a = addFlowNode(s, "source A", 114, 234, 154, 66, C.blue);
  const b = addFlowNode(s, "source B", 114, 464, 154, 66, C.magenta);
  const ar = addFlowNode(s, "route α\ncoarse unit", 390, 220, 180, 94, C.violet2);
  const br = addFlowNode(s, "route β\nrefined unit", 390, 450, 180, 94, C.violet2);
  const ah = addFlowNode(s, "history A", 690, 234, 154, 66, C.cyan);
  const bh = addFlowNode(s, "history B", 690, 464, 154, 66, C.green);
  connect(s, a, ar, C.blue, 4, { fromSide: "right", toSide: "left" });
  connect(s, ar, ah, C.cyan, 4, { fromSide: "right", toSide: "left" });
  connect(s, ah, result, C.cyan, 4, { fromSide: "right", toSide: "left", kind: "curved" });
  connect(s, b, br, C.magenta, 4, { fromSide: "right", toSide: "left" });
  connect(s, br, bh, C.green, 4, { fromSide: "right", toSide: "left" });
  connect(s, bh, result, C.green, 4, { fromSide: "right", toSide: "left", kind: "curved" });
  addText(s, "Meaning is recoverable from identity, route, unit, resolution, repetition, history, and provenance—not from the final scalar alone.", 185, 584, 890, 72, { fontSize: 24, color: C.muted, alignment: "center", lineSpacing: 1.1 });
  addNotes(s, [
    `${SOURCE_DECK} (source slides: context; perspectives)`,
    `${REPO}\\Gojo\\Structures\\ReadWitness.cs`,
    `${REPO}\\Gojo\\Structures\\BasisReadSurface.cs`,
    `${REPO}\\Gojo\\docs\\GojoReadSpine.md`,
  ]);
}

// 12 — Why Gojo
{
  const s = p.slides.add();
  s.background.fill = C.paper;
  addHeader(s, "Why Gojo", "Interpretability belongs inside the number", 12, "current synthesis");
  addImageCard(s, IMG.distributed, "image/png", 74, 198, 526, 406,
    "Historical contrast between a top-down monolith and a distributed self-correcting mesh", "contain", C.violet);
  addText(s, "The structure can expose:", 656, 204, 470, 34, { fontSize: 27, bold: true });
  addText(s,
    "• what was selected\n• which surface or basis read it\n• what result settled\n• what residue or tension remains\n• which route and history produced it",
    656, 265, 486, 228, { fontSize: 25, color: C.ink, lineSpacing: 1.18 });
  addText(s, "That gives search, heuristics, ML, and later design agents a shared inspectable target without turning one classifier or dispatcher into the source of meaning.", 656, 518, 486, 103, { fontSize: 22, color: C.muted, lineSpacing: 1.1 });
  addText(s, "912 current kernel, encoding, and CAD tests pass", 78, 635, 520, 28, { fontSize: 17, bold: true, color: C.amber, alignment: "center" });
  addNotes(s, [
    `${SOURCE_DECK} (source slide: Why gojo)`,
    `${REPO}\\Gojo\\docs\\GojoReadSpine.md`,
    `${REPO}\\Gojo\\Structures\\ReadWitness.cs`,
    `Verification on 2026-09-01: Gojo.Tests 632 passed; Gojo.Encoding.Tests 134 passed; GojoCAD.Tests 146 passed. Full Gojo.slnx requires the local wasm-tools workload because it includes Gojo.Blocks.Browser.`,
  ]);
}

// 13 — Why CAD
{
  const s = p.slides.add();
  s.background.fill = C.paper;
  addHeader(s, "Why CAD", "CAD makes numeric structure meet reality", 13, "application frontier");
  addImageCard(s, IMG.pcbProcess, "image/jpeg", 72, 198, 628, 425,
    "CopperKit board planning beside a live PCB milling operation", "cover", C.violet);
  addText(s, "Geometry, units, resolution, tolerance, provenance, manufacturing limits, and cost are not decorations around a number. They determine what the number means.", 754, 210, 420, 154, { fontSize: 25, bold: true, color: C.ink, lineSpacing: 1.1 });
  addText(s, "A Gojo-native ECAD structure can connect board setup, tracks, holes, nets, pins, software, fab rules, and uncertainty without flattening them into unrelated fields.", 754, 406, 420, 140, { fontSize: 23, color: C.muted, lineSpacing: 1.12 });
  addText(s, "GojoCAD and GojoDraw are implemented footholds. KiCad-to-Gojo import is the next application pass.", 754, 576, 420, 70, { fontSize: 20, bold: true, color: C.amber, lineSpacing: 1.08 });
  addNotes(s, [
    `${SOURCE_DECK} (source slide: Why cad)`,
    `${DRIVE}\\presentations\\scratch-process-poster.jpg (user-provided visual)`,
    `${REPO}\\GojoCAD\\README.md`,
    `${REPO}\\GojoDraw\\README.md`,
    `${REPO}\\GojoCAD\\structures\\EcadDocumentarySurfaces.cs`,
  ]);
}

// 14 — Goal
{
  const s = p.slides.add();
  s.background.fill = C.paper;
  addHeader(s, "The goal", "A living design can refine without erasing its history", 14, "next proof");
  addText(s, "Start small", 78, 208, 210, 32, { fontSize: 27, bold: true, color: C.magenta });
  addText(s, "Parse a small KiCad board fragment: selected setup values, a few tracks, and a few holes.", 78, 254, 430, 82, { fontSize: 23, color: C.ink, lineSpacing: 1.12 });
  addText(s, "Preserve the unknowns", 78, 376, 290, 32, { fontSize: 27, bold: true, color: C.cyan });
  addText(s, "Carry real resolution, orientation, missing structure, and provenance into a readable Gojo serialization.", 78, 422, 430, 88, { fontSize: 23, color: C.ink, lineSpacing: 1.12 });
  addText(s, "Let evidence refine it", 78, 548, 270, 32, { fontSize: 27, bold: true, color: C.amber });
  addText(s, "Coarsen, refine, crystallize, and reconnect the design as new constraints arrive.", 78, 594, 430, 62, { fontSize: 23, color: C.ink, lineSpacing: 1.08 });
  addImageCard(s, IMG.pcbProbe, "image/jpeg", 580, 196, 630, 352,
    "A small CopperKit continuity probe PCB", "cover", C.violet);
  addText(s, "The artifact is not a final answer. It is a structure that can keep settling.", 592, 584, 606, 68, { fontSize: 26, bold: true, color: C.violet2, alignment: "center", lineSpacing: 1.08 });
  addNotes(s, [
    `${SOURCE_DECK} (source slide: The goal)`,
    `${DRIVE}\\presentations\\continuity-probe-poster.jpg (user-provided visual)`,
    `${REPO}\\Gojo\\docs\\StructuralAndConceptualLadders.md`,
    `${REPO}\\Gojo.Encoding\\IntentRefinementLadder.md`,
    `${REPO}\\GojoCAD\\docs\\FormatPlan.md`,
  ]);
}

await fs.mkdir(QA, { recursive: true });
for (let i = 0; i < p.slides.items.length; i += 1) {
  const slide = p.slides.getItem(i);
  await writeBlob(path.join(QA, `slide-${String(i + 1).padStart(2, "0")}.png`), await slide.export({ format: "png", scale: 1.5 }));
  await fs.writeFile(path.join(QA, `slide-${String(i + 1).padStart(2, "0")}.layout.json`), await (await slide.export({ format: "layout" })).text());
}
await writeBlob(path.join(QA, "montage.webp"), await p.export({ format: "webp", montage: true, scale: 1 }));
const pptx = await PresentationFile.exportPptx(p);
await pptx.save(OUT);
console.log(JSON.stringify({ output: OUT, slides: p.slides.items.length, qa: QA }, null, 2));
