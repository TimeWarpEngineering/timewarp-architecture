https://github.com/PoshCode/PowerShellPracticeAndStyle/issues/36

Keywords (try, catch, foreach, switch)
lowercase (rationale: no language other than VB uses mixed case keywords?)

Process Block Keywords (begin, process, end, dynamicparameter)
lowercase (same reason as above)

Comment Help Keywords (.Synopsis, .Example, etc)
PascalCase (rationale: readability)

Package/Module Names
PascalCase

Class Names
PascalCase

Exception Names (these are just classes in PowerShell)
PascalCase

Global Variable Names
$PascalCase

Local Variable Names
$camelCase (see for example: $args and $this)

Function Names
PascalCase

Function/method arguments
PascalCase

Private Function Names (in modules)
PascalCase

Constants
$PascalCase
