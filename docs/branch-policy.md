# Permanent branch policy and repository onboarding

All Meitou-Modding-Ecosystem repositories use the [central permanent policy](https://github.com/TheIvoryDisaster/GutenStructure/blob/main/docs/branch-policy.md):

| Branch | Requirement |
| --- | --- |
| Ordinary branches | Unprotected |
| beta | PR + passing CI; super-maintainers may bypass review, including self-merges |
| main / release | PR + passing CI + one independent super-maintainers approval; no review bypass |

GitHub Actions has no review bypass. Beta's team exception applies only to reviews: mandatory PR/CI safety has no bypass. CODEOWNERS does not add an extra approval requirement.

GitHub templates copy source, not branch rulesets, team permissions, environments or secrets. GutenStructure's Configure release branch protections discovers new public org repositories hourly at minute 17, assigns the team repository write access if missing, and applies the policy. To configure a new repo immediately, ask an authorized GutenStructure maintainer to preview then apply that workflow on main, target organization. Do this before relying on protected merges or publishing. Keep Release readiness in the caller CI and verify actual passing runs; expected checks with no matching workflow will wait forever.

Create main/release from reviewed stable source when ready; policy targets them even before they exist. Keep this template's immutable reviewed central workflow pins, updating both release gate and package publisher together through PRs. Private repos require separate plan/protection validation; no Azure access is granted by discovery. External platform secrets remain in author-owned environments and are never copied from this template.
