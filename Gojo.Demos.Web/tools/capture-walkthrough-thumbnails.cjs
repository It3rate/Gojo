// Capture SVG from the original walkthrough renderer; no duplicate geometry.
// Uses the same GOJO_PLAYWRIGHT_MODULE and GOJO_BROWSER options as check-syntax.cjs.
const {chromium}=require(process.env.GOJO_PLAYWRIGHT_MODULE||'playwright');
const fs=require('node:fs/promises'),path=require('node:path');
const {pathToFileURL}=require('node:url');
(async()=>{
  const {walkthroughs}=JSON.parse(await fs.readFile(path.join(__dirname,'syntax-walkthroughs.json'),'utf8'));
  const output=path.join(__dirname,'walkthrough-thumbnails');
  await fs.mkdir(output,{recursive:true});
  const browser=await chromium.launch({headless:true,executablePath:process.env.GOJO_BROWSER});
  try{
    const page=await browser.newPage();
    for(const d of walkthroughs){
      const url=pathToFileURL(path.resolve(__dirname,'../../Gojo/docs/examples',d.source));
      url.searchParams.set('step',d.thumbnailStep);
      await page.goto(url.href);
      const svg=await page.locator('svg').first().evaluate((source,viewBox)=>{
        const copy=source.cloneNode(true);
        copy.setAttribute('xmlns','http://www.w3.org/2000/svg');
        copy.removeAttribute('id');
        if(viewBox)copy.setAttribute('viewBox',viewBox);
        // External CSS is unavailable when the SVG is loaded as an image.
        const texts=[...source.querySelectorAll('text')];
        copy.querySelectorAll('text').forEach((node,i)=>{
          const style=getComputedStyle(texts[i]);
          for(const property of ['font-family','font-size','font-weight','fill'])node.setAttribute(property,style.getPropertyValue(property));
        });
        return copy.outerHTML+'\n';
      },d.thumbnailViewBox);
      await fs.writeFile(path.join(output,d.id+'.svg'),svg);
    }
    console.log('Captured '+walkthroughs.length+' walkthrough thumbnails from their original SVG renderers.');
  }finally{await browser.close();}
})().catch(error=>{console.error(error);process.exitCode=1;});
