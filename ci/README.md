# Continuous integration

[`github-actions-ci.yml`](github-actions-ci.yml) is a ready-to-use GitHub Actions workflow that
builds and tests the solution on every push and pull request.

It lives here (instead of `.github/workflows/`) only because the initial push was made with a token
that lacked the `workflow` scope. To enable it:

```sh
gh auth refresh -h github.com -s workflow      # grant the workflow scope (one-time)
mkdir -p .github/workflows
git mv ci/github-actions-ci.yml .github/workflows/ci.yml
git commit -m "Enable CI workflow"
git push
```
