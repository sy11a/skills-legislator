// legislator-guard.ts — opencode enforcement port of the legislator-hooks plugin.
//
// Ports the three Claude Code hooks shipped at plugin/hooks/*.py into opencode's
// plugin event model. Active globally; silently no-op outside legislated repos
// (a docs/ai/manifest.json found by walking up from the relevant path / at the
// git worktree toplevel). Mirrors the Python hooks' contract: a bug in this
// plugin must never block the user's work — every non-intentional throw is
// swallowed.
//
// Event mapping:
//   guard_owned_files.py  (PreToolUse)  ->  tool.execute.before
//        blocks edit/write/patch of docs/ai/rules/** in a legislated repo
//        by throwing (opencode surfaces the message to the agent).
//   format_on_edit.py     (PostToolUse) ->  tool.execute.after
//        best-effort dotnet-format / prettier on the just-edited file; never blocks.
//   okf_sync_check.py     (Stop)        ->  event "session.idle"
//        warns when src/** changed but docs/okf/** didn't.
//
// Accepted limitation vs the Claude port: opencode's event model has no
// "Stop with feedback fed back to the model" equivalent, so the OKF reminder
// is logged (client.app.log warn) rather than force-feeding the agent. The
// write-guard (the load-bearing hook) is a true block in both tools.
//
// See plugin/README.md and docs/superpowers/specs/2026-07-09-hooks-plugin-design.md.

import { existsSync, realpathSync, readdirSync, readFileSync } from "node:fs";
import * as path from "node:path";

const BLOCK_MESSAGE =
  "docs/ai/rules/** is machine-managed law — edit the legislator skill " +
  "source and re-run /legislator instead.";

const OKF_REMINDER =
  "src/ changed but docs/okf/ didn't — update the OKF (map/log/glossary) " +
  "or state why no update is needed.";

const EDIT_TOOLS = new Set(["edit", "write", "patch"]);

const PRETTIER_EXTS = new Set([".ts", ".tsx", ".js", ".jsx", ".html", ".css"]);
const PRETTIER_CONFIG_NAMES = [
  ".prettierrc", ".prettierrc.json", ".prettierrc.yml", ".prettierrc.yaml",
  ".prettierrc.js", ".prettierrc.cjs", "prettier.config.js", "prettier.config.cjs",
];

/** Defensive extraction of a file path from an opencode tool args object. */
function filePathFromArgs(args: unknown): string | null {
  if (!args || typeof args !== "object") return null;
  const a = args as Record<string, unknown>;
  const raw =
    (typeof a.filePath === "string" && a.filePath) ||
    (typeof a.path === "string" && a.path) ||
    (typeof a.file_path === "string" && a.file_path) ||
    (typeof a.notebookPath === "string" && a.notebookPath) ||
    (typeof a.notebook_path === "string" && a.notebook_path) ||
    null;
  return raw && raw.length > 0 ? raw : null;
}

/** Resolve a path to absolute, against `base` if relative. Symlinks on the
 * existing portion are resolved (Python Path.resolve() semantics); a missing
 * file falls back to a lexical resolve so brand-new files are still guarded. */
function resolveAbs(filePath: string, base: string): string {
  const abs = path.isAbsolute(filePath) ? filePath : path.resolve(base, filePath);
  try {
    return realpathSync(abs);
  } catch {
    return path.resolve(abs);
  }
}

/** Walk up from `start` (inclusive) looking for docs/ai/manifest.json. */
function findRepoRoot(start: string): string | null {
  try {
    let dir = path.resolve(start);
    for (;;) {
      if (existsSync(path.join(dir, "docs", "ai", "manifest.json"))) return dir;
      const parent = path.dirname(dir);
      if (parent === dir) return null;
      dir = parent;
    }
  } catch {
    return null;
  }
}

/** True if `filePath` is inside `base/` (lexical, after resolve). */
function isUnder(filePath: string, base: string): boolean {
  try {
    const rel = path.relative(base, filePath);
    return rel === "" || (!rel.startsWith("..") && !path.isAbsolute(rel));
  } catch {
    return false;
  }
}

/** First ancestor (inclusive) of `filePath` containing any of `names`, or null. */
function findUpwardFile(filePath: string, names: string[]): string | null {
  try {
    let dir = path.dirname(path.resolve(filePath));
    for (;;) {
      for (const n of names) {
        const c = path.join(dir, n);
        if (existsSync(c)) return c;
      }
      const parent = path.dirname(dir);
      if (parent === dir) return null;
      dir = parent;
    }
  } catch {
    return null;
  }
}

/** First ancestor containing a .sln/.csproj (mirrors the Python glob). */
function findDotnetProject(filePath: string): string | null {
  try {
    let dir = path.dirname(path.resolve(filePath));
    for (;;) {
      const hit = readdirSync(dir, { withFileTypes: true })
        .filter((e) => e.isFile() && (e.name.endsWith(".sln") || e.name.endsWith(".csproj")))
        .map((e) => path.join(dir, e.name));
      if (hit.length > 0) return hit[0];
      const parent = path.dirname(dir);
      if (parent === dir) return null;
      dir = parent;
    }
  } catch {
    return null;
  }
}

/** First ancestor with a prettier config (file or package.json "prettier" key). */
function findPrettierConfig(filePath: string): string | null {
  try {
    let dir = path.dirname(path.resolve(filePath));
    for (;;) {
      for (const n of PRETTIER_CONFIG_NAMES) {
        const c = path.join(dir, n);
        if (existsSync(c)) return c;
      }
      const pkg = path.join(dir, "package.json");
      if (existsSync(pkg)) {
        try {
          const data = JSON.parse(readFileSync(pkg, "utf8"));
          if (data && typeof data === "object" && "prettier" in data) return pkg;
        } catch {
          /* ignore malformed package.json */
        }
      }
      const parent = path.dirname(dir);
      if (parent === dir) return null;
      dir = parent;
    }
  } catch {
    return null;
  }
}

export const LegislatorGuard = async (ctx: {
  directory: string;
  worktree?: string;
  client?: { app?: { log?: (args: unknown) => Promise<unknown> | unknown } };
  $?: unknown;
}) => {
  const base = ctx.directory || process.cwd();

  return {
    // 1. Write-guard (PreToolUse equivalent). Blocks via throw.
    "tool.execute.before": async (input: { tool?: string }, output: { args?: unknown }) => {
      try {
        if (!input || !EDIT_TOOLS.has((input.tool ?? "") as string)) return;
        const raw = filePathFromArgs(output?.args);
        if (!raw) return;
        const filePath = resolveAbs(raw, base);
        const repoRoot = findRepoRoot(path.dirname(filePath));
        if (!repoRoot) return;
        const inRules = isUnder(filePath, path.join(repoRoot, "docs", "ai", "rules"));
        const isOwnedRootConfig = path.relative(repoRoot, filePath) === "opencode.json";
        if (inRules || isOwnedRootConfig) {
          throw new Error(BLOCK_MESSAGE);
        }
      } catch (e) {
        // Re-throw only the intentional block; swallow any plugin bug so we
        // never block the user's work for the wrong reason.
        if (e instanceof Error && e.message === BLOCK_MESSAGE) throw e;
      }
    },

    // 2. Format-on-edit (PostToolUse equivalent). Best-effort, never blocks.
    "tool.execute.after": async (input: { tool?: string }, output: { args?: unknown }) => {
      try {
        if (!input || !EDIT_TOOLS.has((input.tool ?? "") as string)) return;
        const raw = filePathFromArgs(output?.args);
        if (!raw) return;
        const filePath = resolveAbs(raw, base);
        const ext = path.extname(filePath).toLowerCase();
        const $ = ctx.$ as
          | ((parts: TemplateStringsArray, ...rest: unknown[]) => Promise<unknown>)
          | undefined;

        if (ext === ".cs") {
          const project = findDotnetProject(filePath);
          if (project && $) {
            try {
              await $`dotnet format ${project} --include ${filePath}`;
            } catch {
              /* missing toolchain or formatter error — swallow */
            }
          }
          return;
        }

        if (PRETTIER_EXTS.has(ext)) {
          const config = findPrettierConfig(filePath);
          if (config && $) {
            try {
              await $`npx prettier --write ${filePath}`;
            } catch {
              /* swallow */
            }
          }
        }
      } catch {
        /* never block */
      }
    },

    // 3. OKF-sync reminder (Stop equivalent -> session.idle). Logged, not
    //    force-fed (opencode has no Stop-feedback loop).
    event: async (payload: { event?: { type?: string } }) => {
      try {
        if (!payload || payload.event?.type !== "session.idle") return;
        const $ = ctx.$ as
          | ((parts: TemplateStringsArray, ...rest: unknown[]) => Promise<unknown>)
          | undefined;
        // Determine the git worktree toplevel; prefer worktree, fall back to base.
        const cwd = ctx.worktree || base;
        if (!existsSync(path.join(cwd, "docs", "ai", "manifest.json"))) return;
        if (!$) return;
        let out = "";
        try {
          const res = $`git -C ${cwd} status --porcelain` as unknown as {
            stdout?: string;
          } & Promise<unknown>;
          const got = await (res as Promise<unknown>);
          // Bun's $ returns a ShellString (string-like) when awaited in many
          // contexts; handle both string and {stdout} shapes.
          out = typeof got === "string"
            ? got
            : typeof (got as { stdout?: string })?.stdout === "string"
              ? (got as { stdout: string }).stdout
              : "";
        } catch {
          return;
        }
        const paths = out.split("\n")
          .map((l) => (l.length < 4 ? "" : l.slice(3).replace(/^"|"$/g, "")))
          .filter(Boolean);
        const srcChanged = paths.some((p) => p === "src" || p.startsWith("src/"));
        const okfChanged = paths.some((p) => p === "docs/okf" || p.startsWith("docs/okf/"));
        if (srcChanged && !okfChanged) {
          const log = ctx.client?.app?.log;
          if (log) {
            try {
              await log({
                body: { service: "legislator-guard", level: "warn", message: OKF_REMINDER },
              });
            } catch {
              /* swallow */
            }
          }
        }
      } catch {
        /* never block */
      }
    },
  };
};
