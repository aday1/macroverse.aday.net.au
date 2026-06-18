/*{
    "DESCRIPTION": "CheckerboardVariaent2",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "grid"
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
            "NAME": "speed",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 5.0,
            "LABEL": "Speed"
        },
        {
            "NAME": "mouseX",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Mouse X"
        },
        {
            "NAME": "mouseY",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Mouse Y"
        },
        {
            "NAME": "zoom",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 4.0,
            "LABEL": "Zoom"
        },
        {
            "NAME": "colorR",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Red"
        },
        {
            "NAME": "colorG",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Green"
        },
        {
            "NAME": "colorB",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Blue"
        },
        {
            "NAME": "brightness",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Brightness"
        },
        {
            "NAME": "saturation",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Saturation"
        },
        {
            "NAME": "contrast",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Contrast"
        },
        {
            "NAME": "hueShift",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Hue Shift"
        },
        {
            "NAME": "invert",
            "TYPE": "bool",
            "DEFAULT": 0,
            "LABEL": "Invert Colors"
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "grid"
    ]
}*/
#define E 2.71828182846

varying vec2 position;

uniform vec4 color;
uniform float colorB;
uniform float colorG;
uniform float colorR;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)

#ifdef GL_ES
precision highp float;
#endif

uniform vec4 inputColour;

//Simple raymarching sandbox with camera

//Raymarching Distance Fields
//About http://www.iquilezles.org/www/articles/raymarchingdf/raymarchingdf.htm
//Also known as Sphere Tracing

//Util Start
vec2 ObjUnion(in vec2 obj0,in vec2 obj1){
    if (obj0.x<obj1.x)
        return obj0;
    else
        return obj1;
}
//Util End

//Scene Start

//Floor
vec2 obj0(in vec3 p){
    //obj deformation
    p.y=p.y+sin(sqrt(p.x*p.x+p.z*p.z)-sin(time)*9.0)*mouse.x;
    //plane
    
    return vec2(p.y+3.0,0);
}
//Floor Color (checkerboard)
vec3 obj0_c(in vec3 p){
    if (fract(p.x*.5)>.5)
        if (fract(p.z*.5)>.5)
            return vec3(inputColour.z,0,3);
        else
            return vec3(0,inputColour.w,3);
        else
            if (fract(p.z*.5)>.5)
                return vec3(0,1,0);
            else
                return vec3(3,0,1);
}

//IQs RoundBox (try other objects http://www.iquilezles.org/www/articles/distfunctions/distfunctions.htm)
vec2 obj1(in vec3 p){
    //obj deformation
    p.y=3.5+p.y+sin(sqrt(p.x*p.x+p.z*p.z)-time*0.0)*inputColour.w;
    p.x=fract(p.x+0.0)-0.0;
    p.z=fract(p.z+0.0)-0.0;
    p.y=p.y-1.0+sin(time*inputColour.x);
    return vec2(length(max(abs(p)-vec3(0.0,0.5,0.0),0.0))-0.0,0);
}

//RoundBox with simple solid color
vec3 obj1_c(in vec3 p){
    return vec3(mouse.y,sin(p.x*0.2),sin(p.z*0.2));
}

//Objects union
vec2 inObj(in vec3 p){
    return ObjUnion(obj0(p),obj1(p));
}

//Scene End

void _userMain(void){
    vec2 vPos=-mouse.y+2.0*gl_FragCoord.xy/resolution.xy;
    
    //Camera animation
    vec3 vuv=vec3(0.0,0.5,inputColour.x);//Change camere up vector here
    vec3 prp=vec3(-sin(time*inputColour.y)*8.0,3,cos(time*0.4)*30.0 - 10.0); //Change camera path position here
    vec3 vrp=vec3(2,inputColour.z,30); //Change camere view here

    //Camera setup
    vec3 vpn=normalize(vrp-prp);
    vec3 u=normalize(cross(vuv,vpn));
    vec3 v=cross(vpn,u);
    vec3 vcv=(prp+vpn);
    vec3 scrCoord=vcv+vPos.x*u*resolution.x/resolution.y+vPos.y*v;
    vec3 scp=normalize(scrCoord-prp);
    
    //Raymarching
    const vec3 e=vec3(0.1,0,0);
    const float maxd=60.0; //Max depth
    
    vec2 s=vec2(0.1,0.0);
    vec3 c,p,n;
    
    float f=inputColour.w;
    for(int i=0;i<256;i++){
        if (abs(s.x)<.01||f>maxd) break;
        f+=s.x;
        p=prp+scp*f;
        s=inObj(p);
    }
    
    if (f<maxd){
        if (s.y==0.0)
            c=obj0_c(p);
        else
            c=obj1_c(p);
        n=normalize(
                    vec3(s.x-inObj(p-e.xyy).x,
                         s.x-inObj(p-e.yxy).x,
                         s.x-inObj(p-e.yyx).x));
        float b=dot(n,normalize(prp-p));
        gl_FragColor=vec4((b*c+pow(b,8.0))*(1.0-f*.02),inputColour.z);//simple phong LightPosition=CameraPosition
    }
    else gl_FragColor=vec4(0,0,0,1); //background color
}

void main() {
    _userMain();
    vec3 c = gl_FragColor.rgb;
    float a = gl_FragColor.a;
    float luma = dot(c, vec3(0.299, 0.587, 0.114));
    c = mix(vec3(luma), c, saturation);
    c = (c - 0.5) * contrast + 0.5;
    c *= vec3(colorR, colorG, colorB);
    c += brightness;
    if (hueShift > 0.001) {
        float cosH = cos(hueShift * 6.28318);
        float sinH = sin(hueShift * 6.28318);
        c = vec3(
            c.r * (0.299 + 0.701*cosH + 0.168*sinH) + c.g * (0.587 - 0.587*cosH + 0.330*sinH) + c.b * (0.114 - 0.114*cosH - 0.497*sinH),
            c.r * (0.299 - 0.299*cosH - 0.328*sinH) + c.g * (0.587 + 0.413*cosH + 0.035*sinH) + c.b * (0.114 - 0.114*cosH + 0.292*sinH),
            c.r * (0.299 - 0.300*cosH + 1.250*sinH) + c.g * (0.587 - 0.588*cosH - 1.050*sinH) + c.b * (0.114 + 0.886*cosH - 0.203*sinH)
        );
    }
    if (invert) c = 1.0 - c;
    gl_FragColor = vec4(clamp(c, 0.0, 1.0), a);
}