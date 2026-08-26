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

import { existsSync, realpathSync, readdirSync, readFileSync, statSync } from "node:fs";
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


// --- Git-conduct guard (BL-064) -------------------------------------------
// Port of plugin/hooks/guard_git_conduct.py. Same fail-open contract; git
// state is read from the filesystem (.git/HEAD, refs, packed-refs) so the
// guard needs no shell and the mjs harness can drive it without git.

const MERGE_MSG =
  "merging into the default branch is the user's act — push the task " +
  "branch and leave merging to them (core/pair-development.md).";
const PUSH_MSG =
  "pushing the default branch is the user's act — push the task branch " +
  "and leave integration to them (core/pair-development.md).";
const PR_MERGE_MSG =
  "merging the PR is the user's act, whatever the channel — leave it to " +
  "them (core/pair-development.md).";
const ATTRIBUTION_MSG =
  "AI attribution in the VCS record is forbidden — drop the " +
  "Co-Authored-By trailer / Generated-with footer " +
  "(core/pair-development.md).";
const CONDUCT_MESSAGES = new Set([MERGE_MSG, PUSH_MSG, PR_MERGE_MSG, ATTRIBUTION_MSG]);

const ATTRIBUTION_PATTERNS = [
  /co-authored-by\s*:[^\n]{0,120}\b(claude|anthropic)\b/i,
  /generated\s+with\b[\s\S]{0,80}\b(claude|anthropic)\b/i,
];

const SEGMENT_BREAKS = new Set(["&&", "||", ";", "|", "&"]);
const GIT_OPTS_WITH_ARG = new Set(["-C", "-c", "--exec-path", "--git-dir", "--work-tree", "--namespace"]);
const PUSH_OPTS_WITH_ARG = new Set(["-o", "--push-option", "--receive-pack", "--exec", "--repo"]);

/** Minimal shell-word splitter: quotes and backslashes respected, the
 * control operators & | ; emitted as their own tokens. Null on unbalanced
 * quotes (undecidable — the caller allows). */
function shellTokens(command: string): string[] | null {
  const tokens: string[] = [];
  let cur = "";
  let hasCur = false;
  const push = () => { if (hasCur) { tokens.push(cur); cur = ""; hasCur = false; } };
  let i = 0;
  while (i < command.length) {
    const ch = command[i];
    if (ch === "'") {
      const end = command.indexOf("'", i + 1);
      if (end === -1) return null;
      cur += command.slice(i + 1, end); hasCur = true; i = end + 1; continue;
    }
    if (ch === '"') {
      let j = i + 1; let buf = "";
      while (j < command.length && command[j] !== '"') {
        if (command[j] === "\\" && j + 1 < command.length && '"\\$`'.includes(command[j + 1])) {
          buf += command[j + 1]; j += 2;
        } else { buf += command[j]; j++; }
      }
      if (j >= command.length) return null;
      cur += buf; hasCur = true; i = j + 1; continue;
    }
    if (ch === "\\") {
      if (i + 1 < command.length) { cur += command[i + 1]; hasCur = true; i += 2; } else { i++; }
      continue;
    }
    if (ch === " " || ch === "\t" || ch === "\n") { push(); i++; continue; }
    if (ch === "&" || ch === "|" || ch === ";") {
      push();
      let op = ch;
      if (command[i + 1] === ch) { op += ch; i++; }
      tokens.push(op); i++; continue;
    }
    cur += ch; hasCur = true; i++;
  }
  push();
  return tokens;
}

function conductSegments(command: string): string[][] {
  const tokens = shellTokens(command);
  if (!tokens) return [];
  const out: string[][] = [];
  let cur: string[] = [];
  for (const tok of tokens) {
    if (SEGMENT_BREAKS.has(tok)) { if (cur.length) out.push(cur); cur = []; }
    else cur.push(tok);
  }
  if (cur.length) out.push(cur);
  return out;
}

function stripEnvPrefix(seg: string[]): string[] {
  let i = 0;
  while (i < seg.length && /^[A-Za-z_][A-Za-z0-9_]*=/.test(seg[i])) i++;
  return seg.slice(i);
}

/** Walk up from startDir for .git; a .git *file* (worktree) is followed. */
function findGitDir(startDir: string): string | null {
  try {
    let dir = path.resolve(startDir);
    for (;;) {
      const g = path.join(dir, ".git");
      if (existsSync(g)) {
        const st = statSync(g);
        if (st.isDirectory()) return g;
        const m = readFileSync(g, "utf8").match(/^gitdir:\s*(.+)\s*$/m);
        return m ? path.resolve(dir, m[1].trim()) : null;
      }
      const parent = path.dirname(dir);
      if (parent === dir) return null;
      dir = parent;
    }
  } catch {
    return null;
  }
}

function refLine(gitdir: string, rel: string): string | null {
  try { return readFileSync(path.join(gitdir, rel), "utf8").trim(); } catch { return null; }
}

function currentBranchFs(gitdir: string): string | null {
  const m = refLine(gitdir, "HEAD")?.match(/^ref: refs\/heads\/(.+)$/);
  return m ? m[1] : null;
}

function localBranchesFs(gitdir: string): Set<string> {
  const names = new Set<string>();
  const base = path.join(gitdir, "refs", "heads");
  const walk = (dir: string, prefix: string) => {
    for (const e of readdirSync(dir, { withFileTypes: true })) {
      if (e.isDirectory()) walk(path.join(dir, e.name), prefix + e.name + "/");
      else names.add(prefix + e.name);
    }
  };
  try { walk(base, ""); } catch { /* no loose refs */ }
  const packed = refLine(gitdir, "packed-refs");
  if (packed) {
    for (const line of packed.split("\n")) {
      const m = line.match(/^[0-9a-f]+\s+refs\/heads\/(.+)$/);
      if (m) names.add(m[1]);
    }
  }
  return names;
}

function defaultBranchFs(gitdir: string): string | null {
  const m = refLine(gitdir, path.join("refs", "remotes", "origin", "HEAD"))
    ?.match(/^ref: refs\/remotes\/origin\/(.+)$/);
  if (m) return m[1];
  const names = localBranchesFs(gitdir);
  const cands = ["main", "master"].filter((b) => names.has(b));
  return cands.length === 1 ? cands[0] : null;
}

function gitSubcommand(seg: string[]): { sub: string | null; args: string[]; cPath: string | null } {
  let cPath: string | null = null;
  let i = 1;
  while (i < seg.length) {
    const tok = seg[i];
    if (GIT_OPTS_WITH_ARG.has(tok)) {
      if (tok === "-C" && i + 1 < seg.length) cPath = seg[i + 1];
      i += 2; continue;
    }
    if (tok.startsWith("-")) { i++; continue; }
    return { sub: tok, args: seg.slice(i + 1), cPath };
  }
  return { sub: null, args: [], cPath };
}

function pushTargetsDefault(args: string[], def: string, current: string | null): boolean {
  const positional: string[] = [];
  let forceAll = false;
  let i = 0;
  while (i < args.length) {
    const tok = args[i];
    if (PUSH_OPTS_WITH_ARG.has(tok)) { i += 2; continue; }
    if (tok === "--all" || tok === "--branches" || tok === "--mirror") { forceAll = true; i++; continue; }
    if (tok.startsWith("-")) { i++; continue; }
    positional.push(tok); i++;
  }
  if (forceAll) return true;
  const refspecs = positional.slice(1);
  if (refspecs.length === 0) return current === def;
  for (let rs of refspecs) {
    rs = rs.replace(/^\+/, "");
    const dst = rs.includes(":") ? rs.slice(rs.indexOf(":") + 1) : rs;
    if (dst.replace(/^refs\/heads\//, "") === def) return true;
  }
  return false;
}

function hasAttribution(text: string): boolean {
  return ATTRIBUTION_PATTERNS.some((p) => p.test(text));
}

/** Block message for a git/gh conduct violation, or null to allow. */
function judgeGitConduct(command: string, cwd: string): string | null {
  for (const rawSeg of conductSegments(command)) {
    const seg = stripEnvPrefix(rawSeg);
    if (!seg.length) continue;
    const head = seg[0].split("/").pop();

    if (head === "git") {
      const { sub, args, cPath } = gitSubcommand(seg);
      const dir = cPath ? path.resolve(cwd, cPath) : cwd;
      if (sub === "merge") {
        if (args.some((a) => a === "--abort" || a === "--quit")) continue;
        const gitdir = findGitDir(dir);
        if (!gitdir) continue;
        const cur = currentBranchFs(gitdir);
        const def = defaultBranchFs(gitdir);
        if (cur !== null && def !== null && cur === def) return MERGE_MSG;
      } else if (sub === "push") {
        const gitdir = findGitDir(dir);
        if (!gitdir) continue;
        const def = defaultBranchFs(gitdir);
        if (def !== null && pushTargetsDefault(args, def, currentBranchFs(gitdir))) return PUSH_MSG;
      } else if (sub === "commit") {
        if (hasAttribution(seg.join(" "))) return ATTRIBUTION_MSG;
      }
    } else if (head === "gh" && seg.length >= 3 && seg[1] === "pr") {
      if (seg[2] === "merge") return PR_MERGE_MSG;
      if ((seg[2] === "create" || seg[2] === "edit") && hasAttribution(seg.join(" "))) return ATTRIBUTION_MSG;
    }
  }
  return null;
}

function commandFromArgs(args: unknown): string | null {
  if (!args || typeof args !== "object") return null;
  const a = args as Record<string, unknown>;
  const raw =
    (typeof a.command === "string" && a.command) ||
    (typeof a.cmd === "string" && a.cmd) ||
    null;
  return raw && raw.length > 0 ? raw : null;
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
        if (!input) return;
        if ((input.tool ?? "") === "bash") {
          const command = commandFromArgs(output?.args);
          if (!command) return;
          if (!command.includes("git") && !command.includes("gh")) return;
          if (!findRepoRoot(base)) return;
          const msg = judgeGitConduct(command, base);
          if (msg) throw new Error(msg);
          return;
        }
        if (!EDIT_TOOLS.has((input.tool ?? "") as string)) return;
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
        // Re-throw only the intentional blocks; swallow any plugin bug so we
        // never block the user's work for the wrong reason.
        if (e instanceof Error &&
            (e.message === BLOCK_MESSAGE || CONDUCT_MESSAGES.has(e.message))) throw e;
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
