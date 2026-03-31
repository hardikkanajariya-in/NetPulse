# Repository Rules

## Visibility Model

This repository is intended to be public and open source, while keeping contribution and release control managed by the maintainer.

## Branch Protection

### main

- Pull requests required
- At least 1 approval required
- Required status check: `Build / build`
- Dismiss stale approvals when new commits are pushed
- Force pushes disabled
- Branch deletion disabled

### develop

- Pull requests required
- Required status check: `Build / build`
- Force pushes disabled

## Merge Strategy

- Squash merge is preferred
- Rebase merge may be allowed for clean linear history
- Direct commits to protected branches are not allowed

## Review Expectations

- New features should be reviewed before merge
- Bug fixes should include a clear reproduction or rationale
- UI changes should include screenshots or a short recording when practical

## Labels

Recommended default labels:

- `bug`
- `enhancement`
- `documentation`
- `good first issue`
- `help wanted`
- `question`
- `wontfix`
- `duplicate`

## Release Control

- Only the maintainer publishes official releases from `main`
- Releases are created from signed or trusted tags following `v*` semantic versioning
- Release artifacts are produced by GitHub Actions

## Maintainer

- Hardik Kanajariya
- https://hardikkanajariya.in
