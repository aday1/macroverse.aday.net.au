/*{
    "DESCRIPTION": "OrbitFX",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "3d"
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
            "NAME": "orbitSpeed",
            "TYPE": "float",
            "MIN": 0.0,
            "MAX": 5.0,
            "DEFAULT": 1.0,
            "LABEL": "Orbit Speed"
        },
        {
            "NAME": "orbitRadius",
            "TYPE": "float",
            "MIN": 0.0,
            "MAX": 5.0,
            "DEFAULT": 1.0,
            "LABEL": "Orbit Radius"
        },
        {
            "NAME": "planetCount",
            "TYPE": "float",
            "MIN": 0.0,
            "MAX": 5.0,
            "DEFAULT": 1.0,
            "LABEL": "Planet Count"
        },
        {
            "NAME": "trailLength",
            "TYPE": "float",
            "MIN": 0.0,
            "MAX": 5.0,
            "DEFAULT": 1.0,
            "LABEL": "Trail Length"
        },
        {
            "NAME": "colorShift",
            "TYPE": "float",
            "MIN": 0.0,
            "MAX": 5.0,
            "DEFAULT": 1.0,
            "LABEL": "Color Shift"
        },
        {
            "NAME": "reverseOrbit",
            "TYPE": "bool",
            "DEFAULT": 0,
            "LABEL": "Reverse Orbit"
        },
        {
            "NAME": "orbitLayers",
            "TYPE": "float",
            "MIN": 0.0,
            "MAX": 5.0,
            "DEFAULT": 1.0,
            "LABEL": "Orbit Layers"
        },
        {
            "NAME": "planetSize",
            "TYPE": "float",
            "MIN": 0.0,
            "MAX": 5.0,
            "DEFAULT": 1.0,
            "LABEL": "Planet Size"
        },
        {
            "NAME": "objectShape",
            "TYPE": "float",
            "MIN": 0.0,
            "MAX": 5.0,
            "DEFAULT": 1.0,
            "LABEL": "Object Shape"
        },
        {
            "NAME": "objectRotation",
            "TYPE": "float",
            "MIN": 0.0,
            "MAX": 5.0,
            "DEFAULT": 1.0,
            "LABEL": "Object Rotation"
        },
        {
            "NAME": "objectPulse",
            "TYPE": "float",
            "MIN": 0.0,
            "MAX": 5.0,
            "DEFAULT": 1.0,
            "LABEL": "Object Pulse"
        },
        {
            "NAME": "objectGlow",
            "TYPE": "float",
            "MIN": 0.0,
            "MAX": 5.0,
            "DEFAULT": 1.0,
            "LABEL": "Object Glow"
        },
        {
            "NAME": "objectSpeed",
            "TYPE": "float",
            "MIN": 0.0,
            "MAX": 5.0,
            "DEFAULT": 1.0,
            "LABEL": "Object Speed"
        }
    ],
    "TAGS": [
        "space",
        "geometric",
        "3d"
    ]
}*/

// OrbitFX - Advanced Planetary System with Individual Object Controls
precision highp float;
uniform float time;
uniform vec2 mouse, resolution;

// Custom uniform parameters that will be exposed in Resolume
uniform float orbitSpeed;     // Controls orbital speed (0.0 to 1.0)
uniform float orbitRadius;    // Controls orbit radius (0.0 to 1.0)
uniform float planetCount;    // Controls number of planets (0.0 to 1.0)
uniform float trailLength;    // Controls trail length (0.0 to 1.0)
uniform float colorShift;     // Controls color shifting (0.0 to 1.0)
uniform bool reverseOrbit;    // Boolean parameter to reverse orbit direction
uniform float orbitLayers;    // Controls number of orbital layers (0.0 to 1.0)
uniform float planetSize;     // Controls planet size (0.0 to 1.0)
uniform float objectShape;    // Controls object shapes (0.0 to 1.0)
uniform float objectRotation; // Controls individual object rotation (0.0 to 1.0)
uniform float objectPulse;    // Controls object pulsing (0.0 to 1.0)
uniform float objectGlow;     // Controls object glow intensity (0.0 to 1.0)
uniform float objectSpeed;   // Controls individual object rotation speed (0.0 to 1.0)

// Function to create different shapes
float createShape(vec2 pos, float shape, float size) {
    float dist = length(pos);
    
    if (shape < 0.2) {
        // Circle
        return exp(-dist * (1.0 / size));
    } else if (shape < 0.4) {
        // Square
        float square = max(abs(pos.x), abs(pos.y));
        return exp(-square * (1.0 / size));
    } else if (shape < 0.6) {
        // Diamond
        float diamond = abs(pos.x) + abs(pos.y);
        return exp(-diamond * (1.0 / size));
    } else if (shape < 0.8) {
        // Star (4-pointed)
        float angle = atan(pos.y, pos.x);
        float star = cos(angle * 4.0) * 0.5 + 0.5;
        return exp(-dist * (1.0 / (size * star)));
    } else {
        // Hexagon
        float angle = atan(pos.y, pos.x);
        float hex = cos(angle * 6.0) * 0.3 + 0.7;
        return exp(-dist * (1.0 / (size * hex)));
    }
}

void main( void ) {
    vec2 uv = (gl_FragCoord.xy - 0.5 * resolution.xy) / resolution.y;
    
    // Apply custom parameters
    float speed = 0.3 + orbitSpeed * 2.0;
    float baseRadius = 0.05 + orbitRadius * 0.3;
    float planets = 2.0 + planetCount * 8.0;
    float trail = 0.1 + trailLength * 0.9;
    float colorSpeed = 0.5 + colorShift * 2.0;
    float layers = 1.0 + orbitLayers * 4.0;
    float size = 0.5 + planetSize * 2.0;
    float shape = objectShape;
    float rotation = objectRotation * 6.28318;
    float pulse = 0.5 + objectPulse * 2.0;
    float glow = 0.3 + objectGlow * 1.5;
    float objSpeed = 0.5 + objectSpeed * 3.0;
    
    vec3 color = vec3(0.0);
    
    // Create multiple orbital layers (like planetary system)
    for(float layer = 0.0; layer < layers; layer++) {
        float layerRadius = baseRadius * (1.0 + layer * 0.8);
        float layerSpeed = speed * (1.0 + layer * 0.3) * (reverseOrbit ? -1.0 : 1.0);
        
        // Draw orbital path (faint circle)
        float orbitDist = abs(length(uv - mouse * 0.3) - layerRadius);
        color += vec3(0.05, 0.1, 0.15) * exp(-orbitDist * 50.0) * (1.0 - layer * 0.2);
        
        // Create planets in this orbital layer
        for(float i = 0.0; i < planets; i++) {
            float fi = i / planets;
            
            // Calculate planet position with orbital mechanics
            float angle = time * layerSpeed + fi * 6.28318;
            vec2 planetPos = vec2(cos(angle), sin(angle)) * layerRadius;
            
            // Add mouse interaction (center of system)
            planetPos += mouse * 0.3;
            
            // Calculate distance from current pixel to planet
            vec2 localPos = uv - planetPos;
            
            // Apply individual object rotation
            float objAngle = time * objSpeed + fi * 6.28318 + rotation;
            mat2 rotMatrix = mat2(cos(objAngle), -sin(objAngle), sin(objAngle), cos(objAngle));
            localPos = rotMatrix * localPos;
            
            // Create planet with size variation and pulsing
            float planetSize = (0.8 + 0.4 * sin(fi * 6.28318 + time * 0.5)) * size * 0.02;
            planetSize *= (1.0 + sin(time * pulse + fi * 6.28318) * 0.3);
            
            // Create different shapes
            float planet = createShape(localPos, shape, planetSize);
            
            // Add planet glow with individual control
            float glowSize = planetSize * (2.0 + glow);
            float planetGlow = exp(-length(localPos) * (1.0 / glowSize)) * glow;
            planet += planetGlow;
            
            // Add orbital trail
            float trailAngle = angle - trail * 2.0;
            vec2 trailPos = vec2(cos(trailAngle), sin(trailAngle)) * layerRadius;
            trailPos += mouse * 0.3;
            vec2 trailLocalPos = uv - trailPos;
            trailLocalPos = rotMatrix * trailLocalPos;
            float trailDist = length(trailLocalPos);
            planet += exp(-trailDist * (1.0 / (planetSize * 2.0))) * trail * 0.5;
            
            // Color based on layer, planet position, and individual variation
            vec3 planetColor = vec3(
                0.3 + 0.4 * sin(fi * 6.28318 + time * colorSpeed + layer * 1.0 + i * 0.5),
                0.3 + 0.4 * sin(fi * 6.28318 + time * colorSpeed + 2.094 + layer * 1.0 + i * 0.5),
                0.3 + 0.4 * sin(fi * 6.28318 + time * colorSpeed + 4.188 + layer * 1.0 + i * 0.5)
            );
            
            // Add layer-based color variation
            planetColor *= (1.0 - layer * 0.15);
            
            // Add individual object color variation based on shape
            if (shape < 0.2) {
                planetColor *= vec3(1.0, 0.9, 0.8); // Warm for circles
            } else if (shape < 0.4) {
                planetColor *= vec3(0.8, 1.0, 0.9); // Cool for squares
            } else if (shape < 0.6) {
                planetColor *= vec3(1.0, 0.8, 1.0); // Purple for diamonds
            } else if (shape < 0.8) {
                planetColor *= vec3(1.0, 1.0, 0.7); // Yellow for stars
            } else {
                planetColor *= vec3(0.7, 0.9, 1.0); // Blue for hexagons
            }
            
            color += planetColor * planet;
        }
    }
    
    // Add central star/sun with variable size
    float centerDist = length(uv - mouse * 0.3);
    vec3 sunColor = vec3(1.0, 0.8, 0.4);
    float sunSize = 0.02 + size * 0.01;
    color += sunColor * exp(-centerDist * (1.0 / sunSize)) * 0.8;
    color += sunColor * exp(-centerDist * (1.0 / (sunSize * 2.0))) * 0.4;
    
    // Add solar wind/particle effects
    vec2 windUV = uv - mouse * 0.3;
    float windAngle = atan(windUV.y, windUV.x);
    float windDist = length(windUV);
    float windEffect = sin(windAngle * 8.0 + time * 3.0) * exp(-windDist * 5.0);
    color += vec3(0.1, 0.2, 0.4) * windEffect * 0.3;
    
    // Add distant stars
    vec2 starUV = uv * 15.0;
    float star = sin(starUV.x + time * 0.5) * sin(starUV.y + time * 0.3);
    color += vec3(0.8) * max(0.0, star - 0.95) * 0.5;
    
    // Apply mouse interaction for overall brightness
    float mouseDist = length(uv - mouse * 0.5);
    color *= (1.0 - smoothstep(0.0, 0.8, mouseDist) * 0.3);
    
    gl_FragColor = vec4(color, 1.0);
}
