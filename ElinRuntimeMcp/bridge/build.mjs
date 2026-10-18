// Bundles src/ with its dependencies into one ESM file that runs on plain Node >= 20, shipped inside the mod package,
// plus the license texts of every package that ended up in it (the bundle carries their code, so it carries their notices).
import * as fs from "node:fs";
import * as path from "node:path";
import { build } from "esbuild";

const OUT_DIR = "../package/bridge";

const result = await build({
	entryPoints: ["src/index.ts"],
	outfile: `${OUT_DIR}/elin-mcp.mjs`,
	bundle: true,
	platform: "node",
	format: "esm",
	target: "node20",
	legalComments: "eof",
	logLevel: "warning",
	metafile: true,
	banner: {
		js:
			"// Elin Runtime MCP bridge. Generated from bridge/src by `node build.mjs`, do not edit.\n" +
			"// Third-party licenses: THIRD-PARTY-NOTICES.txt next to this file.\n" +
			"import { createRequire as __cr } from 'node:module'; const require = __cr(import.meta.url);",
	},
});

/** The nearest package.json above a bundled file, i.e. the package it belongs to. */
function owningPackage(file) {
	for (let dir = path.dirname(path.resolve(file)); dir !== path.dirname(dir); dir = path.dirname(dir)) {
		const manifest = path.join(dir, "package.json");
		if (fs.existsSync(manifest)) {
			const pkg = JSON.parse(fs.readFileSync(manifest, "utf8"));
			if (pkg.name) return { dir, name: pkg.name, version: pkg.version, license: pkg.license };
		}
	}
	return null;
}

const packages = new Map();
for (const input of Object.keys(result.metafile.inputs)) {
	if (!input.includes("node_modules")) continue;
	const pkg = owningPackage(input);
	if (pkg && !packages.has(pkg.name)) packages.set(pkg.name, pkg);
}

const sections = [...packages.values()]
	.sort((a, b) => a.name.localeCompare(b.name))
	.map((pkg) => {
		const licenseFile = fs.readdirSync(pkg.dir).find((f) => /^(licen[cs]e|copying)(\.|$)/i.test(f));
		const body = licenseFile ? fs.readFileSync(path.join(pkg.dir, licenseFile), "utf8").trim() : `License: ${pkg.license ?? "see package"}`;
		return `${pkg.name}@${pkg.version}\n${"-".repeat(60)}\n${body}\n`;
	});

fs.writeFileSync(
	`${OUT_DIR}/THIRD-PARTY-NOTICES.txt`,
	`elin-mcp.mjs bundles the following packages.\n\n${sections.join("\n")}`,
);
