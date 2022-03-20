# Simple Icons

Social Media Icons are found here
https://simpleicons.org/

## Contribute

To add more icons from Simple Icons.
1. Create and <XXX>Icon.razor file.
2. Add 

```
@namespace SimpleIcons
@inherits BaseSvg
```
3. Copy the desired svg from simple icons
4. Paste the svg into https://jakearchibald.github.io/svgomg/  (will reduce the size and pretty print)
5. Copy result from svgomg into razor file.
6. Add `@attributes=Attributes` to the end of the svg tag as in the example below:

```
<svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" @attributes=Attributes>
```
