$ErrorActionPreference = "Continue"

# Project scope. For global use instead:
#   $Dest = Join-Path $env:USERPROFILE ".gemini\config\skills"
$Dest = Join-Path (Get-Location) ".agents\skills"
New-Item -ItemType Directory -Force -Path $Dest | Out-Null

$Tmp = Join-Path $env:TEMP ("skills_" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $Tmp | Out-Null

function Install-From {
    param([string]$Repo, [string[]]$Skills)

    $dir = Join-Path $Tmp ($Repo -replace "/", "_")
    if (-not (Test-Path $dir)) {
        git clone --depth 1 "https://github.com/$Repo.git" $dir 2>$null | Out-Null
        if (-not (Test-Path $dir)) { Write-Host "x could not clone $Repo"; return }
    }

    foreach ($skill in $Skills) {
        # find a folder named $skill that contains SKILL.md, anywhere in the repo
        $match = Get-ChildItem -Path $dir -Recurse -Filter SKILL.md -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Directory.Name -eq $skill -and $_.FullName -notmatch "node_modules" } |
            Select-Object -First 1

        if ($match) {
            $target = Join-Path $Dest $skill
            if (Test-Path $target) { Remove-Item -Recurse -Force $target }
            Copy-Item -Recurse -Path $match.Directory.FullName -Destination $target
            Write-Host "OK  $skill  ($Repo)"
        } else {
            Write-Host "x   $skill not found in $Repo"
        }
    }
}

# --- Design ---
Install-From "anthropics/skills" @("frontend-design", "theme-factory", "canvas-design", "brand-guidelines", "web-artifacts-builder")
Install-From "vercel-labs/agent-skills" @("web-design-guidelines")

# --- Development ---
Install-From "vercel-labs/agent-skills" @("react-best-practices", "composition-patterns")
Install-From "anthropics/skills" @("webapp-testing", "mcp-builder")

# --- Workflow: plan, test, debug, verify ---
Install-From "obra/superpowers" @(
    "brainstorming", "writing-plans", "executing-plans", "test-driven-development",
    "systematic-debugging", "verification-before-completion", "requesting-code-review"
)

Remove-Item -Recurse -Force $Tmp -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Installed in: $Dest"
Get-ChildItem $Dest | Select-Object -ExpandProperty Name
Write-Host "Restart Antigravity to load them."