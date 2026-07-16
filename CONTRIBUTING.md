# Contributing

## Commit messages

This repo has a commit message template at `.gitmessage`. Git doesn't apply
repo-committed templates automatically, so opt in once per clone:

```
git config commit.template .gitmessage
```

## Pull requests

Opening a PR pre-fills the description from
`.github/pull_request_template.md` - fill in the summary and test plan.
BLE/board behavior generally can't be verified by CI alone, so call out
anything that still needs a hardware test.

## Releases

See the comments at the top of `.github/workflows/release-android.yml` for
how to cut a signed release APK. Release notes are auto-generated and
grouped using the categories in `.github/release.yml` - label PRs
accordingly (`bug`, `enhancement`, `dependencies`) if you want them grouped
correctly.
