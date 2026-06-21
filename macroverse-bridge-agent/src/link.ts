export interface LinkClockState {
  bpm: number;
  beat: number;
  bar: number;
  isPlaying: boolean;
  linkPeers: number;
}

type LinkCtor = new (bpm?: number) => {
  enable?: (on?: boolean) => void;
  disable?: () => void;
  update?: () => void;
  startUpdate?: (interval: number) => void;
  stopUpdate?: () => void;
  bpm: number;
  beat: number;
  numPeers?: number;
  peers?: number;
};

export class AbletonLinkSession {
  private link: InstanceType<LinkCtor> | null = null;
  private available = false;
  private lastBeatInt = 0;
  private bar = 1;
  private beat = 1;
  private timeSig = 4;

  async init(initialBpm = 120): Promise<boolean> {
    try {
      const mod = await import('abletonlink');
      const AbletonLink = ((mod as unknown as { default?: LinkCtor }).default ?? (mod as unknown as LinkCtor));
      this.link = new AbletonLink(initialBpm);
      this.link.bpm = initialBpm;
      if (this.link.enable) this.link.enable();
      else if (this.link.startUpdate) this.link.startUpdate(50);
      this.available = true;
      this.lastBeatInt = Math.floor(this.link.beat);
      return true;
    } catch (err) {
      console.warn('[bridge] Ableton Link not available:', (err as Error).message);
      return false;
    }
  }

  shutdown(): void {
    if (this.link) {
      try {
        if (this.link.disable) this.link.disable();
        else if (this.link.enable) this.link.enable(false);
        if (this.link.stopUpdate) this.link.stopUpdate();
      } catch {
        /* ignore */
      }
      this.link = null;
    }
    this.available = false;
  }

  isAvailable(): boolean {
    return this.available;
  }

  snapshot(): LinkClockState {
    if (!this.link || !this.available) {
      return { bpm: 120, beat: 1, bar: 1, isPlaying: false, linkPeers: 0 };
    }
    if (this.link.update) this.link.update();
    const beatInt = Math.floor(this.link.beat);
    if (beatInt !== this.lastBeatInt) {
      this.lastBeatInt = beatInt;
      this.beat++;
      if (this.beat > this.timeSig) {
        this.beat = 1;
        this.bar++;
      }
    }
    const peers = this.link.numPeers ?? this.link.peers ?? 0;
    return {
      bpm: this.link.bpm,
      beat: this.beat,
      bar: this.bar,
      isPlaying: peers > 0 || true,
      linkPeers: peers
    };
  }
}
