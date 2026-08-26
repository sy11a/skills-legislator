// Deterministic unit tests for the opencode legislator-guard plugin (the TS
// analog of evals/check_hooks.py which covers the .py hooks). No agent, no
// opencode runtime — instantiates the plugin with a mock context and drives
// the returned hooks directly. Run: node evals/check_opencode_plugin.mjs
import assert from "node:assert/strict";
import { existsSync, mkdtempSync, mkdirSync, writeFileSync, rmSync, symlinkSync } from "node:fs";
import { tmpdir } from "node:os";
import * as path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const mod = await import(path.resolve(here, "../plugin/opencode/legislator-guard.ts"));
const makePlugin = mod.LegislatorGuard;

const BLOCK =
  "docs/ai/rules/** is machine-managed law — edit the legislator skill " +
  "source and re-run /legislator instead.";

let passed = 0;
const results = [];
function ok(name, cond, evidence = "") {
  results.push({ name, passed: !!cond, evidence });
  if (cond) passed++;
}

// Build an isolated legislated fake repo in /tmp.
const repo = mkdtempSync(path.join(tmpdir(), "leg-guard-"));
mkdirSync(path.join(repo, "docs/ai/rules/core"), { recursive: true });
mkdirSync(path.join(repo, "docs/ai/rules/stacks/dotnet"), { recursive: true });
mkdirSync(path.join(repo, ".claude/rules"), { recursive: true });
mkdirSync(path.join(repo, "src"), { recursive: true });
writeFileSync(path.join(repo, "docs/ai/manifest.json"), '{"legislatorVersion":13}');
writeFileSync(path.join(repo, "docs/ai/rules/core/okf.md"), "## OKF\n");
writeFileSync(path.join(repo, ".claude/rules/local.md"), "## local\n");
writeFileSync(path.join(repo, "src/Program.cs"), "// hi\n");
writeFileSync(path.join(repo, "opencode.json"), '{"instructions":[]}');
writeFileSync(path.join(repo, "package.json"), '{"name":"x"}');

// A non-legislated dir for the no-op cases.
const free = mkdtempSync(path.join(tmpdir(), "leg-guard-free-"));
mkdirSync(path.join(free, "docs/ai/rules/core"), { recursive: true });
writeFileSync(path.join(free, "docs/ai/rules/core/okf.md"), "## OKF\n");

const hooks = await makePlugin({ directory: repo });
const before = hooks["tool.execute.before"];

// 1. edit an owned rule -> blocked
try {
  await before({ tool: "edit" }, { args: { filePath: path.join(repo, "docs/ai/rules/core/okf.md") } });
  ok("edit_owned_blocked", false, "no throw");
} catch (e) {
  ok("edit_owned_blocked", e.message === BLOCK, `threw: ${e.message}`);
}

// 2. write a NEW file under docs/ai/rules/** -> blocked (new file, realpath fallback)
try {
  await before({ tool: "write" }, { args: { filePath: path.join(repo, "docs/ai/rules/core/new-rule.md") } });
  ok("write_new_owned_blocked", false, "no throw");
} catch (e) {
  ok("write_new_owned_blocked", e.message === BLOCK, `threw: ${e.message}`);
}

// 3. patch an owned stack rule -> blocked
try {
  await before({ tool: "patch" }, { args: { filePath: path.join(repo, "docs/ai/rules/stacks/dotnet/x.md") } });
  ok("patch_owned_blocked", false, "no throw");
} catch (e) {
  ok("patch_owned_blocked", e.message === BLOCK, `threw: ${e.message}`);
}

// 4. edit a non-owned file in a legislated repo -> allowed
try {
  await before({ tool: "edit" }, { args: { filePath: path.join(repo, "src/Program.cs") } });
  ok("edit_non_owned_allowed", true);
} catch (e) {
  ok("edit_non_owned_allowed", false, `unexpected throw: ${e.message}`);
}

// 5. manifest.json itself is NOT guarded (legislator rewrites it every run)
try {
  await before({ tool: "edit" }, { args: { filePath: path.join(repo, "docs/ai/manifest.json") } });
  ok("manifest_not_guarded", true);
} catch (e) {
  ok("manifest_not_guarded", false, `threw on manifest: ${e.message}`);
}

// 5b. the owned root wiring file opencode.json IS guarded
try {
  await before({ tool: "edit" }, { args: { filePath: path.join(repo, "opencode.json") } });
  ok("opencode_json_blocked", false, "no throw");
} catch (e) {
  ok("opencode_json_blocked", e.message === BLOCK, `threw: ${e.message}`);
}

// 5c. a different root config (package.json) is NOT guarded
try {
  await before({ tool: "edit" }, { args: { filePath: path.join(repo, "package.json") } });
  ok("package_json_allowed", true);
} catch (e) {
  ok("package_json_allowed", false, `threw on package.json: ${e.message}`);
}

// 6. .claude/rules/** (project law) is NOT guarded
try {
  await before({ tool: "edit" }, { args: { filePath: path.join(repo, ".claude/rules/local.md") } });
  ok("project_rules_not_guarded", true);
} catch (e) {
  ok("project_rules_not_guarded", false, `threw on project rule: ${e.message}`);
}

// 7. non-edit tools (read) are ignored even on an owned path
try {
  await before({ tool: "read" }, { args: { filePath: path.join(repo, "docs/ai/rules/core/okf.md") } });
  ok("read_tool_ignored", true);
} catch (e) {
  ok("read_tool_ignored", false, `threw on read: ${e.message}`);
}

// 8. a non-legislated repo is a silent no-op even with rules/** present
const freeHooks = await makePlugin({ directory: free });
try {
  await freeHooks["tool.execute.before"]({ tool: "edit" }, { args: { filePath: path.join(free, "docs/ai/rules/core/okf.md") } });
  ok("non_legislated_noop", true);
} catch (e) {
  ok("non_legislated_noop", false, `threw outside legislated repo: ${e.message}`);
}

// 9. relative path resolved against the plugin directory
try {
  await before({ tool: "edit" }, { args: { filePath: "docs/ai/rules/core/okf.md" } });
  ok("relative_path_blocked", false, "no throw");
} catch (e) {
  ok("relative_path_blocked", e.message === BLOCK, `threw: ${e.message}`);
}

// 10. malformed args (no path) -> no-op, no crash
try {
  await before({ tool: "edit" }, { args: {} });
  ok("malformed_args_noop", true);
} catch (e) {
  ok("malformed_args_noop", false, `crashed: ${e.message}`);
}

// 11. the after-hook (format) and event hook exist and don't throw on benign input
try {
  await hooks["tool.execute.after"]({ tool: "edit" }, { args: { filePath: path.join(repo, "src/Program.cs") } });
  ok("after_hook_safe", true);
} catch (e) {
  ok("after_hook_safe", false, `after threw: ${e.message}`);
}
try {
  await hooks.event({ event: { type: "other" } });
  ok("event_hook_safe", true);
} catch (e) {
  ok("event_hook_safe", false, `event threw: ${e.message}`);
}


// =====================================================================
// Git-conduct guard (BL-064) — bash tool, per R-648
// =====================================================================

function makeFakeGitRepo(root, { legislated = true, head = "master", branches = ["master"] } = {}) {
  mkdirSync(path.join(root, ".git", "refs", "heads"), { recursive: true });
  writeFileSync(path.join(root, ".git", "HEAD"), `ref: refs/heads/${head}\n`);
  for (const b of branches) {
    const ref = path.join(root, ".git", "refs", "heads", b);
    mkdirSync(path.dirname(ref), { recursive: true });
    writeFileSync(ref, "0000000000000000000000000000000000000000\n");
  }
  if (legislated) {
    mkdirSync(path.join(root, "docs", "ai"), { recursive: true });
    writeFileSync(path.join(root, "docs", "ai", "manifest.json"), "{}");
  }
}

async function conduct(hooksObj, command) {
  try {
    await hooksObj["tool.execute.before"]({ tool: "bash" }, { args: { command } });
    return null;
  } catch (e) {
    return e.message ?? String(e);
  }
}

const gitRepo = mkdtempSync(path.join(tmpdir(), "leg-conduct-"));
makeFakeGitRepo(gitRepo, { head: "master", branches: ["master", "bl/064-x"] });
const gitHooks = await makePlugin({ directory: gitRepo });

// per R-641: merging while ON the default branch is blocked.
let msg = await conduct(gitHooks, "git merge bl/064-x");
ok("bash_merge_on_default_blocked", msg !== null && msg.includes("pair-development"),
   `got: ${msg}`);

// per R-641: a compound command hides no merge.
msg = await conduct(gitHooks, "git fetch && git merge origin/master");
ok("bash_compound_merge_blocked", msg !== null, `got: ${msg}`);

// per R-642: pushing the default branch is blocked; a task branch passes.
msg = await conduct(gitHooks, "git push origin master");
ok("bash_push_default_blocked", msg !== null, `got: ${msg}`);
msg = await conduct(gitHooks, "git push -u origin bl/064-x");
ok("bash_push_task_branch_allowed", msg === null, `got: ${msg}`);

// per R-643: attribution in a commit message is blocked; ordinary passes.
msg = await conduct(gitHooks,
  'git commit -m "x" -m "Co-Authored-By: Claude <noreply@anthropic.com>"');
ok("bash_commit_attribution_blocked", msg !== null && msg.includes("attribution"),
   `got: ${msg}`);
msg = await conduct(gitHooks, 'git commit -m "fix: ordinary message"');
ok("bash_commit_ordinary_allowed", msg === null, `got: ${msg}`);

// per R-644: attribution in a PR body is blocked.
msg = await conduct(gitHooks,
  'gh pr create --title "x" --body "Generated with [Claude Code](https://claude.com/claude-code)"');
ok("bash_pr_body_attribution_blocked", msg !== null, `got: ${msg}`);

// per R-645: merging the PR is the user's act.
msg = await conduct(gitHooks, "gh pr merge 23 --squash");
ok("bash_pr_merge_blocked", msg !== null && msg.includes("pair-development"), `got: ${msg}`);

// per R-641: the same merge from a feature branch is allowed.
const featRepo = mkdtempSync(path.join(tmpdir(), "leg-conduct-feat-"));
makeFakeGitRepo(featRepo, { head: "bl/064-x", branches: ["master", "bl/064-x"] });
const featHooks = await makePlugin({ directory: featRepo });
msg = await conduct(featHooks, "git merge master");
ok("bash_merge_on_feature_allowed", msg === null, `got: ${msg}`);

// per R-647: outside a legislated repo the guard is a silent no-op.
const plainRepo = mkdtempSync(path.join(tmpdir(), "leg-conduct-plain-"));
makeFakeGitRepo(plainRepo, { legislated: false });
const plainHooks = await makePlugin({ directory: plainRepo });
msg = await conduct(plainHooks, "git merge anything");
ok("bash_non_legislated_noop", msg === null, `got: ${msg}`);

// per R-646: malformed args and non-git commands never throw.
msg = await conduct(gitHooks, undefined);
ok("bash_missing_command_noop", msg === null, `got: ${msg}`);
msg = await conduct(gitHooks, "ls -la && echo done");
ok("bash_non_git_command_noop", msg === null, `got: ${msg}`);

rmSync(gitRepo, { recursive: true, force: true });
rmSync(featRepo, { recursive: true, force: true });
rmSync(plainRepo, { recursive: true, force: true });

// cleanup
rmSync(repo, { recursive: true, force: true });
rmSync(free, { recursive: true, force: true });

const failed = results.filter((r) => !r.passed);
for (const r of results) {
  console.log(`  ${r.passed ? "ok  " : "FAIL"}  ${r.name}${r.passed ? "" : ` — ${r.evidence}`}`);
}
if (failed.length) {
  console.log(`\n${failed.length} check(s) FAILED`);
  process.exit(1);
}
console.log(`\nall ${results.length} opencode-plugin checks passed`);
