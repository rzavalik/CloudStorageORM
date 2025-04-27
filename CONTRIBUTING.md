# Contributing to CloudStorageORM

First of all, thank you for considering contributing to **CloudStorageORM**! 🚀  
We welcome contributions from everyone.

To keep the project clean, organized, and scalable, we ask contributors to follow these simple guidelines.

---

## 📋 How to Contribute

- Fork the repository.
- Create a branch for your work following our branch naming convention.
- Work on your feature, fix, or improvement.
- Open a Pull Request (PR) referencing the related Issue.


## 🏷️ Branch Naming Convention

When creating a new branch, please use the following prefixes:

| Type | Prefix | Example |
|:---|:---|:---|
| New Feature | `feature/` | `feature/implement-azureprovider` |
| Bug Fix | `bugfix/` | `bugfix/fix-azure-saveasync` |
| Tests | `test/` | `test/unit-cloudstoragerepository` |
| Documentation | `docs/` | `docs/update-readme-azure-example` |
| Refactoring | `refactor/` | `refactor/optimize-repository-queries` |

Example branch names:
- `feature/create-istorageprovider`
- `bugfix/fix-path-handling`
- `test/unit-azureblobstorageprovider`


## 📄 Pull Request Guidelines

When submitting a Pull Request, please:

- Use the following title format:

  ```
  [TYPE] Short Description
  ```

  Where `TYPE` is one of:
  - `[FEATURE]` — New functionality
  - `[BUGFIX]` — Fix for a bug
  - `[TEST]` — Tests added or improved
  - `[DOCS]` — Documentation updates
  - `[REFACTOR]` — Refactoring code without changing functionality

Examples:
- `[FEATURE] Implement AzureBlobStorageProvider`
- `[BUGFIX] Fix DeleteAsync error in Azure Provider`
- `[TEST] Add unit tests for CloudStorageRepository`

- In the PR description:
  - Briefly explain what was implemented or fixed.
  - Reference the related Issue using `Closes #issue_number`.
  - Fill in the checklist from the Pull Request Template.


## ✅ Pull Request Checklist

Before submitting your pull request, make sure that:
- [ ] The code follows the branch naming and PR title conventions.
- [ ] Tests are passing.
- [ ] Code is consistent with project standards.
- [ ] Related Issue is referenced.
- [ ] Documentation was updated if necessary.


## 🛡️ Branch Protection Rules

Direct pushes to `main` are not allowed.  
All changes must go through a Pull Request and must pass CI checks.


## 💬 Need Help?

Feel free to open a [Discussion](https://github.com/rzavalik/CloudStorageORM/discussions) if you have questions or suggestions!

We appreciate your effort and interest! Let's build something amazing together! 🚀
