# slnx-validator

[![NuGet](https://img.shields.io/nuget/v/slnx-validator.svg)](https://www.nuget.org/packages/slnx-validator/)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=slnx-validator&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=slnx-validator)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=slnx-validator&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=slnx-validator)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=slnx-validator&metric=coverage)](https://sonarcloud.io/summary/new_code?id=slnx-validator)

`.slnx` is the modern XML-based solution format introduced by Microsoft — and honestly, it's a great improvement over the old `.sln` format. It's human-readable, merge-friendly, and easy to edit by hand. 🎉

There's just one catch: neither Visual Studio, MSBuild, nor the `dotnet` CLI fully validates `.slnx` files. Invalid constructs are silently accepted, which can lead to confusing errors that are surprisingly hard to trace back to the solution file.

`slnx-validator` fills that gap. It catches the issues the toolchain quietly ignores. 🔍

You could read more about the `.slnx` at the [official .NET blog post](https://devblogs.microsoft.com/dotnet/introducing-slnx-support-dotnet-cli/)

## Installation

```powershell
dotnet tool install -g slnx-validator
```

`slnx-validator` runs on .NET 8, 9, and 10. Note that using `.slnx` files in your projects requires .NET SDK 9 or later — but your projects themselves can still target .NET 8.

## Usage

Validate a single file:

```powershell
slnx-validator MySolution.slnx
```

Validate all `.slnx` files in a folder:

```powershell
slnx-validator src\
```

Validate using a wildcard pattern:

```powershell
slnx-validator src\MyProject*.slnx
```

Validate multiple files, folders, or patterns at once (comma-separated):

```powershell
slnx-validator "MySolution.slnx, src\*.slnx, other\"
```

Exit code `0` means everything is valid. Exit code `1` means one or more errors were found.

## Options

### `--sonarqube-report-file <file>`

Writes a [SonarQube generic issue report](https://docs.sonarsource.com/sonarqube-server/analyzing-source-code/importing-external-issues/generic-issue-import-format) to the specified JSON file. Import it into your Sonar analysis via the `sonar.externalIssuesReportPaths` property.

> 💡 When using `--sonarqube-report-file`, it's recommended to also pass `--continue-on-error` so the tool always exits with code `0`. This lets the SonarQube quality gate — not the tool's exit code — determine whether your pipeline fails.

```powershell
slnx-validator MySolution.slnx --sonarqube-report-file sonar-issues.json --continue-on-error
```

### `--continue-on-error`

Always exits with code `0`, even when validation errors are found. Useful in CI pipelines where SonarQube handles the failure decision. Default: `false`.

### `--sarif-report-file <file>`

Writes a [SARIF 2.1.0](https://docs.oasis-open.org/sarif/sarif/v2.1.0/sarif-v2.1.0.html) (Static Analysis Results Interchange Format) report to the specified file path. SARIF is the industry-standard format for static analysis results, supported natively by GitHub Code Scanning, Azure DevOps, Visual Studio, and VS Code.

> 💡 Both `.sarif` and `.sarif.json` are common file extensions. Use `.sarif.json` if your editor or CI tool benefits from JSON syntax highlighting.

```powershell
slnx-validator MySolution.slnx --sarif-report-file results.sarif --continue-on-error
```

Severity mapping from `RuleSeverity` to SARIF levels:

| Severity | SARIF level |
|----------|-------------|
| `BLOCKER`, `CRITICAL`, `MAJOR` | `error` |
| `MINOR` | `warning` |
| `INFO` | `note` |

Severity overrides (via `--minor`, `--info`, `--ignore`, etc.) are reflected in the SARIF output, just like in the SonarQube report.

### `--required-files`

Verify that a set of files or directories matching glob patterns exist on disk **and** are referenced as `<File>` entries in the solution file(s) being validated. Any failure is reported as a normal validation error (exit code `1`) that also appears in SonarQube reports.

- **Disk check** — if no files match the glob patterns, a `SLNX020` (`RequiredFileDoesntExistOnSystem`) error is added to the solution result.
- **Reference check** — for each matched file that is not referenced as `<File Path="...">` in the `.slnx`, a `SLNX021` (`RequiredFileNotReferencedInSolution`) error is added. The error message shows the exact `<File>` element that should be added.

Relative paths in the `.slnx` are resolved relative to the solution file's location.

**Syntax**

```
--required-files "<pattern1>;<pattern2>;..."
```

Patterns are separated by `;`. Patterns starting with `!` are exclusions. Pattern order matters: a later pattern can override an earlier one.

**Supported glob syntax**

| Pattern | Meaning | Example |
|---|---|---|
| `*` | Any file in the current directory (no path separator) | `doc/*.md` |
| `**` | Any depth of subdirectories | `src/**/*.cs` |
| `!pattern` | Exclude matching paths | `!**/bin/**` |
| `dir/` | Match a directory and its contents | `docs/` |

> **Note:** `{a,b}` alternation and `[abc]` character classes are not supported by this library. Use multiple patterns separated by `;` instead.
> For example, instead of `*.{cs,fs}`, use `**/*.cs;**/*.fs`.

**Examples**

Require all `.md` files under `doc/`:
```
slnx-validator MySolution.slnx --required-files "doc/*.md"
```

Require all `.yaml` files except those in the `src/` folder:
```
slnx-validator MySolution.slnx --required-files "**/*.yaml;!src/**"
```

Require a specific config file and the entire `docs/` directory:
```
slnx-validator MySolution.slnx --required-files "appsettings.json;docs/"
```

**Exit codes**

| Code | Description |
|------|-------------|
| `0`  | All patterns matched and all matched files are referenced in the solution. |
| `1`  | Any validation error — including required files not existing or not referenced. |

### Severity override flags

Override the severity of specific validation codes, or suppress them entirely. This controls both the exit code behaviour and the severity written to the SonarQube JSON report.

| Flag | Severity | Causes exit code `1`? |
|------|----------|-----------------------|
| `--blocker <codes>` | `BLOCKER` | ✅ yes |
| `--critical <codes>` | `CRITICAL` | ✅ yes |
| `--major <codes>` | `MAJOR` | ✅ yes (default for all codes) |
| `--minor <codes>` | `MINOR` | ❌ no — shown with `(warning)` label |
| `--info <codes>` | `INFO` | ❌ no — shown with `(info)` label |
| `--ignore <codes>` | *(suppressed)* | ❌ no — not shown at all, not in SonarQube or SARIF report |

Each flag accepts a **comma-separated list of codes** or the **wildcard `*`** to match all codes:

```powershell
# Suppress a specific code
slnx-validator MySolution.slnx --ignore SLNX011

# Downgrade a code to non-failing severity
slnx-validator MySolution.slnx --minor SLNX011,SLNX012

# Set everything to INFO, but keep SLNX011 as MAJOR
slnx-validator MySolution.slnx --info * --major SLNX011

# Ignore everything except SLNX013
slnx-validator MySolution.slnx --ignore * --major SLNX013
```

**Wildcard `*` and specific codes**

When `*` is combined with specific code flags, **specific codes always win over the wildcard**, regardless of flag order:

```powershell
# SLNX011 remains MAJOR even though --info * is also specified
slnx-validator MySolution.slnx --info * --major SLNX011
```

**Effect on reports**

Severity overrides are reflected in the generated rule definition in both SonarQube and SARIF reports:

```json
{
  "id": "SLNX011",
  "severity": "MINOR",
  ...
}
```

Codes set to `--ignore` are excluded from both the `rules` and `issues`/`results` arrays entirely.

## SonarQube integration example

```powershell
slnx-validator MySolution.slnx --sonarqube-report-file sonar-issues.json --continue-on-error
```

```json
{
  "rules": [
    {
      "id": "SLNX011",
      "name": "Referenced file not found",
      "description": "A file referenced in a <File Path=\"...\"> element does not exist on disk.",
      "engineId": "slnx-validator",
      "cleanCodeAttribute": "COMPLETE",
      "type": "BUG",
      "severity": "MAJOR",
      "impacts": [
        {
          "softwareQuality": "MAINTAINABILITY",
          "severity": "MEDIUM"
        }
      ]
    }
  ],
  "issues": [
    {
      "ruleId": "SLNX011",
      "primaryLocation": {
        "message": "File not found: docs\\CONTRIBUTING.md",
        "filePath": "MySolution.slnx",
        "textRange": {
          "startLine": 4
        }
      }
    }
  ]
}
```

Then configure the SonarQube scanner:

```properties
sonar.externalIssuesReportPaths=$(Build.ArtifactStagingDirectory)/slnx-sonar-issues.json
# Required: without this, .slnx files are not visible to SonarQube
sonar.sources=${{ parameters.slnPath }}
# Required: enables syntax highlighting and XML rules for .slnx files in SonarQube
sonar.xml.file.suffixes=.xml,.xsd,.xsl,.slnx
```

## GitHub Code Scanning integration example

```powershell
slnx-validator MySolution.slnx --sarif-report-file results.sarif --continue-on-error
```

```yaml
- name: Validate .slnx files
  run: slnx-validator MySolution.slnx --sarif-report-file results.sarif --continue-on-error

- name: Upload SARIF to GitHub Code Scanning
  uses: github/codeql-action/upload-sarif@v3
  with:
    sarif_file: results.sarif
```

This uploads the validation results to the **Security → Code Scanning** tab of your repository. Issues appear as alerts with rule descriptions, file locations, and links back to the relevant lines.

```json
{
  "$schema": "https://json.schemastore.org/sarif-2.1.0.json",
  "version": "2.1.0",
  "runs": [
    {
      "tool": {
        "driver": {
          "name": "slnx-validator",
          "informationUri": "https://github.com/304NotModified/SLNX-validator",
          "rules": [
            {
              "id": "SLNX011",
              "shortDescription": {
                "text": "Referenced file not found"
              },
              "fullDescription": {
                "text": "A file referenced in a <File Path=\"...\"> element does not exist on disk."
              },
              "defaultConfiguration": {
                "level": "error"
              }
            }
          ]
        }
      },
      "results": [
        {
          "ruleId": "SLNX011",
          "level": "error",
          "message": {
            "text": "File not found: docs\\CONTRIBUTING.md"
          },
          "locations": [
            {
              "physicalLocation": {
                "artifactLocation": {
                  "uri": "MySolution.slnx"
                },
                "region": {
                  "startLine": 4
                }
              }
            }
          ]
        }
      ]
    }
  ]
}
```

## Example output

### All valid ✅

```powershell
slnx-validator MySolution.slnx
```

```
[OK]   MySolution.slnx
```

### Errors found ❌

```powershell
slnx-validator MySolution.slnx
```

```
[FAIL] MySolution.slnx

MySolution.slnx
  - line 5: [SLNX013] The element 'Folder' in namespace '...' has invalid child element 'Folder'. List of possible elements expected: 'Project'.
  - line 12: [SLNX011] File not found: docs\CONTRIBUTING.md
```

### Multiple files — mixed results

```powershell
slnx-validator src\
```

```
[OK]   src\Frontend.slnx
[FAIL] src\Backend.slnx

src\Backend.slnx
  - line 4: [SLNX011] File not found: docs\CONTRIBUTING.md
  - line 8: [SLNX012] Wildcard patterns are not supported in file paths: docs\*.md
```

## What is validated

This tool checks what `dotnet` / MSBuild / Visual Studio does **not** validate by default:

- **XSD schema validation** — verifies that the `.slnx` file conforms to the [official Microsoft schema](https://github.com/microsoft/vs-solutionpersistence/blob/main/src/Microsoft.VisualStudio.SolutionPersistence/Serializer/Xml/Slnx.xsd).
  Visual Studio silently accepts certain invalid constructs without showing any error — for example, a `<Folder>` nested inside another `<Folder>` (see [`examples/invalid-xsd.slnx`](examples/invalid-xsd.slnx)).
- **Solution folder file existence** — checks that every `<File Path="...">` listed inside a `<Folder>` actually exists on disk.
- **Wildcard usage** — `.slnx` does not support wildcard patterns. Visual Studio silently accepts them but simply ignores the entries, so your files appear to be listed but are never actually resolved. `slnx-validator` catches this in `<File Path="...">` entries (see [`examples/invalid-wildcard.slnx`](examples/invalid-wildcard.slnx)):

  ```xml
  <!-- ❌ Silently ignored by Visual Studio — no error, no files loaded -->
  <Folder Name="docs">
    <File Path="docs\*.md" />
  </Folder>
  ```

  > Wildcard support is a [known open request](https://github.com/dotnet/sdk/issues/41465) that was closed as not planned.

The following are **intentionally out of scope** because the toolchain already handles them:

- Project file existence (`<Project Path="...">`) — `dotnet build` / MSBuild already reports missing project files.

## Error codes

| Code | Name | Description |
|------|------|-------------|
| `SLNX001` | `FileNotFound`            | The input `.slnx` file does not exist. |
| `SLNX002` | `InvalidExtension`        | The input file does not have a `.slnx` extension. |
| `SLNX003` | `NotATextFile`            | The file is binary and cannot be parsed as XML. |
| `SLNX010` | `InvalidXml`              | The file is not valid XML (see [`examples/invalid-not-xml.slnx`](examples/invalid-not-xml.slnx)). |
| `SLNX011` | `ReferencedFileNotFound`  | A file referenced in `<File Path="...">` does not exist on disk. |
| `SLNX012` | `InvalidWildcardUsage`    | A `<File Path="...">` contains a wildcard pattern (see [`examples/invalid-wildcard.slnx`](examples/invalid-wildcard.slnx)). |
| `SLNX013` | `XsdViolation`            | The XML structure violates the schema, e.g. `<Folder>` inside `<Folder>` (see [`examples/invalid-xsd.slnx`](examples/invalid-xsd.slnx)). |
| `SLNX020` | `RequiredFileDoesntExistOnSystem`   | A `--required-files` pattern matched no files on the file system. |
| `SLNX021` | `RequiredFileNotReferencedInSolution` | A `--required-files` matched file exists on disk but is not referenced as a `<File>` element in the solution. |

## XSD Schema

Microsoft doesn't provide much documentation for the `.slnx` format, but there is an XSD schema in the official `vs-solutionpersistence` repository — and it's enough to catch real structural problems before they cause trouble:

> https://github.com/microsoft/vs-solutionpersistence/blob/main/src/Microsoft.VisualStudio.SolutionPersistence/Serializer/Xml/Slnx.xsd

Licensed under the MIT License — Copyright (c) Microsoft Corporation.
