/**
 * The in-game log buffer (RmcpLogBuffer.cs): what BepInEx's LogOutput.log would show, per instance.
 * Read through the "log" op, which the game answers off the main thread.
 */
import * as game from "./game.ts";

/** BepInEx LogLevel names, least to most severe. */
export const LEVELS = ["Debug", "Info", "Message", "Warning", "Error", "Fatal"] as const;
export type Level = (typeof LEVELS)[number];

/** t is local time, or null for entries copied from the disk log at startup (it has no timestamps). */
export type Entry = { seq: number; t: string | null; level: string; source: string; msg: string };
export type Page = { next: number; first: number; entries: Entry[] };

export function read(port: number, after = 0): Promise<Page> {
	return game.request(port, "log", { after }, 10_000) as Promise<Page>;
}

export type Filter = { minLevel?: Level; source?: string; contains?: string; sinceMs?: number };

const rank = (level: string) => {
	const i = LEVELS.indexOf(level as Level);
	return i < 0 ? LEVELS.indexOf("Info") : i;
};

export function matches(e: Entry, f: Filter): boolean {
	if (f.minLevel && rank(e.level) < LEVELS.indexOf(f.minLevel)) return false;
	if (f.source && !e.source.toLowerCase().includes(f.source.toLowerCase())) return false;
	if (f.contains && !e.msg.toLowerCase().includes(f.contains.toLowerCase())) return false;
	if (f.sinceMs !== undefined && (e.t === null || Date.parse(e.t) < f.sinceMs)) return false;
	return true;
}

/** `HH:mm:ss.fff LVL [source] message`, local time (`startup` for backfilled entries), continuation lines indented. */
export function format(e: Entry): string {
	const lvl = e.level.slice(0, 3).toUpperCase();
	const time = e.t === null ? "startup     " : e.t.slice(11);
	return `${time} ${lvl} [${e.source}] ${e.msg.replace(/\r?\n/g, "\n    ")}`;
}

/** Grouping key: numbers folded so "loaded 12 items" and "loaded 7 items" count together. */
export function groupKey(e: Entry): string {
	const first = e.msg.split("\n", 1)[0]!.replace(/\d+/g, "#").slice(0, 160);
	return `${e.level.slice(0, 3).toUpperCase()} [${e.source}] ${first}`;
}

/** "10m", "2h", "90s" relative to now, or a local timestamp (2026-09-27T15:54). */
export function parseSince(s: string | undefined): number | undefined {
	if (!s) return undefined;
	const rel = /^(\d+)\s*(s|m|h|d)$/i.exec(s.trim());
	if (rel) {
		const unit = { s: 1_000, m: 60_000, h: 3_600_000, d: 86_400_000 }[rel[2]!.toLowerCase()]!;
		return Date.now() - Number(rel[1]) * unit;
	}
	// a bare date parses as UTC midnight in JavaScript; the log times are local
	const abs = Date.parse(/^\d{4}-\d{2}-\d{2}$/.test(s.trim()) ? `${s.trim()}T00:00` : s);
	if (Number.isNaN(abs)) throw new Error(`cannot parse since='${s}', use 10m / 2h / a local timestamp`);
	return abs;
}
