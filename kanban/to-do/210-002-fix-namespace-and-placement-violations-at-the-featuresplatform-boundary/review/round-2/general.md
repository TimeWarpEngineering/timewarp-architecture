# Round 2 — general
**Date:** 2026-09-12
**Scope reviewed:** M1 snapshot FQN retarget; carry round-1 M1; scan fix delta

## Summary

M1 is confirmed fixed. The living `PostgresDbContextModelSnapshot.cs` entity name is now `TimeWarp.Architecture.Authorization.RolePermissionGrant`, matching the CLR type in `role-permission-grant-infrastructure.cs` (`namespace TimeWarp.Architecture.Authorization`). The working-tree delta is a one-line FQN retarget; historical `*.Designer.cs` files still name `TimeWarp.Architecture.Features.RolePermissionGrant` as expected. No other living (non-Designer) files still map the snapshot entity to Features, and no new defects were found in the fix delta.

## Issues

