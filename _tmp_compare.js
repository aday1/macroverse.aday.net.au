const fs = require('fs');

const ISF_ID = '77697265-4576-4C11-899B-6F11F3275D36';

function getFirstISF(file) {
  const data = JSON.parse(fs.readFileSync(file, 'utf8'));
  const entry = Object.entries(data.patch.nodes).find(([k, n]) => n.class && n.class.id === ISF_ID);
  return { node: entry ? entry[1] : null, key: entry ? entry[0] : null, data };
}

// Template analysis
const tmplData = JSON.parse(fs.readFileSync('resolume/tmpl-gen-plasma5.wire', 'utf8'));
console.log('=== TEMPLATE FULL PATCH ===');
console.log('patch.inputOrder:', JSON.stringify(tmplData.patch.inputOrder));
console.log('patch.meta:', JSON.stringify(tmplData.patch.meta, null, 2));
console.log('patch.ui:', JSON.stringify(tmplData.patch.ui, null, 2));
console.log('All connections:');
tmplData.patch.connections.forEach(c => console.log(JSON.stringify(c)));
console.log();

console.log('=== ALL NODES (non-ISF) in TEMPLATE ===');
Object.entries(tmplData.patch.nodes)
  .filter(([k, n]) => n.class.id !== ISF_ID)
  .forEach(([k, n]) => {
    console.log('Node key:', k, 'class:', JSON.stringify(n.class));
    console.log(JSON.stringify(n, null, 2));
  });

console.log('\n======================================');
console.log('DETAILED STRUCTURAL DIFF SUMMARY');
console.log('======================================\n');

const ref = getFirstISF('resolume/ISF-Test.wire');
const gen = getFirstISF('resolume-example/vj-ambient-1.wire');
const tmpl = getFirstISF('resolume/tmpl-gen-plasma5.wire');

// bounds key order
console.log('1. BOUNDS KEY ORDER:');
console.log('   Reference (Resolume-made):', Object.keys(ref.node.bounds).join(', '), '=> alphabetical: height,width,x,y');
console.log('   Generated (our script):   ', Object.keys(gen.node.bounds).join(', '), '=> positional: x,y,width,height');
console.log('   Template (our script):    ', Object.keys(tmpl.node.bounds).join(', '), '=> positional: x,y,width,height');
console.log('   DIFFERENCE: Reference uses alphabetical key order, our scripts use x,y,width,height order');
console.log();

// bounds size
console.log('2. BOUNDS SIZE:');
console.log('   Reference: width=' + ref.node.bounds.width + ' height=' + ref.node.bounds.height);
console.log('   Generated: width=' + gen.node.bounds.width + ' height=' + gen.node.bounds.height);
console.log('   Template:  width=' + tmpl.node.bounds.width + ' height=' + tmpl.node.bounds.height);
console.log('   DIFFERENCE: Reference=195x178 (Resolume auto-sizes), Generated=195x178 (matches), Template=160x100 (smaller)');
console.log();

// hidden order
console.log('3. HIDDEN ARRAY ORDER:');
console.log('   Reference:', JSON.stringify(ref.node.hidden));
console.log('   Generated:', JSON.stringify(gen.node.hidden));
console.log('   Template: ', JSON.stringify(tmpl.node.hidden));
console.log('   DIFFERENCE: Reference has alphabetical order (bitdepth first), Template has instances first');
console.log('   Generated matches Reference order.');
console.log();

// attributes key order
console.log('4. ATTRIBUTES KEY ORDER:');
console.log('   Reference:', Object.keys(ref.node.attributes).join(', '));
console.log('   Generated:', Object.keys(gen.node.attributes).join(', '));
console.log('   Template: ', Object.keys(tmpl.node.attributes).join(', '));
console.log('   DIFFERENCE: Reference alphabetical, Template has instances first');
console.log();

// constants
console.log('5. CONSTANTS KEY ORDER:');
console.log('   Reference:', Object.keys(ref.node.constants).join(', '));
console.log('   Generated:', Object.keys(gen.node.constants).join(', '));
console.log('   Template: ', Object.keys(tmpl.node.constants).join(', '));
console.log('   Reference has alphabetical order. Generated has: bypass, time, useFrameIndex, fps, timeScale, mouseX, mouseY');
console.log();

// input key order and extra fields
console.log('6. ISF INPUT FIELD STRUCTURE (useFrameIndex bool input):');
const refI = ref.node.attributes['fragment-shader'].value.value.inputs[0];
const genI = gen.node.attributes['fragment-shader'].value.value.inputs[0];
const tmplI = tmpl.node.attributes['fragment-shader'].value.value.inputs[0];
console.log('   Reference keys:', Object.keys(refI).join(', '));
console.log('   Generated keys:', Object.keys(genI).join(', '));
console.log('   Template keys: ', Object.keys(tmplI).join(', '));
console.log('   DIFFERENCES:');
console.log('     - Reference: alphabetical key order (defaultValue,label,name,type), no min/max');
console.log('     - Generated: name-first order, no min/max (matches Reference structure, different key order)');
console.log('     - Template:  has EXTRA min/max fields (0,1) for bool type -- NOT present in reference');
console.log('     - Template:  defaultValue=0 (number) vs Reference/Generated defaultValue=false (boolean)');
console.log();

// float precision issue
console.log('7. FLOAT PRECISION (timeScale min value):');
const refTS = ref.node.attributes['fragment-shader'].value.value.inputs.find(i => i.name === 'timeScale');
const genTS = gen.node.attributes['fragment-shader'].value.value.inputs.find(i => i.name === 'timeScale');
console.log('   Reference: min=' + (refTS ? refTS.min : 'N/A') + '  (float32 artifact: 0.10000000149011612)');
console.log('   Generated: min=' + (genTS ? genTS.min : 'N/A') + '  (clean: 0.1)');
console.log('   DIFFERENCE: Resolume preserved float32 precision artifact, our scripts use clean float64 values');
console.log();

// name
console.log('8. NODE NAME:');
console.log('   Reference: "ISF" (Resolume always uses "ISF")');
console.log('   Generated: "ABlackSUN" (shader filename without extension)');
console.log('   Template:  "ISF: plasma5" (prefixed with "ISF: ")');
console.log('   IMPORTANT: Resolume-made nodes just say "ISF" - the name does not include the shader filename');
console.log();

// constants values
console.log('9. CONSTANTS VALUES:');
console.log('   Reference has user-adjusted values (e.g. fps=81.14, mouseX=0.595, timeScale=1.63)');
console.log('   Generated has defaults (e.g. fps=60, mouseX=0.5, timeScale=1)');
console.log('   This is expected - reference was used/tweaked in Resolume');
console.log();

// inputOrder
console.log('10. patch.inputOrder:');
console.log('    Reference:', JSON.stringify(ref.data.patch.inputOrder));
console.log('    Generated:', JSON.stringify(gen.data.patch.inputOrder));
console.log('    Template: ', JSON.stringify(tmplData.patch.inputOrder));
console.log();

// meta differences
console.log('11. patch.meta DIFFERENCES:');
console.log('    Reference saveTarget version: 7.24.3');
console.log('    Note: Templates and generated files will have their own meta');
