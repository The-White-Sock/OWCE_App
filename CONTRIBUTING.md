# Contributing

## Commit messages

Commits use [Gitmoji](https://gitmoji.dev) - prefix the summary line with an
emoji signaling the kind of change (🐛 bug fix, ✨ new feature, ⬆️ dependency
bump, etc.). `.gitmessage` has a quick-reference list built in.

This repo has a commit message template at `.gitmessage`. Git doesn't apply
repo-committed templates automatically, so opt in once per clone:

```
git config commit.template .gitmessage
```

There's also an opt-in hook that rejects a commit if its first line doesn't
start with a recognized gitmoji:

```
git config core.hooksPath .githooks
```

## Pull requests

Opening a PR pre-fills the description from
`.github/pull_request_template.md` - fill in the summary and test plan.
BLE/board behavior generally can't be verified by CI alone, so call out
anything that still needs a hardware test. Prefix the PR title with a
gitmoji too, matching the commit convention above.

PRs merge quickly once CI is green - that's intentional, not an oversight.
CI (build success on both `build-core`/`build-android`, plus the
format-check gate) is what protects `main`; there's no separate soak/review
window layered on top of that.

## Code style

Formatting and naming conventions live in `.editorconfig` - most editors
(Visual Studio, VS Code, Rider) apply it automatically, no setup needed.
`OWCE/OWCE/OWCE.csproj` (the shared core library) also runs Roslyn analyzers
as build warnings; the classic Xamarin.Android/.iOS/.Mac projects don't
support that MSBuild property, so it's only set there.

CI checks `OWCE/OWCE/OWCE.csproj` against `.editorconfig` on every push -
scoped to the `whitespace`/`style` categories only, and blocking (see the
comment on that step in `build.yml` for why `analyzers` is deliberately
excluded). To apply formatting fixes across that project, run the
manually-triggered **Format Code** workflow from the Actions tab; it opens
a PR with the result rather than committing directly.

## Releases

See the comments at the top of `.github/workflows/release-android.yml` for
how to cut a signed release APK. Release notes are auto-generated and
grouped using the categories in `.github/release.yml` - label PRs
accordingly (`bug`, `enhancement`, `dependencies`) if you want them grouped
correctly.
