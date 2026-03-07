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

## XSD Schema

Microsoft doesn't provide much documentation for the `.slnx` format, but there is an XSD schema in the official `vs-solutionpersistence` repository — and it's enough to catch real structural problems before they cause trouble:

> https://github.com/microsoft/vs-solutionpersistence/blob/main/src/Microsoft.VisualStudio.SolutionPersistence/Serializer/Xml/Slnx.xsd

Licensed under the MIT License — Copyright (c) Microsoft Corporation.
