---
name: finish
description: Use when wrapping up a completed change to this repository and cutting a new release — when docs need to be brought current and the package version bumped before committing, tagging, or letting CI publish. Triggers on "finish", "cut a release", "new version", "bump version", "prepare release".
---

# Finish (release prep)

## Overview

Brings this repo to a releasable state: every documentation file is current and the package
version is bumped according to SemVer. Run it **after** the work is implemented and the test
suite is green — it finalizes, it does not implement.

## When to use

- A feature, fix, or breaking change is complete, committed, and `dotnet test` passes, and you
  want to cut a version.
- Before tagging, or before letting the `main`-branch CI publish to NuGet.

Do **not** run mid-feature, with uncommitted half-work, or with failing/skipped tests.

## Steps

1. **Confirm green.** Run `dotnet test -c Release` over the `.slnx`. The integration tests need a
   running Docker engine; if Docker is unavailable, say so explicitly rather than assuming green.
   Stop here if anything fails.

2. **Decide the version (SemVer).** Look at what changed since the last released version (git log,
   the `## [Unreleased]` changelog section):
   - drops a target framework, or requires a new major of Serilog / a breaking API change → **major**
   - new backward-compatible option or sink behavior → **minor**
   - bug fix or internal-only change → **patch**

   The current version is `<VersionPrefix>` in
   `src/Serilog.Sinks.AzureEventHub/Serilog.Sinks.AzureEventHub.csproj`.

3. **Bump the version.** Set `<VersionPrefix>` in that csproj to the new version. Leave the
   `.nuspec` (`$version$`) and `assets/Serilog.snk` alone — `dotnet pack` builds from the csproj.
   If `<VersionPrefix>` already equals the version marked `Unreleased` in the changelog (the dev
   cycle pre-bumped it), it is already correct — don't bump again, just finalize the date in step 4.

4. **Finalize the changelog.** In `CHANGELOG.md`:
   - rename the `## [x.y.z] - Unreleased` (or `## [Unreleased]`) heading to
     `## [<new-version>] - <YYYY-MM-DD>` (use today's real date);
   - confirm the Added / Changed / Removed bullets actually match the shipped changes;
   - optionally add a fresh empty `## [Unreleased]` section above it for the next cycle.

5. **Sync every doc file with reality.** Re-read and update anything stale:
   - `README.md` — usage examples, the configuration-reference table, any version-specific notes;
   - `CLAUDE.md` — build/test commands, architecture notes, the versioning/CI section.
   New options, renamed commands, new projects, or changed defaults must be reflected.

6. **Commit.** `git add -A && git commit -m "Release <new-version>"`, then push to the fork.

7. **(Optional) Tag.** The `main` CI cuts the `v<version>` GitHub release on publish, so only create
   a tag manually if you are releasing outside that pipeline.

## Common mistakes

- Leaving the changelog entry as "Unreleased" (forgetting the date) after bumping the version.
- Editing the `.nuspec` `$version$` instead of the csproj `<VersionPrefix>` — the build ignores the nuspec.
- Reporting green when the integration tests were silently skipped because Docker was not running.
- Choosing a patch bump for a change that drops a target framework or requires a new Serilog major;
  that is a **major** bump.
- Updating `README.md`/`CHANGELOG.md` but forgetting `CLAUDE.md` (or vice versa) — "all doc files".
