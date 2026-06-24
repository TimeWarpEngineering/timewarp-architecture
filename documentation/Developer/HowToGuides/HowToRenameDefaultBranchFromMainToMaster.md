# Renaming Default Branch from `main` to `master` in GitHub

## Steps:

### 1. Rename the branch locally

```bash
git branch -m main master
```

### 2. Push the renamed branch to GitHub

```bash
git push origin master
```

### 3. Change the default branch in GitHub

- Go to your repository on GitHub.
- Click on `Settings`.
- In the left sidebar, click on `Branches`.
- Under `Default branch`, change the branch to `master`.
- Click `Update`.

### 4. Delete the old `main` branch on GitHub

```bash
git push origin --delete main
```

### 5. Update any local clones to use the new default branch

```bash
git fetch origin
git checkout master
git pull origin master
```
