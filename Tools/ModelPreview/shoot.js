// Рендер модели с нескольких ракурсов: node shoot.js <dir> <name> [paint]
const { chromium } = require('/opt/node22/lib/node_modules/playwright');
const path = require('path');
const http = require('http'); const fs = require('fs');
const dir = process.argv[2], out = process.argv[3] || 'shot', paint = process.argv[4] || '#c8141e';
const views = JSON.parse(process.argv[5] || 'null') || {
  fl: 'pos=5.2,1.6,5.0', rl: 'pos=-5.0,1.7,-5.2&at=0,0.6,-0.2', side: 'pos=8.5,0.75,0&at=0,0.6,0&fov=30',
  front: 'pos=0,0.9,8&at=0,0.6,0&fov=24', rear: 'pos=0,1.0,-8&at=0,0.6,0&fov=24', top: 'pos=0.01,9,0&at=0,0,0&fov=30',
};
const server = http.createServer((req, res) => {
  const p = path.join(dir, decodeURIComponent(req.url.split('?')[0]));
  fs.readFile(p, (e, d) => { if (e) { res.writeHead(404); res.end(); return; }
    res.writeHead(200, { 'Content-Type': p.endsWith('.js') ? 'text/javascript' : p.endsWith('.html') ? 'text/html' : 'application/json' }); res.end(d); });
}).listen(8765);
(async () => {
  const browser = await chromium.launch({ executablePath: '/opt/pw-browsers/chromium-1194/chrome-linux/chrome', args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
  const page = await browser.newPage({ viewport: { width: 1000, height: 620 } });
  page.on('console', m => { if (m.type() === 'error') console.log('console:', m.text()); });
  for (const [name, q] of Object.entries(views)) {
    const qs = new URLSearchParams(q);
    await page.setViewportSize({ width: +(qs.get('w') || 1000), height: +(qs.get('h') || 620) });
    await page.goto(`http://localhost:8765/view.html?${q.includes('paint=') ? '' : 'paint=' + encodeURIComponent(paint) + '&'}${q}`);
    await page.waitForFunction(() => document.title.startsWith('done'), null, { timeout: 120000 });
    const t = await page.title(); if (t.length > 5) console.log(name, t);
    await page.screenshot({ path: path.join(dir, `${out}_${name}.png`) });
  }
  await browser.close(); server.close();
})();
