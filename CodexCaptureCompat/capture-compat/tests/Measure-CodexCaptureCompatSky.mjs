import { createHash } from "node:crypto";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";
import { performance } from "node:perf_hooks";

const FIXTURE_TITLE = "Codex Computer Use fixture";
const MIN_RUNS = 5;
const MIN_WARM_SAMPLES = 5;
const DEFAULT_TIMEOUT_MS = 30_000;

function parseArgs(argv) {
  const options = { selfTest: false, runs: MIN_RUNS, samples: MIN_WARM_SAMPLES, outDir: "" };
  for (let index = 0; index < argv.length; index += 1) {
    const argument = argv[index];
    if (argument === "--self-test") options.selfTest = true;
    else if (argument === "--runs" || argument === "--samples" || argument === "--out-dir" || argument === "--runtime-dir" || argument === "--fixture-path" || argument === "--fixture-window-id") {
      const value = argv[++index];
      if (!value) throw new Error(`${argument} requires a value`);
      if (argument === "--runs") options.runs = Number(value);
      if (argument === "--samples") options.samples = Number(value);
      if (argument === "--out-dir") options.outDir = value;
      if (argument === "--runtime-dir") options.runtimeDir = value;
      if (argument === "--fixture-path") options.fixturePath = value;
      if (argument === "--fixture-window-id") options.fixtureWindowId = Number(value);
    } else if (argument === "--help" || argument === "-h") options.help = true;
    else throw new Error(`Unknown argument: ${argument}`);
  }
  if (!Number.isInteger(options.runs) || options.runs < MIN_RUNS || options.runs > 20) {
    throw new Error(`--runs must be an integer from ${MIN_RUNS} to 20`);
  }
  if (!Number.isInteger(options.samples) || options.samples < MIN_WARM_SAMPLES || options.samples > 50) {
    throw new Error(`--samples must be an integer from ${MIN_WARM_SAMPLES} to 50`);
  }
  return options;
}

function median(values) {
  if (!Array.isArray(values) || values.length === 0 || values.some((value) => !Number.isFinite(value))) {
    throw new TypeError("median requires a non-empty array of finite numbers");
  }
  const sorted = [...values].sort((left, right) => left - right);
  const middle = Math.floor(sorted.length / 2);
  return sorted.length % 2 === 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
}

function percentile(values, fraction) {
  if (!Number.isFinite(fraction) || fraction < 0 || fraction > 1) throw new RangeError("fraction must be 0..1");
  const sorted = [...values].sort((left, right) => left - right);
  if (sorted.length === 0) return null;
  return sorted[Math.min(sorted.length - 1, Math.ceil(fraction * sorted.length) - 1)];
}

function selectFixture(windows) {
  if (!Array.isArray(windows)) throw new TypeError("list_windows returned an invalid result");
  const matches = windows.filter((window) => window?.title === FIXTURE_TITLE &&
    typeof window?.app === "string" && window.app.trim() !== "" &&
    Number.isInteger(window?.id) && window.id >= 0);
  if (matches.length !== 1) {
    throw new Error(`Expected exactly one window titled '${FIXTURE_TITLE}', found ${matches.length}; no other window will be selected`);
  }
  return matches[0];
}

function createFixtureApproval(expectedFixture, getActiveRequest, approvalRecords) {
  const expectedProcessName = path.win32.basename(expectedFixture.app.slice("process:".length));
  return async (request) => {
    const meta = request?.meta;
    const app = meta?.tool_params?.app;
    const risk = meta?.riskLevel;
    const persist = meta?.persist;
    const activeRequest = getActiveRequest();
    const method = activeRequest?.method ?? null;
    const targetWindow = activeRequest?.params?.window;
    const exactTarget = method === "get_window_state" &&
      targetWindow?.app === expectedFixture.app &&
      targetWindow?.id === expectedFixture.id &&
      targetWindow?.title === FIXTURE_TITLE;
    const appIdentityMatches = app === expectedFixture.app || app === expectedProcessName;
    if (meta?.connector_id !== "computer-use" || !appIdentityMatches || !exactTarget || risk === "high" ||
        !Array.isArray(persist) || !persist.includes("session") ||
        persist.length === 0) {
      const safe = { method, connectorId: meta?.connector_id ?? null, app: app ?? null, risk: risk ?? null,
        requestTargetApp: targetWindow?.app ?? null, requestTargetId: targetWindow?.id ?? null,
        requestTargetTitle: targetWindow?.title ?? null, persistenceChoices: Array.isArray(persist) ? persist : null };
      approvalRecords.push({ accepted: false, ...safe });
      throw new Error(`Refused out-of-scope app approval: ${JSON.stringify(safe)}`);
    }
    approvalRecords.push({ accepted: true, method, connectorId: meta.connector_id, approvalApp: app,
      targetApp: targetWindow.app, targetWindowId: targetWindow.id, targetWindowTitle: targetWindow.title,
      risk: risk ?? null, persistenceChoices: persist });
    return { action: "accept" };
  };
}

function imageInfo(state) {
  const screenshots = state?.screenshots;
  if (!Array.isArray(screenshots) || screenshots.length === 0) throw new Error("The helper returned no screenshot");
  const screenshot = screenshots[0];
  const url = screenshot?.url;
  if (typeof url !== "string" || url.length === 0) throw new Error("Screenshot URL is missing");
  let mime = "unknown";
  let imageBytes = null;
  let imageSha256 = null;
  let base64ParseAndHashMs = null;
  const dataUrl = /^data:([^;,]+)?;base64,([A-Za-z0-9+/=\r\n]+)$/.exec(url);
  if (dataUrl) {
    mime = dataUrl[1] || "application/octet-stream";
    const base64 = dataUrl[2].replace(/[\r\n]/g, "");
    if (base64.length % 4 !== 0 || !/^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$/.test(base64)) {
      throw new Error("Screenshot data URL contains malformed base64");
    }
    const inspectStart = performance.now();
    const bytes = Buffer.from(base64, "base64");
    imageSha256 = createHash("sha256").update(bytes).digest("hex");
    imageBytes = bytes.byteLength;
    base64ParseAndHashMs = performance.now() - inspectStart;
  } else if (url.startsWith("data:")) {
    mime = url.slice(5, url.indexOf(",") > 0 ? url.indexOf(",") : undefined).split(";")[0] || "unknown";
  }
  return {
    screenshotCount: screenshots.length,
    mime,
    imageUrlChars: url.length,
    imageBytes,
    imageSha256,
    base64ParseAndHashMs,
    metadataWidth: Number.isFinite(screenshot.width) ? screenshot.width : null,
    metadataHeight: Number.isFinite(screenshot.height) ? screenshot.height : null,
  };
}

function csvCell(value) {
  if (value === null || value === undefined) return "";
  const text = String(value);
  return /[\r\n,"]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text;
}

function writeCsv(records) {
  const headers = ["recordType", "run", "sample", "phase", "apiTotalMs", "helperRequestResponseMs",
    "skyAdapterResidualMs", "base64ParseAndHashMs", "responseObjectJsonBytes", "imageUrlChars", "imageBytes",
    "metadataWidth", "metadataHeight", "mime", "imageSha256"];
  return [headers.join(","), ...records.map((record) => headers.map((key) => csvCell(record[key])).join(","))].join("\r\n") + "\r\n";
}

function printHelp() {
  process.stdout.write(`Usage (through the .bat launcher):\n  Measure-CodexCaptureCompatSky.bat --fixture-window-id ID [--runs 5] [--samples 5] [--out-dir PATH]\n  Measure-CodexCaptureCompatSky.bat --self-test\n\n` +
    `Live mode requires the window ID obtained from the exact '${FIXTURE_TITLE}' fixture in the Windows window inventory.\n` +
    `Each run launches a fresh helper on its first target-window request, reports cold first capture separately, then records at least five warm captures.\n` +
    `The helper receives an ephemeral approval only when the active RPC carries the exact fixture path, window ID, and title; no approval is persisted.\n` +
    `The harness reports base64 byte parsing/hash time and dimensions declared by screenshot metadata; it does not decode image pixels.\n` +
    `No application other than the fixture is activated or closed. The installed @oai/sky package and MSIX are read-only.\n`);
}

async function runSelfTest() {
  const assert = (condition, message) => { if (!condition) throw new Error(`Self-test failed: ${message}`); };
  const parsedArgs = parseArgs(["--fixture-window-id", "42", "--runs", "5", "--samples", "5"]);
  assert(parsedArgs.fixtureWindowId === 42 && parsedArgs.runs === 5 && parsedArgs.samples === 5,
    "fixture window id and sample counts parse from launcher arguments");
  assert(median([170, 92, 84, 86, 78]) === 86, "median handles odd sample counts");
  assert(median([40, 10, 30, 20]) === 25, "median handles even sample counts");
  assert(percentile([1, 2, 3, 4, 5], 0.95) === 5, "p95 uses nearest-rank upper sample");
  assert(selectFixture([{ app: "fixture.exe", id: 2, title: FIXTURE_TITLE }]).id === 2, "exact fixture selection");
  for (const windows of [[], [
    { app: "fixture.exe", id: 2, title: FIXTURE_TITLE },
    { app: "fixture.exe", id: 3, title: FIXTURE_TITLE },
  ]]) {
    let threw = false;
    try { selectFixture(windows); } catch { threw = true; }
    assert(threw, "missing and ambiguous fixture windows fail closed");
  }
  const expectedFixture = { app: "process:C:\\fixture\\capture_test_window.exe", id: 42, title: FIXTURE_TITLE };
  const exactTargetRequest = { method: "get_window_state", params: { window: expectedFixture } };
  const exactApprovalRecords = [];
  const exactApproval = createFixtureApproval(expectedFixture, () => exactTargetRequest, exactApprovalRecords);
  const exactRequest = { meta: { connector_id: "computer-use", tool_params: { app: "capture_test_window.exe" },
    riskLevel: "low", persist: ["session", "always"] } };
  const approval = await exactApproval(exactRequest);
  assert(approval.action === "accept" && !("_meta" in approval), "exact window-bound approval is ephemeral even when persistent choice is offered");
  assert(exactApprovalRecords.length === 1 && exactApprovalRecords[0].accepted === true, "ephemeral approval is recorded");
  for (const [name, request, activeRequest] of [
    ["wrong app identity", { meta: { ...exactRequest.meta, tool_params: { app: "other.exe" } } }, exactTargetRequest],
    ["wrong target path", exactRequest, { method: "get_window_state", params: { window: { ...expectedFixture, app: "process:C:\\other\\capture_test_window.exe" } } }],
    ["wrong target window ID", exactRequest, { method: "get_window_state", params: { window: { ...expectedFixture, id: 43 } } }],
    ["wrong target title", exactRequest, { method: "get_window_state", params: { window: { ...expectedFixture, title: "Other window" } } }],
    ["high risk", { meta: { ...exactRequest.meta, riskLevel: "high" } }, exactTargetRequest],
    ["no session approval", { meta: { ...exactRequest.meta, persist: ["always"] } }, exactTargetRequest],
    ["write action", exactRequest, { method: "click", params: { window: expectedFixture } }],
  ]) {
    let threw = false;
    try { await createFixtureApproval(expectedFixture, () => activeRequest, [])(request); }
    catch { threw = true; }
    assert(threw, `approval refuses ${name}`);
  }
  const parsed = imageInfo({ screenshots: [{ url: "data:image/png;base64,aGVsbG8=", width: 2, height: 3 }] });
  assert(parsed.mime === "image/png" && parsed.imageBytes === 5 && parsed.metadataWidth === 2 && parsed.metadataHeight === 3,
    "data URL inspection preserves metadata dimensions and reports base64 byte count");
  assert(Number.isFinite(parsed.base64ParseAndHashMs) && parsed.imageSha256?.length === 64, "base64 parsing and byte-hash telemetry");
  let malformedBase64Threw = false;
  try { imageInfo({ screenshots: [{ url: "data:image/png;base64,abc", width: 1, height: 1 }] }); }
  catch { malformedBase64Threw = true; }
  assert(malformedBase64Threw, "malformed base64 is rejected without claiming image decoding");
  process.stdout.write("PASS: statistics, exact fixture selection, ephemeral approval guard, base64 byte hash, and metadata telemetry.\n");
}

async function sha256File(filePath) {
  return createHash("sha256").update(await readFile(filePath)).digest("hex");
}

async function importRuntime(runtimeDir) {
  if (!runtimeDir) throw new Error("The launcher did not provide --runtime-dir");
  const runtimeRoot = path.resolve(runtimeDir);
  const bin = path.join(runtimeRoot, "bin");
  const packageRoot = path.join(bin, "node_modules", "@oai", "sky");
  const manifestPath = path.join(packageRoot, "package.json");
  const manifest = JSON.parse(await readFile(manifestPath, "utf8"));
  if (manifest.name !== "@oai/sky" || typeof manifest.version !== "string") {
    throw new Error("The runtime package manifest does not identify @oai/sky; stopping without loading it");
  }
  const clientPath = path.join(packageRoot, "dist", "project", "cua", "sky_js", "src", "targets", "windows", "internal", "computer_use_client.js");
  const helperTransportPath = path.join(packageRoot, "dist", "project", "cua", "sky_js", "src", "targets", "windows", "internal", "helper_transport.js");
  const helperPath = path.join(packageRoot, "bin", "windows", process.arch === "arm64" ? "codex-computer-use-arm64.exe" : "codex-computer-use.exe");
  const [clientModule, transportModule] = await Promise.all([
    import(pathToFileURL(clientPath).href),
    import(pathToFileURL(helperTransportPath).href),
  ]);
  if (typeof clientModule.WindowsComputerUseClient !== "function" || typeof transportModule.WindowsHelperTransport !== "function") {
    throw new Error("This @oai/sky build changed its internal Windows transport exports; refusing to benchmark an unknown path");
  }
  return {
    packageRoot,
    manifest,
    helperPath,
    WindowsComputerUseClient: clientModule.WindowsComputerUseClient,
    WindowsHelperTransport: transportModule.WindowsHelperTransport,
    files: {
      package: await sha256File(manifestPath),
      client: await sha256File(clientPath),
      transport: await sha256File(helperTransportPath),
      helper: await sha256File(helperPath),
    },
  };
}

function createMeasuredClient(runtime, measurements, expectedFixture, approvalRecords) {
  let activeRequest = null;
  const createElicitation = createFixtureApproval(expectedFixture, () => activeRequest, approvalRecords);
  const rawTransport = new runtime.WindowsHelperTransport({
    helperArgs: ["--parent-pid", String(process.pid)],
    helperCommand: runtime.helperPath,
    timeoutMs: DEFAULT_TIMEOUT_MS,
  });
  const measuredTransport = {
    async request(method, params, options) {
      const start = performance.now();
      let result;
      let error = null;
      activeRequest = { method, params };
      try {
        result = await rawTransport.request(method, params, { ...options, createElicitation });
        return result;
      } catch (caught) {
        error = caught instanceof Error ? caught.message : String(caught);
        throw caught;
      } finally {
        const elapsedMs = performance.now() - start;
        measurements.push({ method, elapsedMs, responseValue: result, error });
        activeRequest = null;
      }
    },
    close: () => rawTransport.close(),
  };
  return new runtime.WindowsComputerUseClient({ transport: measuredTransport, timeoutMs: DEFAULT_TIMEOUT_MS });
}

async function runLive(options) {
  if (!options.fixturePath) throw new Error("The launcher did not provide the isolated fixture path");
  if (!Number.isInteger(options.fixtureWindowId) || options.fixtureWindowId < 0) {
    throw new Error("Live mode requires --fixture-window-id from the verified Windows window inventory");
  }
  const runtime = await importRuntime(options.runtimeDir);
  const expectedFixtureApp = `process:${path.resolve(options.fixturePath)}`;
  const fixture = { app: expectedFixtureApp, id: options.fixtureWindowId, title: FIXTURE_TITLE };
  const now = new Date();
  const stamp = now.toISOString().replaceAll(":", "-").replaceAll(".", "-");
  const outDir = path.resolve(options.outDir || path.join(process.env.TEMP || process.cwd(), "CodexCaptureCompat", `sky-benchmark-${stamp}`));
  await mkdir(outDir, { recursive: true });
  const sampleRecords = [];
  const runRecords = [];
  const globalWarmRpc = [];
  const globalWarmTotal = [];
  const globalWarmAdapter = [];
  const coldCaptureRpc = [];
  const coldCaptureTotal = [];
  const coldCaptureAdapter = [];
  const approvalRecords = [];

  process.stdout.write(`@oai/sky ${runtime.manifest.version}; helper SHA-256 ${runtime.files.helper}\n`);
  process.stdout.write(`Fixture target is preselected by exact app path, window ID ${fixture.id}, and title '${FIXTURE_TITLE}'. The helper will not enumerate other windows.\n`);
  for (let run = 1; run <= options.runs; run += 1) {
    const rpcMeasurements = [];
    const client = createMeasuredClient(runtime, rpcMeasurements, fixture, approvalRecords);
    try {
      runRecords.push({ type: "helper_run", run, fixtureTitle: fixture.title, fixtureApp: fixture.app,
        fixtureWindowId: fixture.id, helperStartsOnFirstCapture: true });

      for (let capture = 0; capture <= options.samples; capture += 1) {
        const phase = capture === 0 ? "cold_helper_and_first_capture" : "warm";
        const rpcStartIndex = rpcMeasurements.length;
        const apiStart = performance.now();
        const state = await client.get_window_state({ window: fixture, include_screenshot: true, include_text: false });
        const apiTotalMs = performance.now() - apiStart;
        const rpcMeasurement = [...rpcMeasurements.slice(rpcStartIndex)].reverse().find((measurement) => measurement.method === "get_window_state");
        if (!rpcMeasurement) throw new Error("Could not attribute a helper get_window_state request to the capture");
        try { rpcMeasurement.responseObjectJsonBytes = Buffer.byteLength(JSON.stringify(rpcMeasurement.responseValue), "utf8"); } catch { rpcMeasurement.responseObjectJsonBytes = null; }
        rpcMeasurement.responseValue = undefined;
        const info = imageInfo(state);
        const adapterResidualMs = Math.max(0, apiTotalMs - rpcMeasurement.elapsedMs);
        const record = {
          type: "capture",
          run,
          sample: capture,
          phase,
          fixtureTitle: fixture.title,
          apiTotalMs,
          helperRequestResponseMs: rpcMeasurement.elapsedMs,
          skyAdapterResidualMs: adapterResidualMs,
          responseObjectJsonBytes: rpcMeasurement.responseObjectJsonBytes,
          base64ParseAndHashMs: info.base64ParseAndHashMs,
          imageUrlChars: info.imageUrlChars,
          imageBytes: info.imageBytes,
          metadataWidth: info.metadataWidth,
          metadataHeight: info.metadataHeight,
          mime: info.mime,
          imageSha256: info.imageSha256,
          screenshotCount: info.screenshotCount,
          imageDeliveryMs: null,
          imageDeliveryNote: "The Codex host image sink is not present in this standalone Node harness; delivery to the model/UI is not measured.",
        };
        sampleRecords.push(record);
        if (phase === "warm") {
          globalWarmRpc.push(record.helperRequestResponseMs);
          globalWarmTotal.push(record.apiTotalMs);
          globalWarmAdapter.push(record.skyAdapterResidualMs);
        } else {
          coldCaptureRpc.push(record.helperRequestResponseMs);
          coldCaptureTotal.push(record.apiTotalMs);
          coldCaptureAdapter.push(record.skyAdapterResidualMs);
        }
        process.stdout.write(`Run ${run}/${options.runs} ${phase}: helper=${record.helperRequestResponseMs.toFixed(2)} ms, ` +
          `@oai/sky total=${record.apiTotalMs.toFixed(2)} ms, residual=${record.skyAdapterResidualMs.toFixed(2)} ms, ` +
          `${info.metadataWidth ?? "?"}x${info.metadataHeight ?? "?"} metadata dimensions, ${info.imageBytes ?? "?"} image bytes\n`);
      }
    } finally {
      await client.close();
    }
  }

  const summary = {
    type: "summary",
    capturedAtUtc: new Date().toISOString(),
    runtime: { version: runtime.manifest.version, runtimeDir: options.runtimeDir, fileSha256: runtime.files },
    measurement: {
      runs: options.runs,
      warmSamplesPerRun: options.samples,
      warmCaptureCount: globalWarmRpc.length,
      coldCaptureCount: coldCaptureRpc.length,
      fixtureTitle: FIXTURE_TITLE,
      fixtureApp: fixture.app,
      fixtureWindowId: fixture.id,
      ephemeralApprovals: approvalRecords,
      helperStartupBoundary: "The helper starts lazily on the first exact get_window_state request; cold-helper startup and first capture are combined in that request interval.",
      helperRequestResponseIncludes: "JSON request write through parsed helper JSON response over the local helper transport",
      skyAdapterResidualIncludes: "get_window_state validation and shape normalization after helper response; approximate difference of nested wall-clock intervals. The host emitImage sink is absent.",
      imageBytesHashStage: "standalone data-URL base64 parsing and SHA-256 after get_window_state has returned; no pixel/image decoding",
      responseObjectJsonBytesNote: "UTF-8 size of JSON.stringify(parsed response object), not raw helper wire bytes",
      imageDeliveryMeasured: false,
      caveat: "Standalone helper/@oai/sky timing is not the complete Codex tool-to-model image delivery latency and is not directly comparable to the official CUA tool result.",
    },
    groups: {
      coldHelperAndFirstCapture: {
        count: coldCaptureTotal.length,
        helperRequestResponseMedianMs: median(coldCaptureRpc),
        apiTotalMedianMs: median(coldCaptureTotal),
        skyAdapterResidualMedianMs: median(coldCaptureAdapter),
      },
      warm: {
        count: globalWarmTotal.length,
        helperRequestResponseMedianMs: median(globalWarmRpc),
        helperRequestResponseP95Ms: percentile(globalWarmRpc, 0.95),
        apiTotalMedianMs: median(globalWarmTotal),
        apiTotalP95Ms: percentile(globalWarmTotal, 0.95),
        skyAdapterResidualMedianMs: median(globalWarmAdapter),
      },
    },
  };

  const jsonlPath = path.join(outDir, "samples.jsonl");
  const csvPath = path.join(outDir, "summary.csv");
  const manifestPath = path.join(outDir, "benchmark-manifest.json");
  const approvalEvents = approvalRecords.map((record) => ({ type: "ephemeral_app_approval", ...record }));
  await writeFile(jsonlPath, [...approvalEvents, ...runRecords, ...sampleRecords].map((record) => JSON.stringify(record)).join("\r\n") + "\r\n", "utf8");
  const csvRecords = sampleRecords.map((record) => ({ ...record, recordType: record.type }));
  await writeFile(csvPath, writeCsv(csvRecords), "utf8");
  await writeFile(manifestPath, JSON.stringify(summary, null, 2) + "\r\n", "utf8");
  process.stdout.write(`\nWarm median: helper ${summary.groups.warm.helperRequestResponseMedianMs.toFixed(2)} ms; ` +
    `@oai/sky total ${summary.groups.warm.apiTotalMedianMs.toFixed(2)} ms; ` +
    `adapter residual ${summary.groups.warm.skyAdapterResidualMedianMs.toFixed(2)} ms.\n`);
  process.stdout.write(`Cold helper + first capture median: ${summary.groups.coldHelperAndFirstCapture.apiTotalMedianMs.toFixed(2)} ms.\n`);
  process.stdout.write(`Image delivery to Codex is not measured here. Results: ${outDir}\n`);
}

try {
  const options = parseArgs(process.argv.slice(2));
  if (options.help) { printHelp(); process.exitCode = 0; }
  else if (options.selfTest) await runSelfTest();
  else await runLive(options);
} catch (error) {
  process.stderr.write(`ERROR: ${error instanceof Error ? error.message : String(error)}\n`);
  process.exitCode = 1;
}
