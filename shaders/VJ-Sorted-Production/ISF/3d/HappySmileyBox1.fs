/*{
    "DESCRIPTION": "HappySmileyBox1",
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
            "NAME": "timeScale",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 4.0,
            "LABEL": "Time speed"
        },
        {
            "NAME": "mouseX",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Mouse X"
        },
        {
            "NAME": "mouseY",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Mouse Y"
        }
    ],
    "TAGS": [
        "3d",
        "texture-input"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// Ray-casting with procedural textures by Omar El Sayyed. This is far from optimal. It's written to be overly verbose and self-explanatory.
// Borrowed the smily face from here: http://glsl.heroku.com/e#201.0  (By @mnstrmnch).
// Join us on our quest for learning shaders: https://www.facebook.com/groups/748114015233903/
// And please like our page :P
// https://www.facebook.com/

#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D backbuffer;
#define PI 3.14159265   

vec3 eyePosition = vec3(0.0, 0.0, -2.0);  
vec3 lightPosition = vec3(0.0, 0.0, -1.0);  
    
vec4 faceTexture(vec2 position) {  
    
  vec4 color = vec4(0.0, 0.0, 0.5, 1.0);  
        
  // Transform input position from (0.0, 0.0)-(1.0, 1.0) to (-1.0, 1.0)-(1.0, -1.0),  
  position = (position * 2.0) - 1.0;  
  position.y = -position.y;  
    
  // Simple face texture. Bottom left at (-1.0, -1.0) and top right at (1.0, 1.0),  
  if(length(position) < 1.0) {  
    if(length(position) < 0.9) {  
      color = vec4(1.0) - color;  
      if (length(position * vec2(1.0, 1.0)) < 0.7 && length(position) > 0.6 && position.y < -0.125) color = vec4(0.0); // smile  
      if (length((position - vec2(-0.35, 0.35)) * vec2(1.0, 0.5)) < 0.125) color = vec4(0.0); // left eye  
      if (length((position - vec2(+0.35, 0.35)) * vec2(1.0, 0.5)) < 0.125) color = vec4(0.0); // right eye  
    } else {  
      color = vec4(0.0);  
    }  
  }   
    
  color.a = 1.0;
  return color;  
}  

vec4 checkerBoardTexture(vec2 position) {  
  
  position = fract(position);
  if (position.x < 0.5) {
    if (position.y < 0.5) {
      return vec4(1.0, 1.0, 1.0, 1.0);
    } else {
      return vec4(0.2, 0.2, 0.2, 1.0);
    }  
  } else {
    if (position.y < 0.5) {
      return vec4(0.2, 0.2, 0.2, 1.0);
    } else {
      return vec4(1.0, 1.0, 1.0, 1.0);
    }  
  }
}

vec4 plasmaTexture(vec2 position) {  

  float plasma1 = sin((1.0*position.x) + time);
  float plasma2 = sin(1.0*(position.x*sin(time/2.0) + position.y*cos(time/3.0)) + time);
  
  float centerX = position.x + 0.5*sin(time/5.0);
  float centerY = position.y + 0.5*cos(time/3.0);
  float plasma3 = sin(sqrt(100.0*(centerX*centerX + centerY*centerY) + 1.0) + time);

  float plasma = plasma1 + plasma2 + plasma3;
  plasma = cos(plasma);
  return vec4(plasma, plasma, plasma, 1.0);
}

float intersectPlane(vec3 rayVector, vec3 pointOnRay, vec3 planeNormal, vec3 pointOnPlane) {

  float componentInNormalDirection = dot(planeNormal, rayVector);
  //if (componentInNormalDirection == 0.0) return 1000.0;
  
  float t = dot(planeNormal, pointOnPlane - pointOnRay) / componentInNormalDirection;
  if (t < 0.0) return 1000.0;
  return t;
}    
    
float intersectCube(vec3 rayVector, vec3 pointOnRay, vec3 cubeMin, vec3 cubeMax) {  
      
  vec3 tMin = (cubeMin - pointOnRay) / rayVector;  
  vec3 tMax = (cubeMax - pointOnRay) / rayVector;  
      
  vec3 tNear = min(tMin, tMax);  
  vec3 tFar  = max(tMin, tMax);  
      
  float tNearMax = max(max(tNear.x, tNear.y), tNear.z);  
  float tFarMin  = min(min(tFar .x, tFar .y), tFar .z);  
      
  if (tNearMax > tFarMin) return 1000.0;  
      
  return max(max(tNear.x, tNear.y), tNear.z);    
}  
    
vec3 getPointOnCubeNormal(vec3 point, vec3 cubeMin, vec3 cubeMax) {  
  if (abs(point.x-cubeMin.x) < 0.001) return vec3(-1.0,  0.0,  0.0);  
  else if (abs(point.x-cubeMax.x) < 0.001) return vec3( 1.0,  0.0,  0.0);  
  else if (abs(point.y-cubeMin.y) < 0.001) return vec3( 0.0, -1.0,  0.0);  
  else if (abs(point.y-cubeMax.y) < 0.001) return vec3( 0.0,  1.0,  0.0);  
  else if (abs(point.z-cubeMin.z) < 0.001) return vec3( 0.0,  0.0, -1.0);  
  else if (abs(point.z-cubeMax.z) < 0.001) return vec3( 0.0,  0.0,  1.0);  
  else return vec3(0.0);  
}  
    
vec2 getPointOnCubeTextureCoordinates(vec3 point, vec3 cubeMin, vec3 cubeMax) {  
  if (abs(point.x-cubeMin.x) < 0.001) {  
    return vec2(  
        (cubeMax.z - point.z) / (cubeMax.z - cubeMin.z),  
        (cubeMax.y - point.y) / (cubeMax.y - cubeMin.y));  
  } else if (abs(point.x-cubeMax.x) < 0.001) {  
    return vec2(  
        (point.z - cubeMin.z) / (cubeMax.z - cubeMin.z),  
        (cubeMax.y - point.y) / (cubeMax.y - cubeMin.y));  
  } else if (abs(point.y-cubeMin.y) < 0.001) {  
    return vec2(  
        (point.x - cubeMin.x) / (cubeMax.x - cubeMin.x),  
        (point.z - cubeMin.z) / (cubeMax.z - cubeMin.z));  
  } else if (abs(point.y-cubeMax.y) < 0.001) {  
    return vec2(  
        (point.x - cubeMin.x) / (cubeMax.x - cubeMin.x),  
        (cubeMax.z - point.z) / (cubeMax.z - cubeMin.z));  
  } else if (abs(point.z-cubeMin.z) < 0.001) {  
    return vec2(  
        (point.x - cubeMin.x) / (cubeMax.x - cubeMin.x),  
        (cubeMax.y - point.y) / (cubeMax.y - cubeMin.y));  
  } else if (abs(point.z-cubeMax.z) < 0.001) {  
      return vec2(  
        (cubeMax.x - point.x) / (cubeMax.x - cubeMin.x),  
        (cubeMax.y - point.y) / (cubeMax.y - cubeMin.y));  
  } else return vec2(0.0);  
}  
    
vec4 light(vec3 pointPosition, vec3 pointNormal, vec3 lightPostiion, vec4 ambientColor, vec4 diffuseColor, vec4 specularColor, float shininess) {    
    
  vec3 L = normalize(lightPostiion - pointPosition);     
  vec3 E = normalize(eyePosition - pointPosition);   
  vec3 R = normalize(-reflect(L, pointNormal));    
     
  // Calculate ambient term,  
  vec4 ambient = ambientColor;  
   
  // Calculate diffuse term,  
  vec4 diffuse = diffuseColor * max(dot(pointNormal,L), 0.0);  
  diffuse = clamp(diffuse, 0.0, 1.0);       
  
  // Calculate specular term,  
  vec4 specular = specularColor * pow(max(dot(R,E),0.0),0.3*shininess);  
  specular = clamp(specular, 0.0, 1.0);   
    
  return ambient + diffuse + specular;  
}  
    
void main() {  
      
  // Normalize coordinates,  
  vec3 pointPosition = vec3((gl_FragCoord.xy / resolution) * 2.0 - 1.0, 0.0);  
  pointPosition.x *= resolution.x / resolution.y;  
  vec3 initialPointPosition = pointPosition;
      
  vec3 cubeMin = vec3(-0.5, -0.5, -0.5);  
  vec3 cubeMax = vec3( 0.5,  0.5,  0.5);  
    
  // Transform point, eye and light positions to rotate the cube,  
  float aboutXAngle = 0.3123 * time;  
  float aboutYAngle = 0.7123 * time;  
  float aboutZAngle = time;  
  vec3 cubeCenter = (cubeMin + cubeMax) * 0.5;  
    
  // Rotate around x-axis,  
  vec3 tempPoint = pointPosition - cubeCenter;  
  vec3 tempEye   =   eyePosition - cubeCenter;  
  vec3 tempLight = lightPosition - cubeCenter;  
  float cosAngle = cos(aboutXAngle);  
  float sinAngle = sin(aboutXAngle);  
  pointPosition.z = (tempPoint.z * cosAngle) - (tempPoint.y * sinAngle);  
  pointPosition.y = (tempPoint.y * cosAngle) + (tempPoint.z * sinAngle);  
  eyePosition.z   = (tempEye.z   * cosAngle) - (tempEye.y   * sinAngle);  
  eyePosition.y   = (tempEye.y   * cosAngle) + (tempEye.z   * sinAngle);  
  lightPosition.z = (tempLight.z * cosAngle) - (tempLight.y * sinAngle);  
  lightPosition.y = (tempLight.y * cosAngle) + (tempLight.z * sinAngle);  

  // Rotate around y-axis,  
  tempPoint = pointPosition;  
  tempEye   =   eyePosition;  
  tempLight = lightPosition;  
  cosAngle = cos(aboutYAngle);  
  sinAngle = sin(aboutYAngle);  
  pointPosition.x = (tempPoint.x * cosAngle) - (tempPoint.z * sinAngle);  
  pointPosition.z = (tempPoint.z * cosAngle) + (tempPoint.x * sinAngle);  
  eyePosition.x   = (tempEye.x   * cosAngle) - (tempEye.z   * sinAngle);  
  eyePosition.z   = (tempEye.z   * cosAngle) + (tempEye.x   * sinAngle);  
  lightPosition.x = (tempLight.x * cosAngle) - (tempLight.z * sinAngle);  
  lightPosition.z = (tempLight.z * cosAngle) + (tempLight.x * sinAngle);  
    
  // Rotate around z-axis,  
  tempPoint = pointPosition;  
  tempEye   =   eyePosition;  
  tempLight = lightPosition;  
  cosAngle = cos(aboutZAngle);  
  sinAngle = sin(aboutZAngle);  
  pointPosition.x = (tempPoint.x * cosAngle) - (tempPoint.y * sinAngle);  
  pointPosition.y = (tempPoint.y * cosAngle) + (tempPoint.x * sinAngle);  
  eyePosition.x   = (tempEye.x   * cosAngle) - (tempEye.y   * sinAngle);  
  eyePosition.y   = (tempEye.y   * cosAngle) + (tempEye.x   * sinAngle);  
  lightPosition.x = (tempLight.x * cosAngle) - (tempLight.y * sinAngle);  
  lightPosition.y = (tempLight.y * cosAngle) + (tempLight.x * sinAngle);  
      
  pointPosition += cubeCenter;  
  eyePosition   += cubeCenter;  
  lightPosition += cubeCenter;  
      
  // Ray cast,  
  // Cube,
  vec3 rayVector = pointPosition - eyePosition;  
  float cubeT = intersectCube(rayVector, eyePosition, cubeMin, cubeMax);  

  // Plane,   
  vec3 planeNormal = vec3(0.0, 1.0, 0.0);
  vec3 pointOnPlane = vec3(0.0, -2.0, 0.0);
  float planeT = intersectPlane(rayVector, eyePosition, planeNormal, pointOnPlane);

  // Light and texture,
  float t = min(planeT, cubeT);      
  if (t == 1000.0) {  
    gl_FragColor = plasmaTexture(initialPointPosition.xy);
    return ;  
  }  

  vec3 intersectionPoint = eyePosition + (t * rayVector);  

  vec4 textureColor;
  vec3 normal;  
  if (t == planeT) {
    textureColor = checkerBoardTexture(intersectionPoint.xz);
    normal = planeNormal;
  } else {
    textureColor = faceTexture(getPointOnCubeTextureCoordinates(intersectionPoint, cubeMin, cubeMax));  
    normal = getPointOnCubeNormal(intersectionPoint, cubeMin, cubeMax);  
  }  
      
  // Light,  
  vec4 lightColor = light(  
    intersectionPoint, normal,   
    lightPosition,   
    vec4(0.15, 0.15, 0.15, 1.0),   
    vec4(1.0, 1.0, 1.0, 1.0),   
    vec4(0.8, 0.8, 0.8, 1.0),  
    40.0);  
        
  vec4 lastFrameColor = texture2D(backbuffer, gl_FragCoord.xy/resolution);
  vec4 thisFrameColor = textureColor * lightColor;
  gl_FragColor = mix(lastFrameColor, thisFrameColor, 0.25);
}  
