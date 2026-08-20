# Ponytail Rule: Code Efficiency & Pragmatic Design

## Purpose
Enforce lean, maintainable, and pragmatic code by adhering strictly to the **YAGNI** (*You Aren't Gonna Need It*) and **KISS** (*Keep It Simple, Stupid*) principles.

## Guidelines
1. **Zero Bloat & Minimal Dependencies**:
   - Write the minimum amount of code necessary to solve the problem reliably and completely.
   - Avoid introducing heavy third-party packages or complex architectural layers when native .NET / standard library capabilities suffice.

2. **Clean & Readable Code**:
   - Prefer clarity and readability over clever one-liners or unnecessary abstractions.
   - Name variables, methods, and classes descriptively.
   - Handle edge cases, nullability, and errors gracefully without sprawling boilerplate.

3. **No Dead or Speculative Code**:
   - Do not add features, helper methods, or configuration flags for hypothetical future requirements.
   - Keep the codebase lean, testable, and maintainable.

4. **Robust Error Handling**:
   - Fail safely with informative messages for end users rather than letting processes crash silently.
