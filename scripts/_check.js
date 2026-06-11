const fs = require('fs');
const path = require('path');
const dirs = ['resolume', 'resolume-example'];
let flowIssues = [];
let dimIssues = 0;
for (const dir of dirs) {
  if (!fs.existsSync(dir)) continue;
  const files = fs.readdirSync(dir).filter(f => f.endsWith('.wire'));
  for (const f of files) {
    const d = JSON.parse(fs.readFileSync(path.join(dir, f), 'utf-8'));
    for (const [id, n] of Object.entries(d.patch.nodes)) {
      for (const a of Object.keys(n.attributes || {})) {
        if (a.includes('flow')) {
          flowIssues.push(`${dir}/${f} node ${id} (${n.name}) attr: ${a}`);
        }
        if (a.includes('dimensions')) {
          dimIssues++;
        }
      }
    }
  }
}
console.log('Flow attributes:', flowIssues.length);
flowIssues.slice(0, 20).forEach(i => console.log('  ' + i));
console.log('Dimension attributes:', dimIssues);
