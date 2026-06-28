# Solution Explorer icon subset (ADR 0167)

Icons under this folder used by `SolutionExplorerView` are a **curated subset** of
[vscode-icons](https://github.com/vscode-icons/vscode-icons) (MIT License).

Source mapping (vscode-icons filename → local name):

| Local | vscode-icons |
|-------|----------------|
| `cs.svg` | `file_type_csharp.svg` |
| `csproj.svg` | `file_type_csproj.svg` |
| `fsproj.svg` | `file_type_fsproj.svg` |
| `vbproj.svg` | `file_type_vbproj.svg` |
| `solution.svg` | `file_type_sln.svg` |
| `folder.svg` | `default_folder.svg` |
| `file.svg` | `default_file.svg` |
| `json.svg` … `bat.svg` | `file_type_*.svg` (see import script in repo history) |
| `axaml.svg` | `file_type_xaml.svg` (Avalonia XAML; no dedicated axaml glyph upstream) |

Do not commit the full vscode-icons tree (~1000+ files). Refresh via:

`curl` from `https://raw.githubusercontent.com/vscode-icons/vscode-icons/master/icons/`
