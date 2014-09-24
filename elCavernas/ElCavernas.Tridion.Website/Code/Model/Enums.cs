using System;

public enum LayoutType
{
    Vertical = 1,
    Horizontal = 2,
    Grid = 3
}

public enum TridionItemType
{
    Publication = 1,
    Folder = 2,
    StructureGroup = 4,
    Schema = 8,
    Component = 16,
    ComponentTemplate = 32,
    Page = 64,
    PageTemplate = 128,

    Category = 512,
    Keyword = 1024,
}