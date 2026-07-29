# How to filter tests by tags

If you want to only run tests with a given tag/s you can do this by passing in the `--Tag` parameter after `--`.
If you want to run more than one Tag pass the parameter multiple times.

Examples:

`dotnet fixie --no-build -- --Tag Fast --Tag Smoke`

`dotnet fixie -- --Tag Smoke`
