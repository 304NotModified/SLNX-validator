# Release Task — Copilot Coding Agent Prompt

Use this file to perform a full release with a single prompt.

## Usage

Type in Copilot chat:

```
Run the release task for version X.Y.Z
```

or shorthand:

```
/release X.Y.Z
```

---

## Step-by-step instructions for the agent

### 1. Gather changes since the last release

- Fetch the latest release tag via the GitHub API: `GET /repos/{owner}/{repo}/releases/latest`
- Fetch all closed (merged) PRs via: `GET /repos/{owner}/{repo}/pulls?state=closed`
- Filter PRs where `merged_at` is after the date of the latest release

### 2. Filter and categorize the PRs

**Ignore entirely** (do not include in the changelog):
- PRs authored by `dependabot[bot]`
- PRs with label `documentation` or whose title starts with `docs:`
- PRs with label `dependencies`
- PRs whose title contains `Bump version` or `release notes`

**Categorize the remaining PRs** (check both the PR title prefix **and** the GitHub label):
- **New features** — `feat:` title prefix OR label `enhancement`: list as a main bullet with a short description
- **Bug fixes** — `fix:` title prefix OR label `bug`: list as a main bullet with a short description
- **Internal improvements** — `chore:`/`refactor:` title prefix, label `refactoring`, label `build`, or titles containing `Move`, `Rename`, `Replace public API`: list briefly at the bottom in a separate section `🔧 Internal improvements` with PR links only

### 3. Generate the short changelog (for `<PackageReleaseNotes>` in the csproj)

- Maximum ~5 bullet points
- Include only features and bug fixes — no refactoring, no docs
- Format:
  ```
  * [description] in [PR link]
  ```

### 4. Generate the extended changelog (for the GitHub Release body)

Sections (include only if relevant PRs exist):
- `## ✨ New features`
- `## 🐛 Bug fixes`
- `## 🔧 Internal improvements` — brief, with PR links only, no explanation

For relevant features, add a **short code example** demonstrating the new CLI argument, e.g.:

```powershell
slnx-validator MySolution.slnx --new-flag
```

Close with:

```
**Full Changelog**: https://github.com/{owner}/{repo}/compare/vOLD...vNEW
```

Documentation changes (`docs:` prefix) are **not** included.

### 5. Determine the release title

- Format: `X.Y.Z: <short description of the most important change(s)>`
- Maximum ~70 characters
- Summarize using the 1–2 most important features or fixes
- Example: `0.6.0: SARIF report output & severity override flags`

### 6. Update the version in the csproj

File: `src/SLNX-validator/SLNX-validator.csproj`

- Set `<VersionPrefix>` to the new version (e.g. `0.6.0`)
- Replace the contents of `<PackageReleaseNotes>` with the generated short changelog from step 3

### 7. Open a PR

- Include only the csproj change
- Title: `Bump version to X.Y.Z and update release notes`
- Body: the full extended changelog from step 4

### 8. Create the GitHub Release via the API

**Wait for the PR from step 7 to be merged into `main` before creating the release.**

`POST /repos/{owner}/{repo}/releases`

```json
{
  "tag_name": "vX.Y.Z",
  "name": "<release title from step 5>",
  "body": "<extended changelog from step 4>",
  "draft": false,
  "prerelease": false,
  "target_commitish": "main"
}
```

---

## Changelog rules

- Write in **English**
- Do not include documentation changes in release notes
- Mention refactorings only briefly with a PR link — no explanation
- Features may include a short example
- Keep it concise but informative

---

## Example output

**Release title:** `0.6.0: SARIF report output & severity override flags`

**Short changelog (csproj):**

```
* feat: Add SARIF 2.1.0 report output in https://github.com/304NotModified/SLNX-validator/pull/56
* feat: Add CLI severity override flags (--blocker, --critical, etc.) in https://github.com/304NotModified/SLNX-validator/pull/45
* fix: Remove duplicate filename in verbose error output in https://github.com/304NotModified/SLNX-validator/pull/52
```

**GitHub Release body:**

```markdown
## ✨ New features

- **SARIF 2.1.0 report output** — generate a SARIF report for integration with GitHub Code Scanning, Azure DevOps, Visual Studio, and more ([#56](https://github.com/304NotModified/SLNX-validator/pull/56))

  ```powershell
  slnx-validator MySolution.slnx --sarif-report-file results.sarif
  ```

- **CLI severity override flags** — override the severity of specific validation rules per run ([#45](https://github.com/304NotModified/SLNX-validator/pull/45))

  ```powershell
  slnx-validator MySolution.slnx --minor SLNX001 --ignore SLNX002
  ```

## 🐛 Bug fixes

- Remove duplicate filename in verbose error output ([#52](https://github.com/304NotModified/SLNX-validator/pull/52))

## 🔧 Internal improvements

- Replace public `XDocument` API on `SlnxFile` with typed SLNX domain model ([#54](https://github.com/304NotModified/SLNX-validator/pull/54))
- Move XML parsing into `SlnxCollector` and rename `ValidationCollector` ([#51](https://github.com/304NotModified/SLNX-validator/pull/51))

**Full Changelog**: https://github.com/304NotModified/SLNX-validator/compare/v0.5.0...v0.6.0
```
