# Versioning

## Scheme: Semantic Versioning

This project uses [Semantic Versioning 2.0.0](https://semver.org/): `MAJOR.MINOR.PATCH`,
e.g. `1.4.2`, with an optional pre-release suffix (`-test`, `-rc.1`, etc.) attached
with a hyphen.

- **PATCH** - a backward-compatible bug fix, no new capability. Most P0/P1 issue
  fixes land here (e.g. a reconnect-reliability fix).
- **MINOR** - a backward-compatible new capability (e.g. #26's crash telemetry,
  #34's offline board cache). Also used for any change while still on `0.y.z` -
  see below.
- **MAJOR** - a breaking change. For an app (not a library) this means things like:
  dropping support for a previously-supported board generation or OS version, a
  locally-stored data format (`Ride`, `CachedBoardData`) that changes shape without
  a migration, or intentionally removing behavior users previously relied on.
- **`0.y.z` (major version zero)** - per the SemVer spec itself: *"anything MAY
  change at any time, the public API SHOULD NOT be considered stable."* This
  project has been at `0.0.x` since its first tagged release, and stays there
  until the [1.0.0 checklist](#reaching-100) below is met.

Android's `versionCode` (a separate integer Google Play/Android itself requires to
be strictly increasing between installs) is **not** hand-edited - `release-android.yml`
derives it automatically from the CI run number, which is already monotonic. Don't
try to keep it in sync with `MAJOR.MINOR.PATCH` by hand; it isn't meant to be.

## The `-test` pre-release suffix

Per this repo's `CLAUDE.md`: **a green CI build proves the code compiles - it
proves nothing about whether a BLE fix actually works on real hardware.** Only
installing a build on the user's own board/phone does that. `-test` is the
version string's way of tracking that exact gap:

- `-test` means: *this build is CI-green but has not yet been confirmed on real
  hardware.*
- Drop the suffix only once **that specific build** (same commit/tag) has actually
  been installed and ridden - the version number becomes the record of "this one
  was verified," not "we think it's probably fine."
- `-test` is a valid arbitrary pre-release identifier under SemVer, so it already
  sorts correctly (`1.2.0-test` < `1.2.0`) with no special tooling. If a staged
  rollout is ever wanted, the more conventional graduated identifiers work the same
  way: `-alpha` < `-beta` < `-rc.1` < the final release.

## Reaching 1.0.0

`1.0.0` is the point at which the app is considered solid enough for its target
users to depend on for its actual supported platform(s) - not "zero known bugs
anywhere." Distribution today is a sideloaded APK (no Play Store listing), and
Android is the only hardware-tested platform, so `1.0.0` tracks Android readiness;
iOS/macOS/WatchOS parity is a separate, later concern (see [#20](https://github.com/The-White-Sock/OWCE_App/issues/20))
and doesn't block it.

Checklist before tagging `1.0.0`:

- [ ] The release build has had a real ride session on the target hardware
      (connect, ride-mode change, live telemetry, disconnect, and a
      reconnect-after-range-loss) with no hang/crash/lost-board.
- [ ] [#21](https://github.com/The-White-Sock/OWCE_App/issues/21) (Android
      `targetSdkVersion` ceiling) has an explicit, written decision attached to
      it - either fixed, or formally accepted as a known constraint of the
      current sideload-only distribution (not silently ignored).
- [ ] README/release notes explicitly say iOS/macOS/WatchOS are unsupported or
      experimental, rather than leaving platform parity implied.
- [ ] No open P0/P1 issue describes a safety- or data-loss-relevant bug that
      hasn't been hardware-confirmed fixed (check the [#19 backlog](https://github.com/The-White-Sock/OWCE_App/issues/19)
      for current P0/P1 items before tagging).

Backlog items like dark mode (#35), the full MAUI migration (#31), the UX pass
(#40), or DigiTilt (#62) are post-1.0 roadmap, not `1.0.0` blockers.

## Tagging in practice

- Tag format: `vMAJOR.MINOR.PATCH[-prerelease]`, e.g. `v0.1.0`, `v1.0.0-rc.1`, `v1.0.0`.
- **`workflow_dispatch`'s `version` input takes the version WITHOUT the leading
  `v`** - the workflow adds it. Typing `v1.2.3` into that field produces the
  broken double-`v` tag `vv1.2.3` (this actually happened - see this repo's own
  tag history, `vv0.0.2-test` through `vv0.0.5-test`). `release-android.yml` now
  rejects an input starting with `v` outright, and validates the rest against a
  SemVer pattern, so a typo fails the workflow immediately instead of quietly
  producing a malformed tag/release.
- See `CONTRIBUTING.md`'s Releases section and the comments at the top of
  `.github/workflows/release-android.yml` for the release mechanics themselves.
