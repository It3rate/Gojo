// Shared local browser harness for the compiled Primitive Explorer.
const { createServer } = require('node:http');
const fs = require('node:fs/promises');
const path = require('node:path');

async function serve(root) {
  const server = createServer(async (req, res) => {
    try {
      const request = new URL(req.url, 'http://localhost');
      let file = path.resolve(root, '.' + decodeURIComponent(request.pathname));
      if (!file.startsWith(root + path.sep)) throw Error('Outside site');
      if ((await fs.stat(file)).isDirectory()) file = path.join(file, 'index.html');
      res.setHeader('Content-Type', ({'.js':'text/javascript','.wasm':'application/wasm','.html':'text/html',
        '.css':'text/css','.json':'application/json','.svg':'image/svg+xml','.csv':'text/csv'})[path.extname(file)] || 'application/octet-stream');
      res.end(await fs.readFile(file));
    } catch { res.writeHead(404); res.end(); }
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  return server;
}

async function firstSelectorY(page) {
  // Locate the interior of the first white selector against the surrounding cream panel.
  const shot = await page.screenshot();
  return page.evaluate(async png => {
    const img = new Image();
    img.src = 'data:image/png;base64,' + png; await img.decode();
    const canvas = document.createElement('canvas');
    canvas.width = img.width; canvas.height = img.height;
    const ctx = canvas.getContext('2d'); ctx.drawImage(img, 0, 0);
    const data = ctx.getImageData(268, 195, 1, 105).data;
    let white = 0;
    for (let row = 0; row < 105; row++) {
      const i = row * 4;
      white = data[i] > 252 && data[i+1] > 252 && data[i+2] > 252 ? white+1 : 0;
      if (white === 15) return 195 + row;
    }
    return null;
  }, shot.toString('base64'));
}

module.exports = { serve, firstSelectorY };
