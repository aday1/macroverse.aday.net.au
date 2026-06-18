/*{
    "DESCRIPTION": "EnhancedTest",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "fractal"
    ],
    "INPUTS": [
        {
            "NAME": "useFrameIndex",
            "TYPE": "bool",
            "DEFAULT": 0,
            "LABEL": "Use frame index (timeline sync)"
        },
        {
            "NAME": "fps",
            "TYPE": "float",
            "DEFAULT": 60.0,
            "MIN": 24.0,
            "MAX": 120.0
        },
        {
            "NAME": "intensity",
            "TYPE": "float",
            "MIN": 0.0,
            "MAX": 5.0,
            "DEFAULT": 1.0,
            "LABEL": "Intensity"
        },
        {
            "NAME": "frequency",
            "TYPE": "float",
            "MIN": 0.0,
            "MAX": 5.0,
            "DEFAULT": 1.0,
            "LABEL": "Frequency"
        },
        {
            "NAME": "scale",
            "TYPE": "float",
            "MIN": 0.0,
            "MAX": 5.0,
            "DEFAULT": 1.0,
            "LABEL": "Scale"
        },
        {
            "NAME": "rotation",
            "TYPE": "float",
            "MIN": 0.0,
            "MAX": 5.0,
            "DEFAULT": 1.0,
            "LABEL": "Rotation"
        }
    ],
    "TAGS": [
        "fractal"
    ]
}*/

// Enhanced test shader with custom parameters
precision highp float;
uniform float time;
uniform vec2 mouse, resolution;

// Custom uniform parameters that will be exposed in Resolume
uniform float intensity;    // Controls effect intensity (0.0 to 1.0)
uniform float frequency;    // Controls animation frequency (0.0 to 1.0)
uniform float scale;        // Controls pattern scale (0.0 to 1.0)
uniform float rotation;     // Controls rotation speed (0.0 to 1.0)
uniform bool invert;        // Boolean parameter for effect inversion

void main( void ) {
    vec2 uv = (gl_FragCoord.xy - 0.5 * resolution.xy) / resolution.y;
    
    // Apply custom parameters
    float timeScale = time * (0.5 + frequency * 2.0);
    float patternScale = 0.5 + scale * 3.0;
    float effectIntensity = 0.1 + intensity * 2.0;
    
    // Create rotating pattern
    float angle = timeScale * rotation * 2.0;
    mat2 rot = mat2(cos(angle), -sin(angle), sin(angle), cos(angle));
    uv = rot * uv * patternScale;
    
    // Generate fractal-like pattern
    vec3 color = vec3(0.0);
    for(int i = 0; i < 5; i++) {
        float fi = float(i);
        vec2 p = uv * pow(2.0, fi);
        float d = length(fract(p) - 0.5);
        color += exp(-d * effectIntensity) / pow(2.0, fi);
    }
    
    // Apply mouse interaction
    vec2 mousePos = mouse * 2.0 - 1.0;
    mousePos.x *= resolution.x / resolution.y;
    float mouseDist = length(uv - mousePos);
    color *= (1.0 - smoothstep(0.0, 0.3, mouseDist));
    
    // Apply inversion if enabled
    if (invert) {
        color = 1.0 - color;
    }
    
    // Add time-based color variation
    color.r += sin(timeScale) * 0.3;
    color.g += cos(timeScale * 1.3) * 0.3;
    color.b += sin(timeScale * 0.7) * 0.3;
    
    gl_FragColor = vec4(color, 1.0);
}
