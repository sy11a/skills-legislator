## .NET Coding Standards

- Meaningful variable names — never single-letter or abbreviated names (`p`, `e`, `v`)
- Explicit types over `var` for built-in types; editorconfig enforces this where configured
- Braces `{}` on **all** `if` statements, including single-line guard returns
- No alignment-padding around `=` in initializers or assignments
- No comments unless the WHY is non-obvious
- No empty catch blocks; no swallowed exceptions
- Treat compiler warnings as errors where the project enables it — zero-warnings policy
