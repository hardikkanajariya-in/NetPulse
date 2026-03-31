# Branching Strategy

NetPulse uses a lightweight release flow built around `main`, `develop`, and short-lived working branches.

## Branches

- `main`: stable, protected, always releasable
- `develop`: integration branch for ongoing work
- `feature/*`: new work branched from `develop`, merged back into `develop`
- `fix/*`: non-critical bug fixes branched from `develop`, merged back into `develop`
- `hotfix/*`: urgent production fixes branched from `main`, merged into both `main` and `develop`
- `release/*`: release preparation branches created from `develop`

## Standard Flow

1. Branch from `develop`
2. Open a pull request back to `develop`
3. Merge using squash merge after review and CI passes
4. When `develop` is ready, create `release/x.y.z`
5. Merge `release/x.y.z` into `main`
6. Tag `main` with `vX.Y.Z`
7. Push the tag to trigger the release workflow
8. Merge the release branch back into `develop` if additional release-only changes were made

## Hotfix Flow

1. Branch `hotfix/x.y.z` from `main`
2. Apply the fix and open PR to `main`
3. After merge, tag the release
4. Merge the hotfix back into `develop`

## Protection Rules

- `main` requires pull requests, at least one approval, and passing CI
- `develop` requires pull requests and passing CI
- Direct pushes to protected branches are disabled
