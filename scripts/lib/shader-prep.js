/* Minimal copy of Macroverse prepareFragmentForOffscreenRender + helpers (no server). */
(function (global) {
  const ISF_PREAMBLE_NAMES = new Set([
    'TIME', 'RENDERSIZE', 'FRAMEINDEX', 'iFrame',
    'useFrameIndex', 'fps', 'timeScale', 'mouseX', 'mouseY',
    'uTimeScale', 'uMouse',
  ]);

  function extractUniformNames(preamble) {
    const names = new Set();
    const re = /uniform\s+\w+\s+(\w+)\s*[;=]/g;
    let m;
    while ((m = re.exec(preamble)) !== null) names.add(m[1]);
    return names;
  }

  function stripDuplicateUniformDecls(body, preamble) {
    const declared = extractUniformNames(preamble);
    if (declared.size === 0) return body;
    return body.replace(/^\s*uniform\s+\w+\s+(\w+)\s*[;=][^\n]*/gm, (line, name) => {
      return declared.has(name) ? '' : line;
    });
  }

  function isfInputsToUniforms(body) {
    const blockMatch = body.match(/\/\*\s*\{[\s\S]*?\}\s*\*\//);
    if (!blockMatch) return '';
    const jsonStr = blockMatch[0].replace(/^\s*\/\*\s*/, '').replace(/\s*\*\/\s*$/, '');
    try {
      const meta = JSON.parse(jsonStr);
      const inputs = meta.INPUTS;
      if (!Array.isArray(inputs) || inputs.length === 0) return '';
      const lines = [];
      for (const inp of inputs) {
        const name = inp.NAME;
        if (!name || typeof name !== 'string' || !/^\w+$/.test(name)) continue;
        if (ISF_PREAMBLE_NAMES.has(name)) continue;
        const t = (inp.TYPE || 'float').toLowerCase();
        let glslType = 'float';
        if (t === 'bool') glslType = 'bool';
        else if (t === 'vec2' || t === 'point2d') glslType = 'vec2';
        else if (t === 'vec3' || t === 'color') glslType = 'vec3';
        else if (t === 'vec4') glslType = 'vec4';
        else if (t === 'image' || t === 'sampler2d') glslType = 'sampler2D';
        lines.push('uniform ' + glslType + ' ' + name + ';');
      }
      return lines.length ? lines.join('\n') + '\n' : '';
    } catch {
      return '';
    }
  }

  function stripLeadingGarbage(src) {
    if (!src || typeof src !== 'string') return src;
    const lines = src.split('\n');
    let i = 0;
    while (i < lines.length) {
      const t = lines[i].trim();
      if (t === '') {
        i++;
        continue;
      }
      if (/^#|^precision\s|^\/\/|^\/\*|^uniform\s|^varying\s|^attribute\s|^void\s|^const\s|^layout\s|^in\s|^out\s|^flat\s|^smooth\s|^float\s|^vec[234]\s|^mat[234]\s|^int\s|^bool\s|^sampler2D\s|^if\s|^for\s|^while\s|^return\s|^discard\s|^struct\s|^\{\s*$/i.test(t)) break;
      if (/^[a-zA-Z_][a-zA-Z0-9_]*$/.test(t) && t.length > 10) {
        i++;
        continue;
      }
      break;
    }
    if (i === 0) return src;
    return lines.slice(i).join('\n');
  }

  function prepareFragmentForOffscreenRender(src) {
    let body = src || '';
    body = body.replace(/#\s*(extension|version)\s+[^\n]+/g, '');
    body = body.replace(/\n\s*\n\s*\n/g, '\n\n').trim();
    const hasSandboxUniforms = /uniform\s+float\s+time\s*;/.test(body) || /uniform\s+vec2\s+(mouse|resolution)\s*;/.test(body);
    const hasISF = /"INPUTS"\s*:/.test(body) && (/\bTIME\b/.test(body) || /\bRENDERSIZE\b/.test(body));
    const hasOurPreamble = /uniform\s+float\s+TIME\s*;/.test(body);
    const needsPreamble = !hasOurPreamble && !hasSandboxUniforms;
    body = body.replace(/\s*precision\s+(lowp|mediump|highp)\s+float\s*;\s*/gi, '\n');
    const usesRendersize = /\bRENDERSIZE\b|\bresolution\b|\biResolution\b/.test(body);
    const declaresRendersize =
      /uniform\s+vec2\s+RENDERSIZE\s*[;=]/.test(body) ||
      /uniform\s+vec2\s+resolution\s*[;=]/.test(body) ||
      /#define\s+RENDERSIZE\s+/.test(body);
    if (usesRendersize && declaresRendersize) {
      body = body.replace(/^\s*uniform\s+vec2\s+RENDERSIZE\s*[;=]\s*\n?/gm, '');
      body = body.replace(/^\s*uniform\s+vec2\s+resolution\s*[;=]\s*\n?/gm, '');
      body = body.replace(/^\s*#ifndef\s+RENDERSIZE\s*\n#define\s+RENDERSIZE\s+[^\n]+\n#endif\s*\n?/gm, '');
    }
    const needsRendersize = usesRendersize;
    const rendersizeDecl = needsRendersize ? 'uniform vec2 RENDERSIZE;\n' : '';
    const preamble =
      'uniform float TIME;\n' +
      rendersizeDecl +
      'uniform float uTimeScale;\nuniform vec2 uMouse;\nuniform float iFrame;\n#ifndef time\n#define time (TIME * uTimeScale)\n#endif\n#ifndef resolution\n#define resolution RENDERSIZE\n#endif\n#ifndef mouse\n#define mouse uMouse\n#endif\n#ifndef iGlobalTime\n#define iGlobalTime TIME\n#endif\n#ifndef iTime\n#define iTime TIME\n#endif\n#ifndef iResolution\n#define iResolution RENDERSIZE\n#endif\n#ifndef iMouse\n#define iMouse vec4(uMouse,0.,0.)\n#endif\n#ifndef iTimeDelta\n#define iTimeDelta 0.016\n#endif\n';
    const hasTimeScale = /\buniform\s+float\s+timeScale\s*[;=]|\bfloat\s+timeScale\s*[=;]|\b#define\s+timeScale\b/.test(body);
    const hasMouseX = /\buniform\s+float\s+mouseX\s*[;=]|\bfloat\s+mouseX\s*[=;]|\b#define\s+mouseX\b/.test(body);
    const hasMouseY = /\buniform\s+float\s+mouseY\s*[;=]|\bfloat\s+mouseY\s*[=;]|\b#define\s+mouseY\b/.test(body);
    let isfPreamble =
      'uniform float TIME;\n' +
      (needsRendersize ? 'uniform vec2 RENDERSIZE;\n' : '') +
      'uniform float FRAMEINDEX;\nuniform float iFrame;\nuniform bool useFrameIndex;\nuniform float fps;\n';
    if (!hasTimeScale) isfPreamble += 'uniform float timeScale;\n';
    if (!hasMouseX) isfPreamble += 'uniform float mouseX;\n';
    if (!hasMouseY) isfPreamble += 'uniform float mouseY;\n';
    if (hasISF) isfPreamble += isfInputsToUniforms(body);
    const usesIFrame = /\biFrame\b/.test(body);
    const declaresIFrame = /uniform\s+float\s+iFrame\s*[;=]/.test(body);
    const needsIFrame = usesIFrame && !declaresIFrame;
    const rendersizeBlock = needsRendersize
      ? 'uniform vec2 RENDERSIZE;\n#ifndef resolution\n#define resolution RENDERSIZE\n#endif\n#ifndef iResolution\n#define iResolution RENDERSIZE\n#endif\n'
      : '';
    const iFrameLine = needsIFrame ? 'uniform float iFrame;\n' : '';
    let rest = body;
    if (hasISF && !hasOurPreamble) {
      rest = stripDuplicateUniformDecls(rest, isfPreamble);
      const insert = rest.indexOf('*/') >= 0 && rest.indexOf('*/') < 800 ? rest.indexOf('*/') + 2 : 0;
      rest = rest.slice(0, insert) + '\n' + isfPreamble + rest.slice(insert);
    } else if (needsPreamble) {
      const insert = rest.indexOf('*/') >= 0 && rest.indexOf('*/') < 500 ? rest.indexOf('*/') + 2 : 0;
      rest = rest.slice(0, insert) + '\n' + preamble + rest.slice(insert);
    } else {
      const insert = rest.indexOf('*/') >= 0 && rest.indexOf('*/') < 500 ? rest.indexOf('*/') + 2 : 0;
      const toAdd = rendersizeBlock + iFrameLine;
      if (toAdd) rest = rest.slice(0, insert) + '\n' + toAdd + rest.slice(insert);
    }
    return 'precision highp float;\n' + rest;
  }

  global.ShaderPrep = {
    stripLeadingGarbage,
    prepareFragmentForOffscreenRender,
  };
})(typeof window !== 'undefined' ? window : globalThis);
