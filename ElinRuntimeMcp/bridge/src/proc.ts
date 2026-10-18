/** Build / launch / kill helpers. Everything here runs outside the game. */
import { spawn } from "node:child_process";
import * as fs from "node:fs";
import * as path from "node:path";

export const STEAM_APP = "2135150";

export function run(cmd: string, args: string[], opts: { cwd?: string; timeoutMs?: number } = {}): Promise<{ code: number | null; out: string }> {
	return new Promise((resolve, reject) => {
		const child = spawn(cmd, args, { cwd: opts.cwd, windowsHide: true, shell: false });
		let out = "";
		child.stdout.on("data", (d: Buffer) => (out += d.toString()));
		child.stderr.on("data", (d: Buffer) => (out += d.toString()));
		const timer = opts.timeoutMs
			? setTimeout(() => {
					child.kill();
					reject(new Error(`'${cmd} ${args.join(" ")}' timed out after ${opts.timeoutMs}ms\n${out}`));
				}, opts.timeoutMs)
			: null;
		child.on("error", (e) => {
			if (timer) clearTimeout(timer);
			reject(e);
		});
		child.on("close", (code) => {
			if (timer) clearTimeout(timer);
			resolve({ code, out });
		});
	});
}

export async function build(project: string, configuration: string): Promise<string> {
	const abs = path.resolve(project);
	if (!fs.existsSync(abs)) throw new Error(`project not found: ${abs}`);
	const { code, out } = await run("dotnet", ["build", abs, "-c", configuration, "--nologo", "-v", "q", "-clp:NoSummary"], {
		timeoutMs: 600_000,
	});
	const lines = out.split(/\r?\n/).map((l) => l.trim()).filter(Boolean);
	const diags = [...new Set(lines.filter((l) => /(error|warning) [A-Z]+\d+/.test(l)))];
	const head = code === 0 ? `build ${path.basename(abs)} ${configuration}: OK` : `build ${path.basename(abs)} ${configuration}: FAILED (exit ${code})`;
	const body = diags.length ? diags.join("\n") : lines.slice(-15).join("\n");
	return `${head}\n${body}`;
}

async function steamExe(): Promise<string | null> {
	if (process.env.SteamPath) {
		const exe = path.join(process.env.SteamPath, "steam.exe");
		if (fs.existsSync(exe)) return exe;
	}
	const { out } = await run("reg", ["query", "HKCU\\Software\\Valve\\Steam", "/v", "SteamExe"], { timeoutMs: 10_000 }).catch(() => ({ out: "" }));
	const m = /SteamExe\s+REG_SZ\s+(.+)/.exec(out);
	return m ? path.normalize(m[1]!.trim()) : null;
}

/** Launch through Steam; with a box name, inside that Sandboxie-Plus sandbox (for a second instance). */
export async function launch(sandbox: string | undefined): Promise<string> {
	const url = `steam://rungameid/${STEAM_APP}`;
	const steam = await steamExe();

	if (!sandbox) {
		if (steam) spawn(steam, [url], { detached: true, stdio: "ignore", windowsHide: true }).unref();
		else spawn("cmd", ["/c", "start", "", url], { detached: true, stdio: "ignore", windowsHide: true }).unref();
		return `launched ${url}`;
	}

	if (!steam) throw new Error("steam.exe not found (set SteamPath), a sandboxed launch needs it");
	const start = path.join(process.env.SandboxPath ?? "C:\\Program Files\\Sandboxie-Plus", "Start.exe");
	if (!fs.existsSync(start)) throw new Error(`Sandboxie Start.exe not found at ${start} (set SandboxPath)`);
	spawn(start, [`/box:${sandbox}`, steam, url], { detached: true, stdio: "ignore", windowsHide: true }).unref();
	return `launched ${url} in Sandboxie box ${sandbox}`;
}

export async function killAll(): Promise<string> {
	// taskkill prints in the OEM code page, so report the exit code instead of echoing it
	const { code } = await run("taskkill", ["/F", "/IM", "Elin.exe"], { timeoutMs: 15_000 });
	return code === 0 ? "terminated every Elin.exe" : `taskkill exit ${code}: no Elin.exe was running`;
}

export function killPid(pid: number): string {
	process.kill(pid);
	return `sent kill to pid ${pid}`;
}
