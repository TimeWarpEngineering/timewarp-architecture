# Developer Guidelines for Building UI with FluentUI and Tailwind CSS

## Introduction
These guidelines are designed to help developers effectively use FluentUI and Tailwind CSS in building user interfaces within the TimeWarp Architecture. The goal is to ensure a consistent, efficient, and accessible user experience across all platforms.

## 1. Base Styles and CSS Strategy
### Minimizing Global CSS
- **Base Styles**: Use global styles sparingly. Focus on essential site-wide elements that do not change often.
- **CSS Isolation**: Favor local CSS for component-specific styling to avoid unintended global effects. This approach promotes encapsulation and maintainability.

## 2. Layout Management Using TimeWarpPage
- **TimeWarpPage Component**: Utilize the `TimeWarpPage` component to handle the overall layout structure.
- **FluentUI Layout Components**: Within `TimeWarpPage`, use FluentUI’s `Stack` and `Grid` components to manage content sections.

## 3. Styling with Tailwind CSS
- **Spacing**: Use Tailwind for margin and padding to provide precise control over layout spacing.
- **Interactivity**: Apply Tailwind's hover states to enhance UI interactivity without custom CSS.
- **Responsive Design**: Employ Tailwind's responsive utilities for fine-tuning responsiveness where FluentUI components fall short.

## 4. Color and Theme Consistency
- **Avoid Tailwind Colors**: Stick to FluentUI’s color palette to ensure consistency with automatic theme support (e.g., light and dark modes).
- **Documentation**: Maintain a list of prohibited Tailwind utilities that could conflict with FluentUI’s styling, such as colors, typography, borders, and shadows.

## 5. Media Queries and Additional Responsive Design Considerations
- **FluentUI Responsiveness**: Leverage the intrinsic responsiveness of FluentUI’s layout components wherever possible.
- **Selective Use of Tailwind**: Implement Tailwind’s media queries selectively to address specific design requirements not covered by FluentUI.

## Conclusion
Adhering to these guidelines will ensure that the TimeWarp application remains consistent, maintainable, and responsive. By leveraging the strengths of both FluentUI and Tailwind CSS, developers can create sophisticated user interfaces that are both functional and visually appealing.

## Appendix

### Tailwind CSS Classes to Avoid
- **Restricted Tailwind Classes**: List specific Tailwind classes that are restricted due to potential conflicts with FluentUI's design system. 

Mostly colors.

bg-*-*,
text-*-*,
border-*-*,
shadow-*-*,

### Tailwind CSS Classes to Use
- **Recommended Tailwind Classes**: Specify Tailwind classes that are safe to use in conjunction with FluentUI components.

m-*,
p-*,
hover:*
group-hover:*
group

