# 036: Fix Directory.Build.props Linux DateTime Issue

## Description

Critical build failure on Linux due to MSBuild DateTime expression compatibility issue in Directory.Build.props.

## Parent

031_Fix-Assembly-Marker-Inconsistency (discovered during task validation)

## Requirements

- Fix the DateTime expression that fails on Linux systems
- Restore git commit date functionality in assembly metadata
- Ensure cross-platform compatibility for Windows and Linux builds
- Preserve existing functionality while fixing Linux compatibility

## Current Issue

**Location**: `Directory.Build.props` lines 81-99

**Error**: 
```
error MSB4184: The expression """.AddSeconds(%ct)" cannot be evaluated. Method 'System.DateTime.AddSeconds' not found.
```

**Problem**: The current approach uses:
```xml
<GitCommitDate>$([System.DateTime]::Parse($(UnixEpochStart)).AddSeconds($(GitCommitTimestamp)).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssK'))</GitCommitDate>
```

This fails on Linux because the MSBuild expression evaluation behaves differently.

## Solution Analysis

**Working Alternative Available**: Lines 101-103 show a simpler approach:
```xml
<LastCommitDate>$([System.DateTime]::UnixEpoch.AddSeconds($(GitCommitTimestamp)).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssK"))</LastCommitDate>
```

**Key Differences**:
1. Uses `System.DateTime]::UnixEpoch` instead of parsing a string date
2. Directly calls `AddSeconds()` on the Unix epoch
3. Simpler expression that should work cross-platform

## Implementation Plan

### Step 1: Research and Verify
- [ ] Test the alternative DateTime.UnixEpoch approach on both Windows and Linux
- [ ] Verify the output format matches expectations
- [ ] Ensure the git commit timestamp is correctly parsed

### Step 2: Implement Fix
- [ ] Uncomment and activate the working UnixEpoch approach (lines 101-103)
- [ ] Remove the commented-out problematic implementation
- [ ] Restore the AssemblyAttribute ItemGroup (lines 104-109)
- [ ] Clean up TODO comments

### Step 3: Validation
- [ ] Test build on Windows (if available)
- [ ] Test build on Linux
- [ ] Verify assembly metadata includes correct CommitDate
- [ ] Run full solution build to ensure no regressions

## Technical Details

**MSBuild Expression Context**: 
- Linux MSBuild may have different method resolution for DateTime operations
- Static method calls vs instance method calls behave differently
- UnixEpoch is a static property available in .NET 6+

**Expected Outcome**:
- Cross-platform builds work without modification
- Git commit date is correctly embedded in assembly metadata
- No impact on generated assembly attributes

## Files to Modify

- `Directory.Build.props` (lines 81-109)

## Success Criteria

1. **Cross-Platform Builds**: Solution builds successfully on both Windows and Linux
2. **Functional Metadata**: Assembly metadata contains correct git commit date
3. **No Regressions**: All existing functionality preserved
4. **Clean Code**: No TODO comments or commented-out failed implementations

## Risk Assessment

**Low Risk**:
- Simple replacement of DateTime expression
- Alternative implementation already exists and commented
- Only affects build-time metadata generation

**Mitigation**:
- Test on multiple platforms before finalizing
- Keep backup of current approach until verified working
- Ensure git repository detection still works correctly

## Notes

This issue was discovered during Task 031 (Assembly Marker standardization) when attempting to validate changes with a build. The build failure prevented verification of assembly marker changes, but the changes themselves are unrelated to this DateTime issue.

The fix should be straightforward since a working alternative implementation already exists in the file (just commented out).