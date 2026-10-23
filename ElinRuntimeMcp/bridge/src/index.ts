/**
 * Elin Runtime MCP bridge (stdio). Wraps the in-game RmcpListener plus a few offline helpers (dotnet build, launch, kill).
 * Shipped as package/bridge/elin-mcp.mjs; the in-game console command rmcp.setup prints the client configuration,
 * and `node elin-mcp.mjs --config` prints it for every common MCP client.
 */
import * as fs from "node:fs";
import * as os from "node:os";
import * as path from "node:path";
import { fileURLToPath } from "node:url";
import { type CallToolResult, McpServer, type ServerContext } from "@modelcontextprotocol/server";
import { serveStdio } from "@modelcontextprotocol/server/stdio";
import { z } from "zod";

import * as game from "./game.ts";
import * as logs from "./logs.ts";
import * as proc from "./proc.ts";

const VERSION = "1.0.0";
// characters, not tokens: 24k stays under Claude Code's 25k-token cap even for CJK-heavy logs
const MAX_TEXT = 24_000;
const KNOWLEDGE_URL = "https://mcp.elin-modding.net";

const SIGNATURE_RECIPE =
	'string.Join("\\n", typeof(Chara).GetMethods(AccessTools.all).Where(m => m.Name == "DamageHP").Select(m => m.DeclaringType.Name + "." + m.Name + "(" + string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name)) + ")"))';

const LOAD_SAVE_RECIPE =
	"list saves with elin_eval `JsonConvert.SerializeObject(GameIO.GetGameList(CorePath.RootSave).Select(g => new { g.id, g.pcName }))`, " +
	'load one with `Game.Load("<id>", false); return "loading";`, then elin_wait_eval `EClass.core.IsGameStarted`';

/** Sent to every MCP client on initialize; the orientation an agent needs before its first call. */
const INSTRUCTIONS = `Elin Runtime MCP drives a running copy of the game Elin (Unity, Mono, BepInEx) through the "Elin Runtime MCP" mod.

Start with elin_instances. If nothing is listening, elin_launch starts the game through Steam and waits for it. The title screen is enough for reflection and console commands; anything touching the player needs a loaded save: ${LOAD_SAVE_RECIPE}.

Checking a change to a mod:
1. elin_build compiles it (the project comes from the ELIN_BUILD_PROJECT env var or the project argument).
2. elin_kill, then elin_launch: a running game keeps the DLL it loaded until it restarts.
3. Load a save, then elin_eval / elin_command to act and inspect, elin_screenshot to look, elin_logs for errors.
To try a patch without rebuilding, elin_hotload one .cs file or source string: call new Harmony("some.id").PatchAll(...) from a static EScriptLoad() and harmony.UnpatchSelf() from EScriptUnload(). Never call the static Harmony.UnpatchAll(): it removes every mod's patches.

elin_eval runs a C# script on the Unity main thread and the game freezes while it runs. Never await, and never loop waiting for game state inside a script; poll with elin_wait_eval instead. Qualify game statics (EClass.pc, EClass._zone, EClass.game, EClass.core). Return JsonConvert.SerializeObject(...) for structured results. Script["key"] keeps values between calls.

Looking up game code: this server does not index source.
- If the Elin knowledge MCP server (${KNOWLEDGE_URL}) is connected, use its elin_guide, elin_find, elin_callers and elin_read tools. If it is not, offer it to the user: it needs no install, and ${KNOWLEDGE_URL}/llms.txt is its setup guide.
- For the exact signatures of the installed game version, including inherited members and parameter names (Harmony injects by name), reflect in elin_eval: ${SIGNATURE_RECIPE}
- For full-text search, decompile <game>/Elin_Data/Managed/Elin.dll locally with ilspycmd.

elin_logs holds this instance's BepInEx and Unity log since the process started. Lines logged before this mod loaded show "startup" instead of a time; search those with contains, since their level is not recorded. Why a mod did not load, by what to search for: "Failed to activate mod" (its activation threw), "Skipped (" (made for an older game version, or a dependency is missing), "Blocked:" (incompatible with another mod), "Not a package:" (folder without package.xml), "does not parse" (broken package.xml; the game then refuses to start). "Activating(" only means activation was attempted. Older stable game builds log fewer of these lines.

Several instances (a second copy in a Sandboxie box, a multiplayer host and client): each takes a port in 27551-27560. Pass instance as a port, a pid, or a value that another mod reports in elin_instances (e.g. host).`;

function text(s: string, keep: "head" | "tail" = "head"): CallToolResult {
	let clipped = s;
	if (s.length > MAX_TEXT) {
		const note = `… clipped ${s.length - MAX_TEXT} chars`;
		clipped = keep === "head" ? s.slice(0, MAX_TEXT) + "\n" + note : note + "\n" + s.slice(-MAX_TEXT);
	}
	return { content: [{ type: "text", text: clipped }] };
}

function error(s: string): CallToolResult {
	return { isError: true, content: [{ type: "text", text: s }] };
}

/** Uniform error handling: in-game and transport failures become isError results instead of protocol errors. */
function guarded<A>(fn: (a: A, ctx: ServerContext) => Promise<CallToolResult>): (a: A, ctx: ServerContext) => Promise<CallToolResult> {
	return async (a, ctx) => {
		try {
			return await fn(a, ctx);
		} catch (e) {
			let msg = e instanceof Error ? e.message : String(e);
			if (e instanceof game.GameError && e.trace) msg += "\n" + e.trace;
			return error(msg);
		}
	};
}

function show(v: unknown): string {
	if (v === null || v === undefined) return "(null or void)";
	if (typeof v === "string") return v;
	return JSON.stringify(v, null, 2);
}

function truthy(v: unknown): boolean {
	if (v === null || v === undefined || v === false || v === 0) return false;
	if (typeof v === "string") return v !== "" && v.toLowerCase() !== "false" && v !== "0";
	return true;
}

const sleep = (ms: number) => new Promise<void>((r) => setTimeout(r, ms));

const instanceArg = z
	.string()
	.default("any")
	.describe(
		"Which game instance: any (the only one running), a port (27551-27560), a pid, or a value another mod reports in hello " +
			"(e.g. host/client when a multiplayer mod adds a role). elin_instances lists them.",
	);

const timeoutArg = (def: number) =>
	z.number().int().min(1_000).max(600_000).default(def).describe(`Timeout in ms (default ${def}).`);

const READ_ONLY = { readOnlyHint: true, openWorldHint: false } as const;
const ACTS = { readOnlyHint: false, destructiveHint: true, openWorldHint: false } as const;

function createServer() {
	const server = new McpServer({ name: "elin-runtime", version: VERSION }, { instructions: INSTRUCTIONS });

	// ── in-game ────────────────────────────────────────────────────────────

	server.registerTool(
		"elin_instances",
		{
			description:
				"List running Elin instances with the Elin Runtime MCP mod: port, pid, scene mode, game version, plus fields other mods add. " +
				"BUSY = the main thread did not answer (loading, a long eval); ERROR = hello failed. Both can still be targeted by port. " +
				"Call this first, and whenever a tool says the instance is ambiguous.",
			inputSchema: {},
			annotations: READ_ONLY,
		},
		guarded(async () => {
			const list = await game.probe();
			return text(list.length ? list.map(game.describe).join("\n") : game.NOT_RUNNING);
		}),
	);

	server.registerTool(
		"elin_eval",
		{
			description:
				"Evaluate a C# script inside the game on the Unity main thread; the game freezes while it runs. Never `await` and never " +
				"loop waiting for game state inside a script (poll with elin_wait_eval). A call that times out here still runs to " +
				"completion in the game, so do not blindly retry scripts with side effects. " +
				"The last expression is the value, or `return x;`. Strings, bools and numbers come back as-is; anything else as " +
				"ToString(), so serialize structure: `return JsonConvert.SerializeObject(new { EClass.pc.uid, EClass.pc.hp });`. " +
				"Qualify game statics: EClass.pc, EClass._zone, EClass.game, EClass.core. Usings: System, System.Linq, " +
				"System.Collections.Generic, System.IO, System.Reflection, UnityEngine, HarmonyLib, Newtonsoft.Json, EModding. " +
				"Public types of any loaded mod work by full name; internal ones need AccessTools. Script[\"key\"] = value persists " +
				"across calls (object-typed). Compile errors return line numbers, exceptions return stack traces.",
			inputSchema: {
				instance: instanceArg,
				code: z.string().min(1).describe("C# script source."),
				timeout_ms: timeoutArg(60_000),
			},
			annotations: ACTS,
		},
		guarded(async ({ instance, code, timeout_ms }) => {
			const i = await game.resolve(instance);
			return text(show(await game.request(i.port, "eval", { code }, timeout_ms)));
		}),
	);

	server.registerTool(
		"elin_command",
		{
			description:
				"Run an in-game console command (ReflexCLI) exactly as typed in the console and return its output. " +
				"Examples: mod.pos (where the player is), mod.elin_dev true (turns on the game's debug cheats without a restart), " +
				"csc.recompile, and any command a mod registers. An unknown command comes back as an error.",
			inputSchema: {
				instance: instanceArg,
				cmd: z.string().min(1).describe("Console command line."),
				timeout_ms: timeoutArg(60_000),
			},
			annotations: ACTS,
		},
		guarded(async ({ instance, cmd, timeout_ms }) => {
			const i = await game.resolve(instance);
			const out = show(await game.request(i.port, "command", { cmd }, timeout_ms));
			// ReflexCLI reports failures as ordinary return text
			const failed = out.startsWith("Unrecognised Command") || out.startsWith("Command generated internal exception");
			return failed ? error(out) : text(out || "(no output)");
		}),
	);

	server.registerTool(
		"elin_state",
		{
			description:
				"Snapshot as JSON: scene mode, and when a save is loaded the current zone (full name, uid) and the pc (uid, pos, hp, held, " +
				`ai class, isDead), plus sections other mods add. At the title screen only the scene is there; ${LOAD_SAVE_RECIPE}.`,
			inputSchema: { instance: instanceArg },
			annotations: READ_ONLY,
		},
		guarded(async ({ instance }) => {
			const i = await game.resolve(instance);
			return text(show(await game.request(i.port, "state", {}, 15_000)));
		}),
	);

	server.registerTool(
		"elin_screenshot",
		{
			description:
				"Capture the game window at the end of the current frame and save the PNG under the system temp dir. With inline=true " +
				"(default) the image is also returned; set inline=false if your client cannot show images or you only need the file. " +
				"scale defaults to 0.5; 1 is full resolution, which on large screens can exceed a client's image size limit.",
			inputSchema: {
				instance: instanceArg,
				scale: z.number().min(0.1).max(1).default(0.5).describe("Downscale factor."),
				inline: z.boolean().default(true).describe("Return the image itself, not only its path."),
			},
			annotations: READ_ONLY,
		},
		guarded(async ({ instance, scale, inline }) => {
			const i = await game.resolve(instance);
			const r = (await game.request(i.port, "screenshot", { scale }, 30_000)) as { png: string; width: number; height: number };
			const dir = path.join(os.tmpdir(), "elin-runtime-mcp");
			fs.mkdirSync(dir, { recursive: true });
			const d = new Date();
			const p = (n: number, w = 2) => String(n).padStart(w, "0");
			const stamp = `${d.getFullYear()}${p(d.getMonth() + 1)}${p(d.getDate())}_${p(d.getHours())}${p(d.getMinutes())}${p(d.getSeconds())}_${p(d.getMilliseconds(), 3)}`;
			const file = path.join(dir, `screen_${i.port}_${stamp}.png`);
			fs.writeFileSync(file, Buffer.from(r.png, "base64"));
			const caption = { type: "text" as const, text: `${file} (${r.width}x${r.height}, ${Math.round(r.png.length / 1365)} KB)` };
			return { content: inline ? [caption, { type: "image", data: r.png, mimeType: "image/png" }] : [caption] };
		}),
	);

	server.registerTool(
		"elin_hotload",
		{
			description:
				"Compile C# inside the game with its own Roslyn and load it as a throwaway assembly, to try a Harmony patch or a helper " +
				"without rebuilding your mod. Pass a .cs file or the source itself. It may declare types and namespaces, and it " +
				"references every assembly the game has loaded, including your mod's public types. A static EScriptLoad() in any type " +
				"runs after load and EScriptUnload() runs on elin_hotunload: call new Harmony(\"some.id\").PatchAll(...) and " +
				"harmony.UnpatchSelf() there. Never call the static Harmony.UnpatchAll(), which removes every mod's patches. " +
				"Compile errors come back with line numbers; an exception thrown by EScriptLoad comes back as an error with its real " +
				"message (the assembly is loaded anyway). Mono cannot unload assemblies, so every load stays until the game restarts " +
				"and a name can be used once; the default name carries a millisecond stamp.",
			inputSchema: {
				instance: instanceArg,
				file: z.string().optional().describe("Path to a .cs file, absolute or relative to the MCP server's working directory (your project root)."),
				source: z.string().optional().describe("The C# source itself, instead of file."),
				name: z.string().optional().describe("Assembly name; defaults to hot_<file>_<HHmmssfff>. Needed for elin_hotunload."),
				timeout_ms: timeoutArg(120_000),
			},
			annotations: ACTS,
		},
		guarded(async ({ instance, file, source, name, timeout_ms }) => {
			if (!file === !source) throw new Error("pass exactly one of file or source");
			let code = source!;
			let label = "inline.cs";
			if (file) {
				label = path.resolve(file);
				if (!fs.existsSync(label)) throw new Error(`file not found: ${label}`);
				code = fs.readFileSync(label, "utf8");
			}
			const i = await game.resolve(instance);
			return text(show(await game.request(i.port, "load", { source: code, file: label, name }, timeout_ms)));
		}),
	);

	server.registerTool(
		"elin_hotunload",
		{
			description:
				"Run EScriptUnload() of an assembly loaded by elin_hotload and unregister its console commands. Only names loaded by " +
				"elin_hotload in this game session are accepted. An exception it throws comes back as an error, meaning the patch may " +
				"still be applied. The assembly itself stays in memory until the game restarts.",
			inputSchema: {
				instance: instanceArg,
				name: z.string().min(1).describe("Assembly name returned by elin_hotload."),
			},
			annotations: ACTS,
		},
		guarded(async ({ instance, name }) => {
			const i = await game.resolve(instance);
			return text(show(await game.request(i.port, "unload", { name }, 30_000)));
		}),
	);

	server.registerTool(
		"elin_wait_eval",
		{
			description:
				"Poll a C# expression in the game until it is truthy (true, non-zero, non-empty string other than \"False\") or the " +
				"timeout passes. Runtime exceptions (e.g. EClass.pc is null before a save is loaded) count as false and polling " +
				"continues; a compile error stops at once. Use after triggering an action, e.g. `EClass.core.IsGameStarted` or " +
				"`EClass.pc.pos.x == 12`. Each poll briefly runs on the game's main thread.",
			inputSchema: {
				instance: instanceArg,
				code: z.string().min(1).describe("C# expression, same rules as elin_eval."),
				timeout_ms: timeoutArg(30_000),
				interval_ms: z.number().int().min(100).max(10_000).default(500),
			},
			annotations: ACTS,
		},
		guarded(async ({ instance, code, timeout_ms, interval_ms }, ctx) => {
			const i = await game.resolve(instance);
			const start = Date.now();
			let last: unknown;
			while (Date.now() - start < timeout_ms && !ctx.mcpReq.signal.aborted) {
				try {
					last = await game.request(i.port, "eval", { code }, Math.max(1_000, timeout_ms - (Date.now() - start)));
				} catch (e) {
					if (!(e instanceof game.GameError) || e.message.startsWith("EScriptCompilationException")) throw e;
					last = `(exception) ${e.message}`;
					await sleep(interval_ms);
					continue;
				}
				if (truthy(last)) return text(`true after ${Date.now() - start} ms: ${show(last)}`);
				await sleep(interval_ms);
			}
			return error(`${ctx.mcpReq.signal.aborted ? "cancelled" : "timed out"} after ${Date.now() - start} ms, last value: ${show(last)}`);
		}),
	);

	const levelArg = z.enum(logs.LEVELS).default("Debug").describe("Minimum level (BepInEx order: Debug < Info < Message < Warning < Error < Fatal).");

	server.registerTool(
		"elin_logs",
		{
			description:
				"Read the game's log from one instance's memory: Unity's Debug.Log and every BepInEx logger, the last 5000 events plus " +
				"everything from before this mod loaded, so sandboxed instances and a frozen main thread both work. Lines are " +
				"`HH:mm:ss.fff LVL [source] message` in local time. Lines from before this mod loaded come from Unity's Player.log: " +
				"they show `startup` instead of a time, are skipped by `since`, and Unity lines among them have no real level, so search " +
				"them with `contains`. Why a mod did not load: contains \"Failed to activate mod\", \"Skipped (\", \"Blocked:\", " +
				"\"Not a package:\" or \"does not parse\"; \"Activating(\" only means activation was attempted. group=true counts " +
				"events per (level, source, first line with numbers folded) to spot spam and repeated errors.",
			inputSchema: {
				instance: instanceArg,
				level: levelArg,
				source: z.string().optional().describe("Substring of the log source (plugin name, \"Unity Log\"…), case-insensitive."),
				contains: z.string().optional().describe("Substring of the message, case-insensitive."),
				since: z.string().optional().describe("Only events after this: 10m, 2h, 90s, or a local date or timestamp (2026-09-27T15:54)."),
				last: z.number().int().min(1).max(6000).default(100).describe("Return only the last N matches (or top N groups)."),
				group: z.boolean().default(false),
			},
			annotations: READ_ONLY,
		},
		guarded(async ({ instance, level, source, contains, since, last, group }) => {
			const i = await game.resolve(instance);
			const page = await logs.read(i.port);
			const filter = { minLevel: level, source, contains, sinceMs: logs.parseSince(since) };
			const hits = page.entries.filter((e) => logs.matches(e, filter));
			if (hits.length === 0) return text(`no matching events (buffer holds ${page.entries.length})`);
			if (group) {
				const counts = new Map<string, number>();
				for (const e of hits) counts.set(logs.groupKey(e), (counts.get(logs.groupKey(e)) ?? 0) + 1);
				const rows = [...counts.entries()].sort((a, b) => b[1] - a[1]).slice(0, last);
				return text(`${hits.length} events, ${counts.size} groups\n` + rows.map(([k, n]) => `${String(n).padStart(6)}  ${k}`).join("\n"));
			}
			const tail = hits.slice(-last);
			const head = hits.length > last ? `… ${hits.length - last} earlier matches omitted\n` : "";
			return text(head + tail.map(logs.format).join("\n"), "tail");
		}),
	);

	server.registerTool(
		"elin_wait_log",
		{
			description:
				"Wait until the game logs a line matching a regex, then return it. Matching runs on the formatted line " +
				"(`HH:mm:ss.fff LVL [source] message`), so you can anchor on level or source. lookback_ms also accepts events logged " +
				"shortly before the call (newest match wins), for actions that already happened. Works while the main thread is busy.",
			inputSchema: {
				instance: instanceArg,
				pattern: z.string().min(1).describe("JavaScript regex, case-insensitive."),
				level: levelArg,
				lookback_ms: z.number().int().min(0).max(600_000).default(0),
				timeout_ms: timeoutArg(30_000),
			},
			annotations: READ_ONLY,
		},
		guarded(async ({ instance, pattern, level, lookback_ms, timeout_ms }, ctx) => {
			const re = new RegExp(pattern, "i");
			const hit = (e: logs.Entry) => logs.matches(e, { minLevel: level }) && re.test(logs.format(e));
			const i = await game.resolve(instance);
			const start = Date.now();
			const first = await logs.read(i.port);
			if (lookback_ms > 0) {
				const found = first.entries.filter((e) => e.t !== null && Date.parse(e.t) >= start - lookback_ms).findLast(hit);
				if (found) return text(`matched (lookback): ${logs.format(found)}`);
			}
			let cursor = first.next;
			while (Date.now() - start < timeout_ms && !ctx.mcpReq.signal.aborted) {
				await sleep(300);
				const page = await logs.read(i.port, cursor);
				cursor = page.next;
				const found = page.entries.find(hit);
				if (found) return text(`matched after ${Date.now() - start} ms: ${logs.format(found)}`);
			}
			return error(`${ctx.mcpReq.signal.aborted ? "cancelled" : "no line matched"} /${pattern}/i within ${Date.now() - start} ms`);
		}),
	);

	// ── offline ────────────────────────────────────────────────────────────

	server.registerTool(
		"elin_build",
		{
			description:
				"dotnet build a mod project and return its error/warning lines, or the tail of the output when there are none. " +
				"Defaults come from the ELIN_BUILD_PROJECT and ELIN_BUILD_CONFIGURATION environment variables (set them in the env of " +
				"this server's MCP config). Building while the game runs is fine (the loaded DLL is not locked), but the running game keeps " +
				"the old code: elin_kill + elin_launch to pick the new build up, or elin_hotload to try a change without restarting.",
			inputSchema: {
				project: z.string().optional().describe("Path to a .csproj or .sln, relative to the project root. Default: $ELIN_BUILD_PROJECT."),
				configuration: z.string().optional().describe("Build configuration. Default: $ELIN_BUILD_CONFIGURATION, else Debug."),
			},
			annotations: { readOnlyHint: false, destructiveHint: false, openWorldHint: false },
		},
		guarded(async ({ project, configuration }) => {
			const target = project ?? process.env.ELIN_BUILD_PROJECT;
			if (!target) {
				throw new Error("no project: pass `project`, or set ELIN_BUILD_PROJECT in this server's env (e.g. MyMod/MyMod.csproj)");
			}
			return text(await proc.build(target, configuration ?? process.env.ELIN_BUILD_CONFIGURATION ?? "Debug"));
		}),
	);

	server.registerTool(
		"elin_launch",
		{
			description:
				"Launch Elin through Steam, or inside a Sandboxie-Plus box (sandbox=<box name>) to run a second copy side by side. " +
				"Waits until the new instance's listener answers and returns its instance line.",
			inputSchema: {
				sandbox: z.string().optional().describe("Sandboxie-Plus box name. Needs steam.exe (SteamPath or the registry) and Start.exe (SandboxPath)."),
				wait_ms: z.number().int().min(0).max(300_000).default(120_000).describe("0 = return immediately."),
			},
			annotations: { readOnlyHint: false, destructiveHint: false, openWorldHint: false },
		},
		guarded(async ({ sandbox, wait_ms }, ctx) => {
			const before = new Set((await game.probe()).map((i) => i.port));
			const msg = await proc.launch(sandbox);
			if (wait_ms === 0) return text(msg);
			const start = Date.now();
			while (Date.now() - start < wait_ms && !ctx.mcpReq.signal.aborted) {
				const fresh = (await game.probe()).find((i) => !before.has(i.port) && i.hello);
				if (fresh) return text(`${msg}\nready after ${Math.round((Date.now() - start) / 1000)} s: ${game.describe(fresh)}`);
				await sleep(2_000);
			}
			return error(`${msg}\nno new instance answered within ${Math.round((Date.now() - start) / 1000)} s`);
		}),
	);

	server.registerTool(
		"elin_kill",
		{
			description:
				"Terminate game instances: all (taskkill Elin.exe, also catches instances without a listener), or one selected by port, " +
				"pid or hello value. Unsaved progress is lost.",
			inputSchema: {
				instance: z.string().default("all").describe("all, a port, a pid, or a hello value."),
			},
			annotations: ACTS,
		},
		guarded(async ({ instance }) => {
			if (instance.trim().toLowerCase() === "all") return text(await proc.killAll());
			const i = await game.resolve(instance);
			const pid = i.pid ?? (await game.ping(i.port));
			if (pid === null || pid === undefined) throw new Error(`nothing on port ${i.port} reported a pid; check elin_instances`);
			return text(proc.killPid(pid));
		}),
	);

	return server;
}

/** MCP client configuration for this bridge (and the companion knowledge server) in each common client's format. */
function configText(): string {
	const self = fileURLToPath(import.meta.url).replace(/\\/g, "/");
	const json = (o: unknown) => JSON.stringify(o, null, 2);
	return [
		`Elin Runtime MCP bridge: ${self}`,
		`Needs Node >= 20 and the game running with the Elin Runtime MCP mod. "elin" is the optional companion knowledge server (game source index and modding wiki).`,
		"",
		"## Claude Code  (--scope user: one install serves every mod project)",
		`claude mcp add --scope user elin-runtime -- node "${self}"`,
		`claude mcp add --scope user --transport http elin ${KNOWLEDGE_URL}`,
		"",
		"## Claude Code  .mcp.json  /  Cursor  .cursor/mcp.json",
		json({ mcpServers: { "elin-runtime": { command: "node", args: [self] }, elin: { type: "http", url: KNOWLEDGE_URL } } }),
		"",
		"## Gemini CLI  .gemini/settings.json",
		json({ mcpServers: { "elin-runtime": { command: "node", args: [self] }, elin: { httpUrl: KNOWLEDGE_URL } } }),
		"",
		"## VS Code / GitHub Copilot  .vscode/mcp.json",
		json({ servers: { "elin-runtime": { type: "stdio", command: "node", args: [self] }, elin: { type: "http", url: KNOWLEDGE_URL } } }),
		"",
		"## OpenCode  opencode.json",
		json({ mcp: { "elin-runtime": { type: "local", command: ["node", self], enabled: true }, elin: { type: "remote", url: KNOWLEDGE_URL, enabled: true } } }),
		"",
		"## Codex  ~/.codex/config.toml",
		`[mcp_servers.elin-runtime]\ncommand = "node"\nargs = [${JSON.stringify(self)}]\n\n[mcp_servers.elin]\nurl = "${KNOWLEDGE_URL}"`,
		"",
		"## Any other client",
		`elin-runtime is a stdio server: command "node", args ["${self}"]. elin is a Streamable HTTP server at ${KNOWLEDGE_URL} ` +
			"(the field is called url, httpUrl or serverUrl depending on the client).",
		"",
		"Optional env for elin_build defaults: ELIN_BUILD_PROJECT (path to your .csproj, relative to the project root), ELIN_BUILD_CONFIGURATION.",
		`A client that only speaks stdio can reach the knowledge server through mcp-remote: command "npx", args ["-y", "mcp-remote", "${KNOWLEDGE_URL}"] ` +
			`(on Windows: command "cmd", args ["/c", "npx", "-y", "mcp-remote", "${KNOWLEDGE_URL}"]).`,
		`Setup guide for agents, covering both servers and both agent skills: ${KNOWLEDGE_URL}/llms.txt`,
	].join("\n");
}

const argv = process.argv.slice(2);
if (argv.includes("--version")) {
	console.log(VERSION);
} else if (argv.includes("--config")) {
	console.log(configText());
} else if (argv.includes("--help") || argv.includes("-h") || process.stdin.isTTY) {
	// a person or an agent ran this in a terminal: explain instead of silently waiting for JSON-RPC on stdin
	console.log(
		`Elin Runtime MCP bridge ${VERSION}. This is a stdio MCP server: your MCP client starts it, you do not run it by hand.\n` +
			"  --config   print the MCP client configuration for this install (Claude Code, Cursor, VS Code, OpenCode, Codex...)\n" +
			"  --version  print the version\n" +
			"  --help     this text\n\n" +
			INSTRUCTIONS,
	);
} else {
	serveStdio(createServer);
}
