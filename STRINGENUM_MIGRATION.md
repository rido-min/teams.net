# StringEnum Migration Guide

## Overview

This guide documents the migration from the custom `StringEnum` class to standard C# enums with JSON converters.

## Migration Status

### Infrastructure (Complete ✅)

- `CamelCaseEnumConverter<T>` - For standard camelCase enum serialization
- `StringEnumConverter<T>` - For enums with custom string values
- `EnumStringValueAttribute` - To specify custom JSON values for enum members
- Extension methods for EventType support in EventEmitter and App

### Completed Migrations ✅

1. **DeliveryMode** (Microsoft.Teams.Api) - Simple camelCase enum
2. **EventType** (Microsoft.Teams.Apps.Events) - Enum with dotted values using `EnumStringValueAttribute`
3. **EndOfConversationCode** (Microsoft.Teams.Api) - Simple camelCase enum

### Migration Patterns

#### Pattern 1: Simple CamelCase Enum

For StringEnum classes with simple camelCase values:

**Before:**
```csharp
[JsonConverter(typeof(JsonConverter<DeliveryMode>))]
public class DeliveryMode(string value) : StringEnum(value)
{
    public static readonly DeliveryMode Normal = new("normal");
    public bool IsNormal => Normal.Equals(Value);

    public static readonly DeliveryMode Notification = new("notification");
    public bool IsNotification => Notification.Equals(Value);
}
```

**After:**
```csharp
[JsonConverter(typeof(CamelCaseEnumConverter<DeliveryMode>))]
public enum DeliveryMode
{
    Normal,
    Notification,
    ExpectReplies,
    Ephemeral
}
```

#### Pattern 2: Enum with Special String Values

For StringEnum classes with dots, slashes, or other special characters:

**Before:**
```csharp
public class EventType(string value) : StringEnum(value)
{
    public static readonly EventType ActivitySent = new("activity.sent");
    public bool IsActivitySent => ActivitySent.Equals(Value);

    public static readonly EventType ActivityResponse = new("activity.response");
    public bool IsActivityResponse => ActivityResponse.Equals(Value);
}
```

**After:**
```csharp
[JsonConverter(typeof(StringEnumConverter<EventType>))]
public enum EventType
{
    Start,
    Error,
    [EnumStringValue("activity.sent")]
    ActivitySent,
    [EnumStringValue("activity.response")]
    ActivityResponse
}
```

#### Pattern 3: Helper Methods via Extension Methods

For StringEnum classes with helper methods like `IsBuiltIn`:

**Before:**
```csharp
public class EventType(string value) : StringEnum(value)
{
    // ... enum values ...
    
    public bool IsBuiltIn => IsStart || IsError || IsSignIn;
}
```

**After:**
```csharp
[JsonConverter(typeof(StringEnumConverter<EventType>))]
public enum EventType
{
    Start,
    Error,
    SignIn
}

public static class EventTypeExtensions
{
    public static bool IsBuiltIn(this EventType eventType)
    {
        return eventType == EventType.Start 
            || eventType == EventType.Error 
            || eventType == EventType.SignIn;
    }
}
```

#### Pattern 4: Partial Classes

For partial StringEnum classes split across multiple files (like ActivityType):

1. Convert the main definition to an enum
2. Keep all values in a single enum definition (enums can't be partial)
3. Consolidate values from all partial files into one enum
4. Use extension methods for any additional logic

### Remaining Migrations (By Priority)

#### High Priority - Simple, No Dependencies

- InstallUpdateAction
- ChannelId
- ConversationType  
- Role
- Importance
- InputHint
- TextFormat
- AspectRatio
- MembershipType
- MembershipSourceType
- SearchType

#### Medium Priority - May Have Special Values

- ContentType (has MIME types with slashes)
- ActionType (has "Action.Execute" etc with dots)
- Various Name classes in Activities (may have application/vnd.microsoft.* values)

#### High Priority - Complex (Partial Classes)

- **ActivityType** - Defined across ~13 files as partial class. Strategy:
  1. Identify all unique ActivityType values across all files
  2. Consolidate into single enum definition in Activity.cs
  3. Remove all partial ActivityType definitions from other files
  4. Update ActivityType references throughout codebase

- **InvokeActivity.Name** - Has nested partial classes (AdaptiveCards, Configs, MessageExtensions, Messages)
  - May need multiple separate enums or consolidation strategy

#### Lower Priority - AI and Card Related

- Message types in Microsoft.Teams.AI
- Card-related enums in Microsoft.Teams.Cards
- ANSI color codes in Microsoft.Teams.Common.Text

### Migration Checklist for Each StringEnum

For each StringEnum class to migrate:

1. [ ] Identify all static readonly values and their string representations
2. [ ] Check if any values have special characters (dots, slashes, etc.)
3. [ ] Determine converter type:
   - Use `CamelCaseEnumConverter<T>` for simple camelCase
   - Use `StringEnumConverter<T>` with `EnumStringValueAttribute` for special cases
4. [ ] Check for helper methods (IsXxx properties, complex logic)
   - Move to extension methods if needed
5. [ ] Find all usages of the StringEnum class
   - Check for `new ClassName(stringValue)` constructions - these need updates
   - Check for implicit string conversions - may need explicit `ToString()` or converter helper
6. [ ] Update all tests that construct StringEnum instances
7. [ ] Build and test
8. [ ] Verify JSON serialization/deserialization works correctly

### Testing Strategy

For each migrated enum:

1. Verify JSON serialization produces expected string values
2. Verify JSON deserialization from strings works
3. Verify enum comparison and equality work
4. Verify enum values in dictionaries and collections
5. Verify any extension methods work correctly
6. Run existing unit tests
7. Check integration tests if applicable

### Known Issues and Gotchas

1. **No Implicit String Conversion**: Enums don't have implicit conversion to string like StringEnum did.
   - Solution: Use appropriate converter or explicit serialization where needed

2. **Constructor Calls**: `new EventType("value")` is no longer valid
   - Solution: Parse using `JsonSerializer.Deserialize<EventType>("\"value\"")`  or use `Enum.Parse<T>()`

3. **Partial Enums**: C# doesn't support partial enums
   - Solution: Consolidate all values into single enum definition

4. **Extension Methods**: Helper methods need to become extension methods
   - Solution: Create static class with extension methods

5. **Default Values**: Enums have a default value of 0
   - Solution: Consider adding an "Unknown" or "None" value as first member if needed

### Benefits of Migration

1. **Type Safety**: Native enum support with compile-time checking
2. **IDE Support**: Better IntelliSense, refactoring, and code navigation
3. **Performance**: No reflection or custom converter overhead for simple cases
4. **Standard Pattern**: Uses well-known .NET patterns
5. **Tooling**: Works with standard enum tools and analyzers
6. **Pattern Matching**: Can use switch expressions and pattern matching

## Next Steps

1. Continue migrating simple enums (Importance, InputHint, TextFormat, etc.)
2. Handle complex enums with special string values (ContentType, ActionType)
3. Tackle partial class enums (ActivityType - most complex)
4. Remove StringEnum class once all migrations are complete
5. Update documentation and examples

## Questions or Issues

If you encounter issues during migration:

1. Check existing migrations for similar patterns
2. Review test cases for expected JSON formats
3. Use `StringEnumConverter<T>` with `EnumStringValueAttribute` for non-camelCase values
4. Create extension methods for complex logic
