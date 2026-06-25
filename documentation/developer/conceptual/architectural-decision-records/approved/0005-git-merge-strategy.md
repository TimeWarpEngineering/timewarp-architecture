# Git Merge Strategy: Preserve Information Over Presentation

## Context and Problem Statement

When integrating feature branches into the master branch, development teams must choose between different merge strategies that fundamentally affect how git history is preserved and presented. Each approach has implications for debugging, rollbacks, historical analysis, and information preservation.

The primary strategies are:
- **Merge commits**: Preserve complete branch history and merge points
- **Rebase and merge**: Rewrite history to appear linear
- **Squash and merge**: Combine all commits into a single commit

Which merge strategy should we adopt for the TimeWarp Architecture repository?

## Considered Options

### Option 1: Squash and Merge
* **Pros**: 
  - "Clean" linear history appearance
  - Single commit per feature
  - Simplified git log presentation
  - Popular in some development teams
* **Cons**:
  - **Destroys intermediate commit information permanently**
  - Loses individual commit context and progression
  - Makes debugging individual changes difficult
  - Cannot easily revert specific parts of a feature
  - Loses commit author information for individual changes
  - Makes code review archaeology impossible

### Option 2: Rebase and Merge
* **Pros**:
  - Linear history appearance
  - Preserves individual commits
  - No merge commit "clutter"
* **Cons**:
  - **Rewrites commit history and loses original timestamps**
  - Destroys the actual development timeline
  - Loses context of when branches diverged and converged
  - Makes it difficult to understand parallel development
  - Can be confusing for debugging when commits appear to be made in different order than reality

### Option 3: Merge Commits (True Merge)
* **Pros**:
  - **Preserves complete historical information**
  - Maintains actual development timeline
  - Shows parallel development patterns
  - Enables easy rollback of entire features
  - Preserves merge conflict resolution context
  - Supports better debugging and archaeological analysis
  - Maintains commit author and timestamp integrity
* **Cons**:
  - Creates "busier" looking git history
  - More merge commits in the log

## Decision Outcome

Chosen option: **Merge Commits (True Merge)**, because:

### Information Preservation Philosophy
* **Data over presentation**: Historical information is irreplaceable once lost, but presentation can be improved through tooling
* **Archaeological value**: Complete history enables better debugging, code archaeology, and understanding of decision progression
* **Rollback capabilities**: Easy to revert entire features while preserving the work for potential future reference

### Practical Benefits
* **Debugging advantages**: When investigating issues, seeing the actual development progression helps understand the thought process
* **Parallel development visibility**: Shows how different features were developed concurrently
* **Merge conflict context**: Preserves information about how conflicts were resolved
* **Authentic timeline**: Maintains the actual sequence of development work

### Presentation Layer Solutions
* **Tooling handles presentation**: Modern git tools (GitHub, GitKraken, VS Code, etc.) can present history in various ways without losing underlying data
* **Configurable views**: `git log --oneline`, `--graph`, `--first-parent` provide different presentation options
* **IDE integration**: Development environments can show simplified views while preserving complete information

## Implementation Guidelines

### Pull Request Merging
- Use GitHub's "Create a merge commit" option (not squash or rebase)
- Maintain descriptive merge commit messages
- Include pull request number in merge commit for traceability

### Branch Cleanup
- Delete feature branches after merging to reduce visual clutter
- Use `git branch -d` locally after successful merge
- Enable automatic branch deletion in GitHub repository settings

### Git Commands
```bash
# Preferred merge approach
git checkout master
git merge --no-ff feature-branch

# Avoid these approaches
git merge --squash feature-branch  # Loses information
git rebase feature-branch         # Rewrites history
```

### Presentation Tools
When "clean" history view is desired, use:
```bash
# Show only merge commits
git log --first-parent --oneline

# Show simplified graph
git log --graph --oneline --decorate

# Custom format for presentations
git log --pretty=format:"%h %s" --first-parent
```

## Positive Consequences

* **Complete information preservation**: Never lose development context or progression
* **Better debugging**: Full history available for investigating issues
* **Flexible presentation**: Can generate any desired view from complete data
* **Team learning**: New team members can see how features evolved
* **Audit trail**: Complete record of what was changed and when

## Negative Consequences

* **Busier git log**: More commits and merge points in default git log view
* **Team education**: Need to educate team on presentation layer tools
* **Tool configuration**: May need to configure tools to show preferred views

## Related Decisions

* [ADR-0004: Branch Naming Conventions](0004-branch-naming-conventions.md) - Supports better merge commit context
* Git workflow requires merge commits for proper branch lifecycle

---

*"We choose to preserve information over presentation, recognizing that lost data cannot be recovered, but presentation can always be improved through tooling."*