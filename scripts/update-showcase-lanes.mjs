import fs from "node:fs";
import path from "node:path";

const root = process.cwd();
const outPath = path.join(root, "docs", "lane-status.json");
const featuresPath = path.join(root, "docs", "lane-features.json");

const LANES = [
  {
    id: "live",
    name: "Live",
    url: "https://macroverse.aday.net.au",
    branch: "main",
    track: "live",
    note: "Public cloud library"
  },
  {
    id: "test",
    name: "Test",
    url: "https://macroverse-test.aday.net.au",
    branch: "dev",
    track: "dev",
    note: "Pre-promotion integration"
  },
  {
    id: "dev",
    name: "Dev",
    url: "https://macroverse-dev.aday.net.au",
    branch: "dev",
    track: "dev",
    note: "Dev lane (same :dev image as test)"
  },
  {
    id: "private",
    name: "Private",
    url: "https://macroverse-private.aday.net.au",
    branch: "main",
    track: "aday",
    note: "Full library; basic auth on all paths including audience streams",
    auth: true
  }
];

async function fetchJson(url) {
  const res = await fetch(url, {
    headers: { "User-Agent": "macroverse-showcase-lanes/1.0" },
    signal: AbortSignal.timeout(15000)
  });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res.json();
}

function pickFeatures(catalog, semver) {
  if (!catalog?.versions?.length || !semver) return [];
  const num = parseFloat(semver);
  if (!Number.isFinite(num)) return [];
  const sorted = [...catalog.versions].sort(
    (a, b) => parseFloat(b.semver) - parseFloat(a.semver)
  );
  const match = sorted.find((v) => parseFloat(v.semver) <= num);
  return match ? match.features || [] : [];
}

async function hasPath(url, pathname) {
  try {
    const res = await fetch(`${url}${pathname}`, {
      method: "HEAD",
      headers: { "User-Agent": "macroverse-showcase-lanes/1.0" },
      signal: AbortSignal.timeout(8000)
    });
    return res.ok;
  } catch {
    return false;
  }
}

async function probeLane(lane, catalog) {
  const base = {
    id: lane.id,
    name: lane.name,
    url: lane.url,
    branch: lane.branch,
    track: lane.track,
    note: lane.note,
    ok: false,
    auth_required: false,
    error: ""
  };

  try {
    const meta = await fetchJson(`${lane.url}/deploy-meta.json`);
    let version = null;
    try {
      version = await fetchJson(`${lane.url}/api/version`);
    } catch {
      version = null;
    }

    const semverReported = version?.version || meta?.semver || "";
    let semverEffective = semverReported;
    let semver_note = "";
    const hasVr = await hasPath(lane.url, "/vj-vr.html");
    const stable = catalog.current_stable || semverReported;
    if (hasVr && parseFloat(semverReported || "0") < parseFloat(stable)) {
      semverEffective = stable;
      semver_note = `/api/version still reports ${semverReported}; runtime includes ${stable} (e.g. vj-vr.html)`;
    }
    const features = pickFeatures(catalog, semverEffective);
    const sync = meta.lane_sync || {};

    return {
      ...base,
      ok: true,
      semver: semverEffective,
      semver_reported: semverReported,
      semver_note,
      has_vr: hasVr,
      release_tag: version?.releaseTag || "",
      git_sha_short: meta.last_git_sha_short || meta.version || version?.gitRev || "",
      git_sha: meta.last_git_sha || "",
      git_url: meta.last_git_url || "",
      built_at: meta.last_build_at || meta.build_date || "",
      deployed_at: meta.last_deployed_at || meta.last_build_at || meta.build_date || "",
      built_branch: meta.branch || lane.branch,
      image_track: meta.track || lane.track,
      workflow_run_url: meta.workflow_run_url || "",
      changelog_url: meta.changelog_url || "",
      lane_sync: sync,
      promote_ready: Boolean(sync.promote_ready),
      dev_ahead_of_live: Number(sync.dev_commits_ahead_of_live || 0),
      dev_behind_live: Number(sync.dev_commits_behind_live || 0),
      branches_aligned: Boolean(sync.branches_aligned),
      features,
      features_title:
        catalog.versions?.find((v) => v.semver === semverEffective)?.title ||
        catalog.versions?.[0]?.title ||
        ""
    };
  } catch (err) {
    const msg = String(err?.message || err);
    if (lane.auth && /401|403/.test(msg)) {
      return { ...base, auth_required: true, error: "basic auth required for deploy-meta" };
    }
    return { ...base, error: msg };
  }
}

const catalog = JSON.parse(fs.readFileSync(featuresPath, "utf8"));
const lanes = {};
for (const lane of LANES) {
  lanes[lane.id] = await probeLane(lane, catalog);
}

if (lanes.private?.auth_required && lanes.live?.ok) {
  lanes.private = {
    ...lanes.private,
    ok: true,
    mirrored_from: "live",
    semver: lanes.live.semver,
    semver_reported: lanes.live.semver_reported,
    git_sha_short: lanes.live.git_sha_short,
    git_url: lanes.live.git_url,
    built_at: lanes.live.built_at,
    deployed_at: lanes.live.deployed_at,
    built_branch: "main",
    image_track: "aday",
    features: lanes.live.features,
    features_title: lanes.live.features_title,
    note: "Full library; basic auth on all paths; :aday image rebuilt on main CI (typically matches live SHA)"
  };
}

const live = lanes.live?.ok ? lanes.live : null;
const devLane = lanes.test?.ok ? lanes.test : lanes.dev?.ok ? lanes.dev : null;

const snapshot = {
  generated_at: new Date().toISOString(),
  current_stable: catalog.current_stable,
  branch_sync: {
    live_sha_short: live?.git_sha_short || "",
    dev_sha_short: devLane?.git_sha_short || "",
    dev_ahead_of_live: devLane?.dev_ahead_of_live ?? 0,
    dev_behind_live: devLane?.dev_behind_live ?? 0,
    branches_aligned: devLane?.branches_aligned ?? true,
    promote_ready: devLane?.promote_ready ?? false
  },
  lanes
};

fs.writeFileSync(outPath, JSON.stringify(snapshot, null, 2) + "\n", "utf8");
console.log(`Wrote ${outPath} (${Object.keys(lanes).length} lanes)`);
