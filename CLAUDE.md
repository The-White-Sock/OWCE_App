# CLAUDE.md

Guidance for Claude Code (or any agent) working in this repo. See `README.md` for
build/release instructions and `CONTRIBUTING.md` for commit/PR/issue conventions -
this file is about *how to work*, not *how to build*.

## What this is

Onewheel Community Edition (OWCE) - a Xamarin.Forms 5.0 app (classic, non-SDK-style
platform projects) that talks to a Onewheel board over Bluetooth LE. Shared logic
lives in `OWCE/OWCE` (netstandard2.1); `OWCE.Android`/`OWCE.iOS`/`OWCE.MacOS`/
`OWCE.WatchOS` hold platform-specific glue, most importantly each platform's own
`OWBLE.cs` GATT implementation.

## Hard constraint: there is no local compiler here

This environment has no Android/iOS/Mac SDK and no `dotnet` CLI. **Nothing can be
compiled locally.** The only ways to verify a change are:
1. Push and let GitHub Actions (`build.yml`) compile it - minutes, not seconds.
2. Ask the user to test on real hardware (they have a 2017 Onewheel+ and an Android
   phone) - the only way to verify BLE/runtime behavior at all.

A CI pass proves the code *compiles*. It proves nothing about whether a BLE fix
actually fixes the reported symptom. Don't conflate the two, and don't tell the
user something is "fixed" - say "compiles, needs your hardware to confirm."

## Calibrate investigation depth to blast radius, not habit

The balance to strike: don't under-investigate code that can strand a rider or
brick a connection, and don't over-investigate a typo fix or a CI YAML tweak.
Neither extreme serves the user.

**Go deep (trace every path, not just the one being fixed) when touching:**
- `OWBLE.cs` (either platform) - the GATT connection/reconnect state machine
- Ride mode writes or anything that changes board behavior mid-ride
- Anything with a timeout, deadline, or "give up after N" mechanism

**Why this category specifically:** a real regression shipped in this exact area -
a 60s reconnect give-up timeout that never fired, because the deadline was only
*checked* inside a callback (`RetryReconnect`) that itself only runs if a prior
native callback already fired. The bug wasn't the arithmetic, it was an unexamined
assumption: "will this callback always eventually run?" Android's BLE stack does
not guarantee a fresh `ConnectGatt()` ever calls back at all if the peripheral is
unreachable - unlike an *established* connection's supervision timeout, a
connection *attempt* has no such guarantee. That single unverified assumption is
what let a 30-line "fix" ship broken.

The concrete habit this implies: for any timeout/watchdog you add, ask "what
happens if the callback this depends on never fires?" *before* considering it
done. If the answer is "the mechanism silently never runs," it needs an
independent trigger (its own timer), not a check nested inside a conditional
callback.

**Move fast, don't relitigate, when touching:**
- Docs, `CONTRIBUTING.md`, `.github/*.yml`, issue/PR bookkeeping
- Anything that's a one-way information change (logging, comments, README)
- Exploratory research (filing an issue, cross-referencing community projects)

Don't spawn extra verification passes, don't re-read files you just wrote, don't
hedge with "let me also double check" on low-risk work. Ship it.

## Before calling a BLE/reconnect fix done

1. Re-read the *whole* function you changed, not just your diff - state machines
   accumulate special cases, and the bug is as likely to be in an unchanged
   branch that now behaves differently as in the lines you touched.
2. Trace every path that reaches the code you changed, including ones triggered
   by native callbacks that may or may not fire, may fire late, or may fire more
   than once.
3. Ask "does anything here assume a callback is guaranteed to happen?" If yes,
   that assumption needs to be either verified against real Android/iOS docs or
   backstopped with an independent timer.
4. Say explicitly what still needs hardware confirmation. Don't imply "fixed"
   for something only compiled.

## Process conventions (see CONTRIBUTING.md for the full detail)

- Gitmoji-prefixed commits and PR titles; `.gitmessage`/`.githooks/commit-msg` are
  opt-in via `git config`.
- One concern per PR. Fast auto-merge on green CI is intentional (see issue #30) -
  there's no separate review window, so make sure it's actually right before
  pushing, not "probably right, CI will catch it" (CI only compiles, see above).
- Backlog lives in GitHub Issues (tracking issue + linked sub-issues), not a
  Projects board - this repo's GitHub App has no Projects API access.
- Releases: `workflow_dispatch` on `release-android.yml` with version `X.Y.Z-test`
  (no leading `v` - the workflow adds it; a leading `v` in the input produces a
  malformed `vX.Y.Z-test` tag).
