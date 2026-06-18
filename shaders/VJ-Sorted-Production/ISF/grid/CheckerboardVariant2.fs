/*{
    "DESCRIPTION": "CheckerboardVariant2",
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
uniform float timeScale;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE

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

void main(void){
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
