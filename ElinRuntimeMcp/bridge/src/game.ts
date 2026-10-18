/**
 * TCP client for the in-game RmcpListener (ElinRuntimeMcp/RmcpListener.cs).
 * One connection per request: connect, send one JSON line, read one JSON line, close.
 * Instances are discovered by probing the fixed port range with "hello".
 */
import * as net from "node:net";

export const PORT_START = 27551;
export const PORT_COUNT = 10;
export const HOST = "127.0.0.1";

/** Built-in hello fields. Anything else was added by another mod through the runtimemcp.hello event. */
const CORE = new Set(["pid", "port", "version", "gameVersion", "gameStarted", "sceneMode", "scripting"]);

export type Hello = {
	pid: number;
	port: number;
	version: string;
	gameVersion: string | null;
	gameStarted: boolean;
	sceneMode: string | null;
	scripting: boolean;
	[extra: string]: unknown;
};

export type Reply = {
	id: number;
	ok: boolean;
	result?: unknown;
	error?: string;
	trace?: string | null;
	ms: number;
};

/** The game answered with ok=false: the op itself failed (compile error, exception, unknown op…). */
export class GameError extends Error {
	readonly trace: string | null | undefined;
	constructor(message: string, trace?: string | null) {
		super(message);
		this.trace = trace;
	}
}

/** The port accepted the connection but no reply arrived in time: the main thread is busy (loading, blocked eval…). */
export class BusyError extends Error {}

let nextId = 1;

/** Send one op to one port. Rejects with GameError on an in-game failure, BusyError on a timeout after connecting, plain Error otherwise. */
export function request(port: number, op: string, args: Record<string, unknown> = {}, timeoutMs = 30_000): Promise<unknown> {
	return new Promise((resolve, reject) => {
		const id = nextId++;
		const socket = net.connect({ host: HOST, port });
		let buf = "";
		let done = false;
		let connected = false;

		const finish = (fn: () => void) => {
			if (done) return;
			done = true;
			clearTimeout(timer);
			socket.destroy();
			fn();
		};

		const timer = setTimeout(
			() =>
				finish(() => {
					const msg = `timeout after ${timeoutMs}ms waiting for '${op}' on port ${port}`;
					reject(connected ? new BusyError(msg + " (connected, main thread did not answer)") : new Error(msg));
				}),
			timeoutMs,
		);

		socket.setEncoding("utf8");
		socket.on("connect", () => {
			connected = true;
			socket.write(JSON.stringify({ id, op, args }) + "\n");
		});
		socket.on("data", (chunk: string) => {
			buf += chunk;
			const nl = buf.indexOf("\n");
			if (nl < 0) return;
			const line = buf.slice(0, nl);
			let reply: Reply;
			try {
				reply = JSON.parse(line) as Reply;
			} catch {
				finish(() => reject(new Error(`malformed reply from port ${port}: ${line.slice(0, 200)}`)));
				return;
			}
			finish(() => {
				if (reply.ok) resolve(reply.result);
				else reject(new GameError(reply.error ?? "unknown in-game error", reply.trace));
			});
		});
		socket.on("error", (e: Error) => finish(() => reject(e)));
		socket.on("close", () => finish(() => reject(new Error(`connection to port ${port} closed before a reply`))));
	});
}

/**
 * A port that accepted a connection. hello is null when the main thread did not answer in time (BUSY) or hello failed
 * (error). pid comes from the off-main-thread "ping" op, so it is known even for a busy instance.
 */
export type Instance = { port: number; hello: Hello | null; pid?: number; error?: string };

export function extras(h: Hello): [string, unknown][] {
	return Object.entries(h).filter(([k]) => !CORE.has(k));
}

export function describe(i: Instance): string {
	const h = i.hello;
	const pid = i.pid !== undefined ? `  pid ${i.pid}` : "";
	if (!h && i.error) return `port ${i.port}${pid}  ERROR (the listener answered but hello failed: ${i.error})`;
	if (!h) return `port ${i.port}${pid}  BUSY (connected, no hello within probe timeout: loading, blocked eval, or hung)`;
	const ext = extras(h)
		.map(([k, v]) => `  ${k} ${typeof v === "string" ? v : JSON.stringify(v)}`)
		.join("");
	return `port ${h.port}  pid ${h.pid}  scene ${h.sceneMode ?? "?"}  gameStarted ${h.gameStarted}  game ${h.gameVersion ?? "?"}  scripting ${h.scripting}${ext}`;
}

/** pid and port, answered off the main thread; null when nothing (or not our listener) answers. */
export async function ping(port: number, timeoutMs = 1_000): Promise<number | null> {
	try {
		const r = (await request(port, "ping", {}, timeoutMs)) as { pid: number };
		return typeof r?.pid === "number" ? r.pid : null;
	} catch {
		return null;
	}
}

async function probeOne(port: number, timeoutMs: number): Promise<Instance | null> {
	try {
		const hello = (await request(port, "hello", {}, timeoutMs)) as Hello;
		return { port, hello, pid: hello.pid };
	} catch (e) {
		if (e instanceof GameError) return { port, hello: null, pid: (await ping(port)) ?? undefined, error: e.message };
		if (e instanceof BusyError) {
			// something that accepts but never answers is not necessarily us; the listener answers ping even when busy
			const pid = await ping(port);
			return pid === null ? null : { port, hello: null, pid };
		}
		return null;
	}
}

/** Probe every port in the range. Ports with nothing listening are skipped; busy or failing listeners are kept with hello=null. */
export async function probe(timeoutMs = 2_000): Promise<Instance[]> {
	const ports = Array.from({ length: PORT_COUNT }, (_, i) => PORT_START + i);
	const results = await Promise.all(ports.map((port) => probeOne(port, timeoutMs)));
	return results.filter((r): r is Instance => r !== null);
}

export const NOT_RUNNING =
	`no game instance is listening on ${HOST}:${PORT_START}-${PORT_START + PORT_COUNT - 1}. ` +
	"Start the game with elin_launch. If it is already running: the Elin Runtime MCP mod must be enabled in the in-game mod list, " +
	"and Enabled must be true under [Listener] in BepInEx/config/dk.elinplugins.runtimemcp.cfg.";

/**
 * Resolve an instance selector.
 *   "any"    – the only instance (error if zero or several)
 *   "27552"  – a number in the port range is always a port: used without probing, so a busy instance can still be targeted
 *   "12345"  – any other number is a pid
 *   anything else – the instance with an extra hello field of that value, e.g. "host" when a mod reports role=Host
 */
export async function resolve(selector: string | undefined): Promise<Instance> {
	const sel = (selector ?? "any").trim().toLowerCase();

	if (/^\d+$/.test(sel)) {
		const n = Number(sel);
		if (n >= PORT_START && n < PORT_START + PORT_COUNT) return { port: n, hello: null };
		const instances = await probe();
		const hit = instances.find((i) => i.pid === n);
		if (!hit) throw new Error(`no instance with pid ${n}. Running: ${instances.map(describe).join("; ") || "none"}`);
		return hit;
	}

	const instances = await probe();
	if (instances.length === 0) throw new Error(NOT_RUNNING);
	const running = () => instances.map(describe).join("; ");

	const pool =
		sel === "any"
			? instances
			: instances.filter((i) => i.hello && extras(i.hello).some(([, v]) => typeof v === "string" && v.toLowerCase() === sel));

	if (pool.length === 1) return pool[0]!;
	if (pool.length === 0) {
		const unknown = instances.filter((i) => !i.hello).length;
		const hint = unknown ? ` ${unknown} instance(s) could not report their fields (BUSY or ERROR); target them by port.` : "";
		throw new Error(`no instance matches '${sel}'.${hint} Running: ${running()}`);
	}
	throw new Error(`'${sel}' is ambiguous, pick one by port or pid. Running: ${running()}`);
}
