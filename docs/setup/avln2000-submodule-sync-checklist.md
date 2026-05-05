# AVLN2000 Recovery Checklist (submodule/remote sync)

## Purpose

Run this checklist when `AVLN2000` reappears after a known fix was already made in `cascade-ide`, or when parent repos show unexpected submodule drift.

Root cause pattern: `cascade-ide` fix exists in one remote/history, but parent repos still reference an older gitlink, or `main` differs across remotes.

## Preconditions

- You have access to all `cascade-ide` remotes used by the team (for example: `origin`, `github`, `wissance`).
- You are in a clean state (or have stashed WIP) in each repo before sync.

## Checklist

1. **Verify `main` parity inside `cascade-ide`**
   - `git fetch --all --prune`
   - `git checkout main`
   - `git pull --ff-only`
   - Compare remote heads:
     - `git rev-parse main`
     - `git rev-parse origin/main`
     - `git rev-parse github/main` (if configured)
     - `git rev-parse wissance/main` (if configured)
   - All hashes for `main` must match.

2. **Publish missing commits from `cascade-ide/main`**
   - If local `main` contains needed fix not present on one of remotes:
     - `git push <remote> main`
   - Repeat until all remotes point to the same `main` commit.

3. **Update gitlink in parent repo `open`**
   - In `Financial/software/open`:
     - `git submodule update --init --recursive`
     - `cd cascade-ide` and checkout the synced `main` commit
     - `cd ..`
     - `git add cascade-ide`
     - `git commit -m "chore(submodules): bump cascade-ide"`
     - `git push`

4. **Update gitlink in workspace root parent (if used)**
   - In the workspace root repo (for example `PersonalCursorFolder`), bump pointer to `Financial/software/open` the same way:
     - `git add Financial/software/open`
     - `git commit -m "chore(submodules): bump open"`
     - `git push`

5. **Validate build on clean state**
   - `git submodule update --init --recursive`
   - Clean build:
     - `dotnet build CascadeIDE.sln -c Debug`
   - Confirm `AVLN2000` does not reproduce.

## Fast diagnosis commands

- Show remotes: `git remote -v`
- Show current submodule pointer in parent: `git ls-tree HEAD cascade-ide`
- Show submodule status in parent: `git submodule status`
- Detect dirty submodule in parent status: `git status`

## Completion criteria

- `cascade-ide/main` is equal across active remotes.
- Parent gitlinks (`open`, then workspace root if applicable) are bumped and pushed.
- Fresh recursive checkout builds without `AVLN2000`.
