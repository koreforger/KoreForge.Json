[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [string]$Note,

    [switch]$TagOnly,

    [switch]$Force
)

$tag = 'Json/v' + $Version

Push-Location (Resolve-Path "$PSScriptRoot\..")
try {
    if (-not $TagOnly) {
        if (git status --porcelain) {
            $commitMsg = if ($Note) { $Note } else { "chore: release $Version" }
            git add -A
            git commit -m $commitMsg
            git push
        }
    }

    $tagMsg = if ($Note) { $Note } else { "Release $Version" }
    if ($Force) {
        git tag --force -a $tag -m $tagMsg
        git push origin --force-with-lease "refs/tags/$tag"
    } else {
        git tag -a $tag -m $tagMsg
        git push origin $tag
    }
    Write-Host "Tagged and pushed $tag" -ForegroundColor Green
} finally {
    Pop-Location
}
