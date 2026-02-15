# AGENTS.md - Hotstring Windows Agent Guide

## Build and Development Commands

### Build and Run
```bash
# Restore dependencies and build
dotnet build

# Restore, build, and run the application
dotnet run

# Build for release
dotnet build --configuration Release
```

### Lint and Quality Checks
```bash
# Run diagnostics and style checks
dotnet format

# Analyze code for potential issues
dotnet format --verify-no-changes

# Run style analyzers
dotnet build /p:TreatWarningsAsErrors=false
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity diagnostic

# Run a single test file
dotnet test <TestProjectName>.csproj --filter "<FullyQualifiedName>MethodName"

# List all tests
dotnet test --list-tests
```

## Code Style Guidelines

### General C# and .NET 10 Conventions

#### Imports and Namespaces
- Prefer implicit usings (`<ImplicitUsings>enable`) for common namespaces
- Explicitly import only when necessary (e.g., `System.Runtime.InteropServices` for P/Invoke)
- Use `System.Runtime.CompilerServices` for CallerMemberName attribute
- Organize using statements by category: System → custom → third-party
- Place using statements at the top of files before any code

#### Naming Conventions
- **Classes**: PascalCase (`HotstringEngine`, `NativeMethods`)
- **Methods**: PascalCase (`HookCallback`, `HandleReplacement`)
- **Properties**: PascalCase (`Trigger`, `IsPaused`, `Hotstrings`)
- **Private fields**: underscore-prefixed CamelCase (`_proc`, `_hookID`, `_keyState`)
- **Events**: PascalCase with "Changed" or "PropertyChanged" suffix
- **Local variables**: CamelCase (`curProcess`, `hotstring`, `vkCode`)
- **Constants**: PascalCase (`WH_KEYBOARD_LL`, `WM_KEYDOWN`)
- **Local functions**: PascalCase with descriptive names
- **Event handlers**: CamelCase with "Clicked", "Changed", "Closing" suffixes

#### Type Naming
- **Interfaces**: PascalCase with 'I' prefix (`INotifyPropertyChanged`, `IDisposable`)
- **Delegates**: PascalCase describing action (`LowLevelKeyboardProc`)
- **Enums**: PascalCase with clear names
- **Generic types**: Single uppercase letter or PascalCase (`ObservableCollection<T>`)
- **Record types**: PascalCase (when used)

#### Property and Field Declaration
- Use nullable reference types (`<Nullable>enable`)
- Enable implicit usings (`<ImplicitUsings>enable`)
- Use expression-bodied members for simple property accessors
```csharp
public string Trigger 
{ 
    get => _trigger; 
    set { _trigger = value; OnPropertyChanged(); } 
}
```

- Prefer read-only automatic properties when possible
```csharp
public ObservableCollection<Hotstring> Hotstrings { get; } = new ObservableCollection<Hotstring>();
```

#### Method Design
- Use expression-bodied members for simple return statements
- Avoid overly complex methods (>30 lines); extract functionality into helper methods
- Use descriptive parameter names (use `c` for char, `vkCode` for virtual key code)
- Use `null` or `null!` for non-nullable reference types explicitly when needed

#### Error Handling
- Use `try-finally` for resource management (`Dispose` pattern)
- Use `try-catch` with specific exceptions and meaningful messages
- Use `null`-coalescing and null-forgiving operators appropriately
- Avoid empty catch blocks; log or rethrow
- Use `using` statements for `IDisposable` objects

#### Code Formatting and Formatting Tools
- Use 4-space indentation
- Use spaces around operators and after commas
- Place opening brace on same line for control statements
- Align method/property signatures vertically
- Use blank lines to separate logical sections (10-15 lines apart)

#### Collection and Data Structures
- Use `ObservableCollection<T>` for UI data binding
- Use `List<T>` for general collection manipulation
- Use `HashSet<T>` for membership testing
- Use `StringBuilder` for string concatenation in performance-critical code
- Use null-coalescing and null-forgiving operators appropriately
- Use `First()` with fallback null or `FirstOrDefault()` for collection operations
```csharp
var match = Hotstrings.FirstOrDefault(h => 
    !h.ExpandImmediately && 
    BufferEndsWith(h.Trigger, h.CaseSensitive));
```

#### P/Invoke and Interop
- Use `System.Runtime.InteropServices` with `[DllImport]` for Windows API
- Specify CharSet for string marshaling (`CharSet.Auto`)
- Use `[return: MarshalAs(UnmanagedType.Bool)]` for boolean returns
- Use `[StructLayout(LayoutKind.Sequential)` for struct definitions
- Use `[StructLayout(LayoutKind.Explicit)` for union types
- Specify exact struct sizes for input/output operations
- Use explicit marshaling for complex structs (`INPUT`, `KEYBDINPUT`)
- Handle IntPtr return types from Win32 API calls
- Use correct function signatures for low-level keyboard hooks

#### Memory Management
- Use `StringBuilder` for buffer operations
- Use `Marshal.SizeOf()` for struct size calculations
- Use `Marshal.ReadInt32()` for lParam parameter access
- Release unmanaged resources via `Dispose()` pattern
- Avoid memory leaks; ensure all hooks and resources are cleaned up

#### WPF and UI Layer Patterns
- Use `INotifyPropertyChanged` for data binding
- Implement `IDisposable` for clean resource management
- Use `ObservableCollection<T>` for dynamic UI updates
- Handle `Window.Closing` event for cleanup
- Use nullable reference types for UI elements
- Use expression-bodied members for simple UI event handlers
- Use event handlers with sender and event args patterns

#### Win32 API and Low-Level Interop
- Use `Process.GetCurrentProcess()` and `ProcessModule.MainModule` for module handles
- Use `SetWindowsHookEx` with `WH_KEYBOARD_LL` for global keyboard hooks
- Use `CallNextHookEx` to chain hook handlers
- Track key states manually using byte arrays for Windows API compatibility
- Use `SendInput` with INPUT structures for synthetic key input
- Use `ToUnicode` for character conversion with manual state tracking
- Use `GetKeyboardState` for low-level key state access
- Handle virtual key codes and scan codes properly
- Handle low-level keyboard callback with correct parameter types

#### Type Safety and Nullability
- Use nullable reference types consistently
- Use null-conditional operators (`?.`) for member access
- Use null-forgiving operator (`!`) when context proves non-null
- Use null-coalescing operator (`??`) for fallback values
- Use null-coalescing assignment operator (`??=`) for null checks

#### Accessibility and User Experience
- Handle navigation keys properly (arrows, home, end, page up/down, insert, delete)
- Implement pause functionality to prevent user frustration
- Clear buffers after navigation actions
- Handle end characters for hotstring completion
- Support immediate expansion mode for productivity
- Support optional end character omission
- Handle backspace and buffer management

#### Code Organization and Structure
- Place P/Invoke declarations in `NativeMethods` internal static class
- Place domain logic in `HotstringEngine` class
- Place UI logic in `MainWindow` class
- Place data models in `Hotstring` class with INotifyPropertyChanged
- Use clear separation of concerns between layers
- Use descriptive variable names at all levels
- Use private/internal helper methods for complex logic
- Use public APIs with clear documentation

#### Project Configuration
- Use .NET 10.0+ SDK (`global.json` specifies version)
- Enable nullable reference types (`<Nullable>enable`)
- Enable implicit usings (`<ImplicitUsings>enable`)
- Use WPF project template (`<UseWPF>true</UseWPF>`)
- Configure target framework in .csproj file
- Use MSBuild format for project files

#### Accessibility Features
- Implement pause functionality to control hotstring expansion
- Handle special keys that reset buffers
- Support different expansion modes (immediate, delayed)
- Support case-sensitive and case-insensitive patterns
- Support configurable end characters

#### Performance Considerations
- Use StringBuilder for efficient string operations
- Use HashSet for O(1) membership checks
- Use LINQ with appropriate methods (First, FirstOrDefault)
- Clear buffers periodically to prevent memory bloat
- Use manual key state tracking for consistency
- Batch synthetic input operations where possible
- Avoid repeated allocations in hot paths

#### Testing and Verification
- Test hotstring expansion with various key combinations
- Test end character detection
- Test immediate vs delayed expansion modes
- Test pause functionality
- Test navigation key handling
- Test buffer management with backspace
- Test case sensitivity options
- Test omit end character option
- Test resource cleanup on disposal
- Test INotifyPropertyChanged data binding
- Test ObservableCollection dynamic updates

#### Debugging and Logging
- Use Debug.WriteLine for development-time logging
- Add comments for complex Win32 API interactions
- Use clear variable names to convey intent
- Document edge cases and assumptions
- Add telemetry if needed for production