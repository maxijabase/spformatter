# Style contract

Product defaults for formatted SourcePawn. Goldens must follow this file, not ad-hoc taste.

## Indent

- 4 spaces
- no tabs by default (`UseSpaces = true`)

## Braces

- opening brace on its own line after function / control headers when `NewLineAfterOpenBrace` is true (default)
- closing brace on its own line
- do not rewrite bare single-statement `if` bodies into braced blocks in new printer code unless an explicit option says so

## Spacing

- space after commas (default on)
- spaces around binary operators (default on)
- no space before `(` in calls / control headers (default)
- no spaces inside array brackets (default)

## Semicolons

- require semicolons on statements (default on)
- `#pragma semicolon 0` / optional-semicolon stripping is deferred until the option is real

## Line endings

- formatter uses `FormattingOptions.LineEnding`
- tests force `\n` for cross-platform goldens
- expected fixtures on disk should use `\n`

## Includes and top-level order

- preserve source order by default in new printer code
- include sorting is deferred / opt-in only after the printer is stable

## Blank lines

- preserve author blank lines between siblings from original source gaps
- cap with `MaxConsecutiveEmptyLines` (default 2); disable with `PreserveEmptyLines = false`

## Deferred (not options yet)

- alignment of consecutive assignments / declarations
- optional semicolon stripping / `#pragma semicolon 0` as a first-class mode
- brace injection for bare `if` bodies as a silent default
- top-level reordering as a silent default
- indenting preprocessor directives
- formatting through function-like macros (refuse by default; see SUPPORTED.md)

