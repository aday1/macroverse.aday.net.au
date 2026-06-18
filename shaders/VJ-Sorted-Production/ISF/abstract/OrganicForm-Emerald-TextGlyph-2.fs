/*{
    "DESCRIPTION": "OrganicForm-Emerald-TextGlyph-2",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "abstract"
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
        "abstract",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE

precision mediump float;

uniform sampler2D imagePrecedente;

//------------ Mon code de prod -------------------

bool celluleVivante(vec2 position) {
	vec4 valeurCellule = texture2D(
		imagePrecedente, 
		((position+vec2(0.5)) / resolution.xy)
	);
	return valeurCellule.a > 0.5;
}

int compteVoisinsVivants(vec2 position) {
	return
	 int (celluleVivante(position + vec2(-1.0, -1.0)))
	+int (celluleVivante(position + vec2(0.0, -1.0)))
	+int (celluleVivante(position + vec2(1.0, -1.0)))
	+int (celluleVivante(position + vec2(-1.0, 0.0)))
	+int (celluleVivante(position + vec2(1.0, 0.0)))
	+int (celluleVivante(position + vec2(-1.0, 1.0)))
	+int (celluleVivante(position + vec2(0.0, 1.0)))
	+int (celluleVivante(position + vec2(1.0, 1.0)));
}

bool prochainCoup(vec2 position) {
	int voisins = compteVoisinsVivants(position);
	return ((voisins == 2 && celluleVivante(position)) || voisins == 3);
}

void joueLaVie() {
	if (mouse.x < 0.5)
		gl_FragColor = vec4(sin(cos(gl_FragCoord.x*789.0) * gl_FragCoord.y  + time*10.0));
	else
		gl_FragColor = vec4(prochainCoup(floor(gl_FragCoord.xy)));
}

//------------ Mes tests --------------------------

bool allumeCellule(vec2 position) {
	return position == floor(gl_FragCoord.xy);
}

void initData() {
	gl_FragColor = vec4(float(
	allumeCellule(vec2(10.0, 10.0))||
	allumeCellule(vec2(19.0, 10.0))||
	allumeCellule(vec2(21.0, 10.0))||
	
	allumeCellule(vec2(29.0, 10.0))||
	allumeCellule(vec2(30.0, 10.0))||
	allumeCellule(vec2(31.0, 10.0))||
	
	allumeCellule(vec2(39.0, 9.0))||
	allumeCellule(vec2(39.0, 11.0))||
	allumeCellule(vec2(40.0, 10.0))||
	allumeCellule(vec2(41.0, 9.0))||
	allumeCellule(vec2(41.0, 11.0))));
}

bool onSaitDetecterSiUneCelluleEstVivante() {
	return celluleVivante(vec2(10.0, 10.0));
}

bool onSaitCompterUnVoisinVivant() {
	int voisinsVivants = compteVoisinsVivants(
		vec2(10.0, 11.0)
	);
	return voisinsVivants == 1;
}

bool onSaitCompterDeuxVoisinsVivants() {
	int voisinsVivants = compteVoisinsVivants(
		vec2(20.0, 9.0)
	);
	return voisinsVivants == 2;
}

bool uneCelluleIsoleeMeurt() {
	return !prochainCoup(vec2(10.0, 10.0));
}

bool uneCelluleAvecDeuxVoisinsResteVivante() {
	return prochainCoup(vec2(30.0, 10.0));
}

bool uneCelluleNaitSiTroisVoisins() {
	return prochainCoup(vec2(30.0, 9.0));
}

bool uneCelluleMeurtSiPlusDeTroisVoisins() {
	return !prochainCoup(vec2(40.0, 10.0));
}

bool uneCelluleResteMorteSiQueDeuxVoisins() {
	return !prochainCoup(vec2(20.0, 10.0));
}

//------------ Mon cadre de travail ---------------

void dessineBarreRouge() {
	gl_FragColor = vec4(1.0, 0.2, 0.2, 1.0);
}

void dessineBarreVerte() {
	gl_FragColor = vec4(0.2, 1.0, 0.2, 1.0);
}

void afficheResultatTest(bool resultat) {
	if (resultat) dessineBarreVerte();
	else dessineBarreRouge();
}

void lanceSuiteTests() {
		int nombreTests = 8;
		int numeroTest = int(float(nombreTests) * gl_FragCoord.x / resolution.x);
	
		if (numeroTest-- == 0) afficheResultatTest(onSaitDetecterSiUneCelluleEstVivante());
		if (numeroTest-- == 0) afficheResultatTest(onSaitCompterUnVoisinVivant());
		if (numeroTest-- == 0) afficheResultatTest(onSaitCompterDeuxVoisinsVivants());
		if (numeroTest-- == 0) afficheResultatTest(uneCelluleIsoleeMeurt());
		if (numeroTest-- == 0) afficheResultatTest(uneCelluleAvecDeuxVoisinsResteVivante());
		if (numeroTest-- == 0) afficheResultatTest(uneCelluleNaitSiTroisVoisins());
		if (numeroTest-- == 0) afficheResultatTest(uneCelluleMeurtSiPlusDeTroisVoisins());
		if (numeroTest-- == 0) afficheResultatTest(uneCelluleResteMorteSiQueDeuxVoisins());
}

// ------------------- Code de la main ------------------

void main( void ) {
	float ouOnEnEst = gl_FragCoord.y / resolution.y;
	if (ouOnEnEst > 0.8) lanceSuiteTests();
	else if (ouOnEnEst < 0.3) initData();
	else joueLaVie();
}
