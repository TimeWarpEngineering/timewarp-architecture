# SitePage Component

The SitePage component is a specific implementation of the TimeWarpPage component, customized for a particular site. It provides a structured layout while allowing for customization of certain areas.

## Overview

SitePage inherits from BaseComponent and uses the TimeWarpPage as its foundation. It pre-configures some areas of the TimeWarpPage while exposing others for customization.

## Features

- Built on top of the TimeWarpPage component
- Pre-configured Left Pane and Right Pane Footer
- Customizable Page Pane and Aside Pane content
- Controllable visibility of Left Pane, Site Footer, and Placeholders

## Parameters

### Content Parameters

These parameters allow you to inject custom content into different areas of the page:

- `PagePane_HeaderContent`
- `PagePane_MainContent`
- `PagePane_FooterContent`
- `AsidePane_HeaderContent`
- `AsidePane_MainContent`
- `AsidePane_FooterContent`

### Control Parameters

These parameters control the visibility of different page elements:

- `ShowLeftPane`: Controls the visibility of the Left Pane (default: true)
- `ShowSiteFooter`: Controls the visibility of the site footer (default: true)
- `ShowPlaceholders`: Controls the visibility of placeholder content (default: true)

## Usage

To use the SitePage component in your Razor pages:

1. Inherit from SitePage in your Razor component
2. Provide content for the desired sections using the content parameters
3. Optionally, adjust the control parameters to customize the layout

Example:

```razor
@page "/"
@inherits SitePage

<SitePage>
    <PagePane_HeaderContent>
        <h1>Welcome to Our Site</h1>
    </PagePane_HeaderContent>
    <PagePane_MainContent>
        <p>This is the main content of our page.</p>
    </PagePane_MainContent>
    <AsidePane_MainContent>
        <div class="related-content">
            <h3>Related Articles</h3>
            <ul>
                <li><a href="/article1">Article 1</a></li>
                <li><a href="/article2">Article 2</a></li>
                <li><a href="/article3">Article 3</a></li>
            </ul>
        </div>
    </AsidePane_MainContent>
</SitePage>
```

## Pre-configured Components

SitePage comes with pre-configured components for certain areas:

- `<LeftPane_Header/>`: Pre-defined header for the Left Pane
- `<LeftPane_Main/>`: Pre-defined main content for the Left Pane (may include navigation)
- `<LeftPane_Footer/>`: Pre-defined footer for the Left Pane
- `<RightPane_Footer/>`: Pre-defined footer for the Right Pane

These components are automatically included in the SitePage layout.

## Customization

You can customize the SitePage by:

1. Providing content for the exposed RenderFragment parameters
2. Adjusting the `ShowLeftPane`, `ShowSiteFooter`, and `ShowPlaceholders` parameters
3. Overriding the pre-configured components if needed in your specific implementation

## Notes

- The Left Pane content is pre-configured and typically contains site-wide navigation elements. It can be hidden using the `ShowLeftPane` parameter.
- The Right Pane Footer is pre-configured.
- Page Pane is the main content area and is fully customizable.
- Aside Pane is typically used for supplementary content related to the main content (e.g., related articles, additional information, widgets) and is fully customizable.
- The `ShowPlaceholders` parameter is set to true by default, which can be useful during development.

Remember to follow the coding standards specified in your project, such as using PascalCase for public properties and camelCase for local variables.
