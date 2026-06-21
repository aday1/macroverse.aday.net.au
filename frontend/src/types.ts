export interface ParamRange {
  id?: string;
  min?: number;
  max?: number;
  def?: number;
}

export interface IndexEntry {
  id: number;
  path?: string;
  name?: string;
  fixedName?: string;
  category?: string;
  tags?: string[];
  sets?: string[];
  notes?: string;
  uniforms?: string[];
  paramRanges?: ParamRange[];
  favorite?: boolean;
  color?: string;
  fileHash?: string;
  sourceRoot?: string;
  format?: string;
}

export interface PathStatus {
  path: string;
  valid: boolean;
  reason?: string;
  suggestedPath?: string;
}

export interface SourcesResponse {
  paths: string[];
  indexPath: string;
  outputPath?: string;
  pathStatus?: PathStatus[];
}

export interface ThemeColorsRecord {
  amigaBg?: string;
  amigaSurface?: string;
  amigaPanel?: string;
  amigaText?: string;
  amigaTextDim?: string;
  amigaAccent?: string;
  amigaCopper?: string;
  bevelDark?: string;
  bevelLight?: string;
  editorBg?: string;
  editorFg?: string;
  editorKeyword?: string;
  editorString?: string;
  editorComment?: string;
  editorNumber?: string;
  editorPunctuation?: string;
  editorOperator?: string;
  editorFunction?: string;
  editorCaret?: string;
  editorSelection?: string;
  editorGlow?: string;
}

export interface Settings {
  sourcePaths?: string[];
  indexPath?: string;
  graveyardPath?: string;
  themeColors?: ThemeColorsRecord;
  vfxRoot?: string;
  outputPath?: string;
  previewWidth?: number;
  previewHeight?: number;
  previewResolution?: string;
  targetFps?: number;
  enablePipeline?: boolean;
  enableOutput?: boolean;
  enableGit?: boolean;
  showThumbnails?: boolean;
  listViewMode?: string;
  skipSplash?: boolean;
  previewAspect?: string;
  autoOptimizeQuality?: boolean;
  watchFolders?: boolean;
  cursorApiKey?: string;
  thumbnailQuality?: number;
  thumbnailMaxSize?: number;
  previewResolution?: string;
  previewQuality?: number;
  thumbnailLoadingPaused?: boolean;
  enableSpout?: boolean;
  enableNdi?: boolean;
  wirePath?: string;
  transition?: string;
  transitionDuration?: number;
  defaultView?: string;
  defaultParamValue?: number;
  defaultTimeScale?: number;
  llmProviders?: Array<{
    name: string;
    enabled: boolean;
    priority: number;
    model?: string;
    endpoint?: string;
  }>;
  githubToken?: string;
}

export interface VersionResponse {
  version?: string;
  buildDate?: string;
  gitRev?: string;
  gitBranch?: string;
  gitDirty?: boolean;
  releaseTag?: string;
  splashLine?: string;
}

export interface LocalStatusResponse {
  pid?: number;
  lane?: string;
  privateLibrary?: boolean;
  privateAuthorized?: boolean;
  identityMarkers?: Record<string, boolean>;
  bindHost?: string;
  port?: string;
  url?: string;
  lanUrls?: string[];
  hostMode?: string;
  readonly?: boolean;
  version?: string;
  gitRev?: string;
  gitBranch?: string;
  gitDirty?: boolean;
  sourcePaths?: string[];
  sourceStatus?: Array<{ path: string; valid: boolean; reason?: string }>;
  indexPath?: string;
  settingsPath?: string;
  shaderCount?: number;
  osc?: { running?: boolean; port?: number };
  sessions?: Array<{ id?: string; bridgeConnected?: boolean; wsClientCount?: number; clientCount?: number; streamClients?: number; hasSignal?: boolean }>;
  timestamp?: string;
}
