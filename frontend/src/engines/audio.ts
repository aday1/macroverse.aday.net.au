const AUDIO_MAP_KEY = 'macroverse-audio-map';

export const FFT_BAND_LABELS = [
  'Sub1', 'Sub2', 'Low1', 'Low2', 'LoMid1', 'LoMid2', 'Mid1', 'Mid2',
  'Mid3', 'HiMid1', 'HiMid2', 'High1', 'High2', 'Pres', 'Brill', 'Air'
];

export const FFT_BAND_COUNT = FFT_BAND_LABELS.length;

export const audioEngine = {
  ctx: null as AudioContext | null,
  analyser: null as AnalyserNode | null,
  gainNode: null as GainNode | null,
  source: null as MediaStreamAudioSourceNode | null,
  stream: null as MediaStream | null,
  fftData: null as Uint8Array | null,
  bands: new Float32Array(FFT_BAND_COUNT),
  smoothBands: new Float32Array(FFT_BAND_COUNT),
  smoothing: 0.8,
  gain: 1.0,
  active: false,
  bandParamMap: {} as Record<number, string>,
  deviceLabel: '' as string,

  async start(deviceId?: string): Promise<void> {
    if (this.active) this.stop();
    const constraints: MediaStreamConstraints = {
      audio: deviceId ? { deviceId: { exact: deviceId } } : true,
      video: false
    };
    this.stream = await navigator.mediaDevices.getUserMedia(constraints);
    const Ctx = window.AudioContext || (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!Ctx) throw new Error('Web Audio API not supported');
    this.ctx = new Ctx();
    this.source = this.ctx.createMediaStreamSource(this.stream);
    this.gainNode = this.ctx.createGain();
    this.gainNode.gain.value = this.gain;
    this.analyser = this.ctx.createAnalyser();
    this.analyser.fftSize = 2048;
    this.analyser.smoothingTimeConstant = 0.85;
    this.source.connect(this.gainNode);
    this.gainNode.connect(this.analyser);
    this.fftData = new Uint8Array(this.analyser.frequencyBinCount);
    if (this.ctx.state === 'suspended') {
      await this.ctx.resume();
    }
    const track = this.stream.getAudioTracks()[0];
    this.deviceLabel = track?.label || 'Default input';
    this.active = true;
  },

  stop(): void {
    if (this.stream) {
      this.stream.getTracks().forEach((t) => t.stop());
      this.stream = null;
    }
    if (this.source) {
      try {
        this.source.disconnect();
      } catch (_) {}
      this.source = null;
    }
    if (this.ctx) {
      this.ctx.close().catch(() => {});
      this.ctx = null;
    }
    this.analyser = null;
    this.gainNode = null;
    this.fftData = null;
    this.active = false;
    this.deviceLabel = '';
    this.bands.fill(0);
    this.smoothBands.fill(0);
  },

  setGain(v: number): void {
    this.gain = v;
    if (this.gainNode) this.gainNode.gain.value = v;
  },

  update(): void {
    if (!this.active || !this.analyser || !this.fftData) return;
    this.analyser.getByteFrequencyData(this.fftData);
    const nyquist = this.ctx!.sampleRate / 2;
    const binCount = this.analyser.frequencyBinCount;
    const binSize = nyquist / binCount;
    const ranges: [number, number][] = [
      [20, 40], [40, 70], [70, 120], [120, 200],
      [200, 350], [350, 500], [500, 800], [800, 1300],
      [1300, 2000], [2000, 3000], [3000, 5000], [5000, 7000],
      [7000, 10000], [10000, 14000], [14000, 17000], [17000, 20000]
    ];
    for (let b = 0; b < FFT_BAND_COUNT; b++) {
      const lo = Math.floor(ranges[b][0] / binSize);
      const hi = Math.min(Math.ceil(ranges[b][1] / binSize), binCount - 1);
      let sum = 0;
      let count = 0;
      for (let i = lo; i <= hi; i++) {
        sum += this.fftData[i];
        count++;
      }
      const raw = count > 0 ? (sum / count) / 255 : 0;
      this.smoothBands[b] = this.smoothBands[b] * this.smoothing + raw * (1 - this.smoothing);
      this.bands[b] = this.smoothBands[b];
    }
  },

  drawFFT(canvas: HTMLCanvasElement | null): void {
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    const w = canvas.width;
    const h = canvas.height;
    ctx.clearRect(0, 0, w, h);
    const barW = w / FFT_BAND_COUNT;
    for (let i = 0; i < FFT_BAND_COUNT; i++) {
      const v = this.bands[i];
      const barH = v * h;
      const hue = 200 + i * 20;
      ctx.fillStyle = this.bandParamMap[i] !== undefined
        ? 'hsl(' + hue + ',80%,55%)'
        : 'hsl(' + hue + ',40%,35%)';
      ctx.fillRect(i * barW + 1, h - barH, barW - 2, barH);
      ctx.fillStyle = '#888';
      ctx.font = '8px monospace';
      ctx.fillText(FFT_BAND_LABELS[i].slice(0, 3), i * barW + 2, 9);
    }
  },

  saveMappings(): void {
    try {
      localStorage.setItem(AUDIO_MAP_KEY, JSON.stringify(this.bandParamMap));
    } catch (_) {}
  },

  loadMappings(): void {
    try {
      const s = localStorage.getItem(AUDIO_MAP_KEY);
      if (s) this.bandParamMap = JSON.parse(s);
    } catch (_) {}
  }
};
