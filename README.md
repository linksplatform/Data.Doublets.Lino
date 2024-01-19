[![Gitpod](https://img.shields.io/badge/Gitpod-ready--to--code-blue?logo=gitpod)](https://gitpod.io/#https://github.com/linksplatform/Data.Doublets.Lino)

[![NuGet Version and Downloads count](https://buildstats.info/nuget/Platform.Data.Doublets.Lino)](https://www.nuget.org/packages/Platform.Data.Doublets.Lino)
[![Actions Status](https://github.com/linksplatform/Data.Doublets.Lino/workflows/CD/badge.svg)](https://github.com/linksplatform/Data.Doublets.Lino/actions?workflow=CD)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/cd23af97753f4dc2be394daeb2175042)](https://www.codacy.com/gh/linksplatform/Data.Doublets.Lino/dashboard?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=linksplatform/Data.Doublets.Lino&amp;utm_campaign=Badge_Grade)
[![CodeFactor](https://www.codefactor.io/repository/github/linksplatform/Data.Doublets.Lino/badge)](https://www.codefactor.io/repository/github/linksplatform/Data.Doublets.Lino)

# [Data.Doublets.Lino](https://github.com/linksplatform/Data.Doublets.Lino)

LinksPlatform's Platform.Data.Doublets.Lino Class Library.

Namespace: [Platform.Data.Doublets.Lino](https://linksplatform.github.io/Data.Doublets.Lino/csharp/api/Platform.Data.Doublets.Lino.html)

NuGet package: [Platform.Data.Doublets.Lino](https://www.nuget.org/packages/Platform.Data.Doublets.Lino)

## [Documentation](https://linksplatform.github.io/Data.Doublets.Lino/csharp/api/Platform.Data.Doublets.Lino.html)

[PDF file](https://linksplatform.github.io/Data.Doublets.Lino/csharp/Platform.Data.Doublets.Lino.pdf) with code for e-readers.

## Depends on
*   [Platform.Data.Doublets.Sequences](https://github.com/linksplatform/Data.Doublets.Sequences)

## Tools
### [lino2links](https://www.nuget.org/packages/lino2links)
#### SYNOPSIS
   ```shell
   lino2links SOURCE DESTINATION [DOCUMENT_NAME]
   ```
#### PARAMETERS
* `SOURCE` - a lino file path.
* `DESTINATION` - a links storage file path.
* `DOCUMENT_NAME` - a document name.  
#### Note:
`DOCUMENT_NAME` is used to define what name to save a document with. A links storage can contain multiple lino documents. If document name is not specified the entire links data store is exported or imported as is.
#### Example
1. Install
    ```shell
    dotnet tool install --global lino2links
    ```
2. Import a lino file from a doublets links storage
    ```shell
   lino2links notation.lino db.links "MyDocument"
   ```
---
### [links2lino](https://www.nuget.org/packages/links2lino)
#### SYNOPSIS
   ```shell
   links2lino SOURCE DESTINATION [DOCUMENT_NAME]
   ```
#### PARAMETERS
* `SOURCE` - a links storage path.
* `DESTINATION` - a lino file path.
* `DOCUMENT_NAME` - a document name. 
#### Note:
`DOCUMENT_NAME` is used to choose which lino document to export from a links storage. A links storage can contain multiple lino documents. If document name is not specified the entire links data store is exported or imported as is.
#### Example
1. Install
    ```shell
    dotnet tool install --global links2lino
    ```
2. Export lino file to doublets links storage
    ```shell
   links2lino db.links notation.lino "MyDocument"
   ```
