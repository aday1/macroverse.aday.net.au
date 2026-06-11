const DEFAULT_DEVICE_KEY = '';

interface CameraEntry {
  stream: MediaStream;
  refCount: number;
  videos: Set<HTMLVideoElement>;
}

const cameras = new Map<string, CameraEntry>();

function deviceKey(deviceId?: string): string {
  return deviceId?.trim() || DEFAULT_DEVICE_KEY;
}

function buildVideoConstraints(deviceId?: string): MediaTrackConstraints {
  if (deviceId?.trim()) {
    return { deviceId: { exact: deviceId.trim() } };
  }
  return { facingMode: 'user' };
}

export async function enumerateCameraDevices(): Promise<MediaDeviceInfo[]> {
  if (typeof navigator === 'undefined' || !navigator.mediaDevices?.enumerateDevices) {
    return [];
  }
  try {
    const devices = await navigator.mediaDevices.enumerateDevices();
    return devices.filter((d) => d.kind === 'videoinput');
  } catch {
    return [];
  }
}

async function openStream(deviceId?: string): Promise<MediaStream> {
  if (!navigator.mediaDevices?.getUserMedia) {
    throw new Error('getUserMedia unavailable');
  }
  return navigator.mediaDevices.getUserMedia({
    video: buildVideoConstraints(deviceId),
    audio: false,
  });
}

/** Shared MediaStream per deviceId; ref-counted across callers. */
export async function acquireCamera(deviceId?: string): Promise<HTMLVideoElement> {
  const key = deviceKey(deviceId);
  let entry = cameras.get(key);
  if (!entry) {
    const stream = await openStream(deviceId);
    entry = { stream, refCount: 0, videos: new Set() };
    cameras.set(key, entry);
  }
  entry.refCount += 1;
  const video = document.createElement('video');
  video.srcObject = entry.stream;
  video.setAttribute('playsinline', '');
  video.muted = true;
  video.autoplay = true;
  entry.videos.add(video);
  try {
    await video.play();
  } catch {
    /* ignore autoplay block; texImage2D may still work once frames arrive */
  }
  return video;
}

export function releaseCamera(deviceId: string): void {
  const key = deviceKey(deviceId);
  const entry = cameras.get(key);
  if (!entry) return;
  entry.refCount -= 1;
  if (entry.refCount > 0) return;
  entry.stream.getTracks().forEach((t) => t.stop());
  entry.videos.forEach((v) => {
    v.srcObject = null;
  });
  cameras.delete(key);
}

/** Ready for WebGL texImage2D when video has frames. */
export function getVideoTextureSource(video: HTMLVideoElement): TexImageSource {
  return video;
}

export function isVideoTextureReady(video: HTMLVideoElement): boolean {
  return video.readyState >= HTMLMediaElement.HAVE_CURRENT_DATA;
}
